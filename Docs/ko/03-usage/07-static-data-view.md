# 3.7 StaticDataView로 사전계산 뷰 합성

[3.6](./06-static-data-manager.md) 까지 만든 매니저는 테이블을 그대로 노출합니다. 그러나 실무에서는 "여러 테이블을 조인한 결과", "특정 키로 그룹핑한 합계" 처럼 **테이블 위에서 한 번 계산해 두면 좋은 결과** 가 자주 필요합니다. 이런 결과를 매 조회마다 다시 만드는 대신 **로드 시점에 한 번 빌드해 두는** 합성 레이어가 `StaticDataView` 입니다.

## 언제 쓰나

- 두 개 이상의 테이블을 조인한 결과를 자주 조회한다.
- FK 로 묶인 자식 행들을 부모 키로 미리 그룹핑해 두고 싶다.
- 합계·필터링 같은 가공이 매번 같은 입력에 대해 반복된다.

뷰는 read-only 입니다. `LoadAsync` 한 번에 TableSet 과 ViewSet 이 함께 만들어지고, 빌드 도중에는 어떠한 가변 상태도 외부에 노출되지 않습니다.

## ViewSet 정의

ViewSet 은 TableSet 과 같은 모양 — **각 뷰를 파라미터로 받는 record** — 으로 두며, Manager 안에 함께 선언하는 것을 권장합니다 (필수 아님).

```csharp
using Microsoft.Extensions.Logging;
using Sdp.Manager;

public sealed class GameStaticData(ILogger<GameStaticData> logger)
    : StaticDataManager<GameStaticData.TableSet, GameStaticData.ViewSet>(logger)
{
    public sealed record TableSet(
        EventTable? Events,
        WeaponTable? Weapons,
        ArmorTable? Armors);

    public sealed record ViewSet(
        EventBundleView EventBundles,
        EventAttackTotalView EventAttackTotals);

    public EventBundleView EventBundles => Current.Views.EventBundles;
    public EventAttackTotalView EventAttackTotals => Current.Views.EventAttackTotals;
}
```

세 가지 규칙입니다.

1. **2-제네릭 베이스 사용** — TableSet 만 다루는 매니저는 `StaticDataManager<TTableSet>` 이지만, 뷰까지 합성하려면 `StaticDataManager<TTableSet, TViewSet>` 베이스를 상속한다. `ILogger` 를 그대로 base 로 전달하는 부분은 동일.
2. **각 뷰 파라미터는 non-nullable** — TableSet 은 `disabledTables` 옵션으로 일부가 빠질 수 있어 nullable 이지만, ViewSet 은 빌더가 모든 슬롯을 항상 채우므로 nullable 로 선언하면 `ViewSetMemberMustBeNonNullable` 로 거부된다.
3. **`Current` 는 `TableAndViewSet(Tables, Views)` 묶음을 public 으로 노출** — 한 묶음으로 atomic 하게 교체되며, 외부에서 `Current.Tables`, `Current.Views` 로 꺼낸다. 매니저 서브클래스에서 자주 쓰는 뷰는 위 예제처럼 풀어 둔다.

## StaticDataView 구현

뷰는 CRTP 로 자기 자신을 첫 번째 인자에 넣고, **TableSet 하나만 받는 생성자** 를 가집니다. 생성자 안에서 필요한 인덱스·집계를 미리 만들어 두는 것이 표준 패턴입니다.

```csharp
using Sdp.Table;
using Sdp.View;

public sealed record EventBundle(
    EventRecord Event,
    IReadOnlyList<Equipment> Equipment);

public sealed class EventBundleView(GameStaticData.TableSet tables)
    : StaticDataView<EventBundleView, GameStaticData.TableSet>(tables)
{
    private readonly UniqueIndex<EventBundle, int> byEventId = Build(tables);

    public EventBundle Get(int eventId)
        => byEventId.Get(eventId);

    private static UniqueIndex<EventBundle, int> Build(GameStaticData.TableSet t)
    {
        var weaponsByEvent = new MultiIndex<WeaponRecord, int>(t.Weapons!.Records, x => x.EventId);
        var armorsByEvent = new MultiIndex<ArmorRecord, int>(t.Armors!.Records, x => x.EventId);

        var bundles = new List<EventBundle>();
        foreach (var ev in t.Events!.Records)
        {
            var equipment = new List<Equipment>();
            foreach (var weapon in weaponsByEvent.Get(ev.Id))
            {
                equipment.Add(new Equipment(weapon.Id, EquipmentType.Weapon, weapon.Name, weapon.AttackPower, 0));
            }

            foreach (var armor in armorsByEvent.Get(ev.Id))
            {
                equipment.Add(new Equipment(armor.Id, EquipmentType.Armor, armor.Name, 0, armor.DefensePower));
            }

            bundles.Add(new EventBundle(ev, equipment));
        }

        return new UniqueIndex<EventBundle, int>(bundles, b => b.Event.Id);
    }
}
```

핵심은 다음 세 가지입니다.

1. **CRTP** — `StaticDataView<EventBundleView, GameStaticData.TableSet>` 처럼 자기 자신과 입력 TableSet 을 타입 인자로 묶는다.
2. **TableSet 만 받는 단일 생성자** — `ViewSetBuilder` 가 리플렉션으로 이 생성자를 호출한다. 다른 시그니처가 있으면 `ViewConstructorNotFound` 로 실패한다.
3. **생성자 안에서 빌드를 끝낸다** — 외부에 가변 상태를 두지 않고, 조회용 인덱스 (`UniqueIndex`, `MultiIndex`) 와 사전계산된 컬렉션만 readonly 로 보관한다.

## 로드 흐름과 빌드 단계

`LoadAsync` 흐름은 [3.6](./06-static-data-manager.md) 의 단일 제네릭 매니저와 거의 같고, **검증을 모두 통과한 뒤 ViewSet 빌드 단계가 추가** 됩니다.

1. FK 타겟 검증.
2. TableSet 병렬 로드 (테이블별 `Trace` 로그 `LoadedTable`).
3. `ReferenceValidator` 로 실제 FK 값 검증.
4. (override 했다면) `Validate(TTableSet)` 호출.
5. **`ViewSetBuilder.Build` — ViewSet 의 생성자 파라미터를 하나씩 훑어 각 뷰를 차례로 빌드. 뷰별 빌드 시간은 `Trace` 레벨로 기록 (메시지 키 `BuiltView`).**
6. 새로 빌드된 `(TableSet, ViewSet)` 을 `volatile` 필드에 한 번에 교체.
7. 전체 완료 시간을 `Information` 레벨로 기록.

뷰 빌드 도중 어느 하나라도 예외를 던지면 모든 뷰의 시도를 끝까지 모은 뒤 `AggregateException(Messages.ViewsFailedToBuild, ...)` 을 던집니다. 이 경우 `Current` 는 갱신되지 않으므로 직전 스냅샷 (또는 첫 로드라면 아직 비어 있는 상태) 이 유지됩니다.

ViewSet 자체에 잘못된 파라미터가 있을 때의 진단도 정해져 있습니다.

- 파라미터 타입이 `StaticDataView<,>` 서브타입이 아니면 `InvalidViewParameter`.
- 파라미터가 nullable 이면 `ViewSetMemberMustBeNonNullable`.
- TableSet 한 개만 받는 생성자가 없으면 `ViewConstructorNotFound`.
- ViewSet record 에 생성자가 둘 이상이면 `ViewSetMustHaveSingleConstructor`.

모두 빌드 단계에서 `AggregateException(Messages.ViewsFailedToBuild, ...)` 의 inner 로 들어갑니다.

## 동시성

`Current` 가 가리키는 `TableAndViewSet(Tables, Views)` 는 한 묶음으로 atomic 하게 교체됩니다. 조회 중인 스레드가 있을 때 다른 스레드가 `LoadAsync` 를 다시 호출해도, 조회 측은 항상 **TableSet 과 ViewSet 이 정합한 한 묶음** 을 봅니다. 한쪽만 새 값으로 바뀌어 있는 중간 상태는 노출되지 않습니다. 이 보장의 전제는 ViewSet 이 단일 생성자를 가진 record 라는 점이며, `ViewSetMustHaveSingleConstructor` 가 그 전제를 강제합니다.

## ASP.NET Core 에서 DI 로 사용

[3.6 의 등록 패턴](./06-static-data-manager.md#aspnet-core-에서-di-로-사용) 과 동일한 방식으로, View 까지 함께 Scoped 로 등록합니다. Manager 는 Singleton, 각 테이블과 각 View 는 Scoped 입니다.

```csharp
// Program.cs
services.AddSingleton<GameStaticData>();

// 각 테이블
services.AddScoped<EventTable>(sp =>
    sp.GetRequiredService<GameStaticData>().Current.Tables.Events!);

services.AddScoped<WeaponTable>(sp =>
    sp.GetRequiredService<GameStaticData>().Current.Tables.Weapons!);

services.AddScoped<ArmorTable>(sp =>
    sp.GetRequiredService<GameStaticData>().Current.Tables.Armors!);

// 각 View (ViewSet 멤버는 non-nullable 이라 ! 가 없다)
services.AddScoped<EventBundleView>(sp =>
    sp.GetRequiredService<GameStaticData>().Current.Views.EventBundles);

services.AddScoped<EventAttackTotalView>(sp =>
    sp.GetRequiredService<GameStaticData>().Current.Views.EventAttackTotals);
```

```csharp
public sealed class EventController(
    EventBundleView bundles,
    EventAttackTotalView totals) : ControllerBase
{
    [HttpGet("{id:int}")]
    public IActionResult Get(int id)
    {
        // bundles 와 totals 는 이 요청에 묶인 동일한 (TableSet, ViewSet) 묶음에서 꺼낸 값
        ...
    }
}
```

응용 측에서는 보통 `GameStaticData` 전체나 `TableSet` 전체를 받지 않고, 실제로 쓰는 테이블·View 만 골라 주입받는 편이 호출부가 깔끔합니다.

## TableSet 만 쓰는 경우

뷰 합성이 필요 없으면 [3.6](./06-static-data-manager.md) 의 `StaticDataManager<TTableSet>` 를 그대로 사용하면 됩니다. 두 베이스는 별도로 존재하며, 뷰가 필요해진 시점에 2-제네릭 베이스로 옮기는 식으로 점진적으로 확장할 수 있습니다.

## 요약

- `StaticDataManager<TTableSet, TViewSet>` 가 뷰 합성을 담당한다.
- 뷰는 `StaticDataView<TSelf, TTableSet>` 를 상속하고 TableSet 한 개를 받는 생성자에서 빌드를 끝낸다.
- ViewSet 은 non-nullable 뷰들을 가진 record 로 두고, `Current.Views` 로 접근한다.
- TableSet 과 ViewSet 은 한 묶음으로 atomic 교체되므로 조회는 항상 정합한 묶음을 본다.

---

[← 이전: 3.6 StaticDataManager로 여러 테이블 관리](./06-static-data-manager.md) | [목차](../README.md) | [다음: 4.1 지원 타입 (Schemata) →](../04-advanced/01-schemata.md)
