# 3.7 StaticDataView 로 사전 생성 뷰 합성

[3.5](./05-static-data-manager.md) 까지의 매니저는 테이블을 그대로 노출합니다. 실무에서는 "여러 테이블을 조인한 결과", "특정 키로 그룹핑한 합계" 처럼 **테이블 위에서 한 번 만들어 두면 좋은 결과** 가 자주 필요합니다. 이런 결과를 매 조회마다 다시 만드는 대신 **로드 시점에 한 번 생성해 두는** 합성 레이어가 `StaticDataView` 입니다.

## 언제 쓰나

- 두 개 이상의 테이블을 조인한 결과를 자주 조회한다.
- FK 로 묶인 자식 행들을 부모 키로 미리 그룹핑해 두고 싶다.
- 합계, 필터링 같은 가공이 매번 같은 입력에 대해 반복된다.

뷰는 read-only 입니다. `LoadAsync` 한 번에 TableSet 과 ViewSet 이 함께 만들어지고, 한 번 생성된 뷰는 다음 `LoadAsync` 가 새 스냅샷으로 교체될 때까지 그대로 유지됩니다.

> **뷰에는 FK, SwitchForeignKey, Validate 가 없습니다.** 뷰는 이미 검증을 통과한 TableSet 을 입력으로 받아 가공한 결과일 뿐이며, Sdp 의 검증 흐름은 모두 테이블 단에서 끝납니다. 데이터 무결성에 관한 규칙은 Record 와 Table 쪽에 모아 두고, 뷰는 그 위에서 생성만 합니다.

## ViewSet 정의

ViewSet 은 TableSet 과 같은 모양 — **각 뷰를 파라미터로 받는 record** — 입니다.

```csharp
using Microsoft.Extensions.Logging;
using Sdp.Manager;

public sealed class GameStaticData(ILogger<GameStaticData> logger)
    : StaticDataManager<GameStaticData.TableSet, GameStaticData.ViewSet>(logger)
{
    public sealed record TableSet(
        EventTable? EventTable,
        WeaponTable? WeaponTable,
        ArmorTable? ArmorTable);

    public sealed record ViewSet(
        EventBundleView EventBundleView,
        EventAttackTotalView EventAttackTotalView);
}
```

세 가지 규칙입니다.

1. **2-제네릭 베이스 사용** — TableSet 만 다루는 매니저는 `StaticDataManager<TTableSet>` 이지만, 뷰까지 합성하려면 `StaticDataManager<TTableSet, TViewSet>` 베이스를 상속한다. `ILogger` 를 그대로 base 로 전달하는 부분은 동일.
2. **각 뷰 파라미터는 non-nullable** — TableSet 은 `disabledTables` 옵션으로 일부가 빠질 수 있어 nullable 이지만, ViewSet 은 빌더가 모든 슬롯을 항상 채우므로 nullable 로 선언하면 `ViewSetMemberMustBeNonNullable` 로 거부된다.
3. **`Current` 한 곳만 외부에 노출한다.** `Current` 는 `TableAndViewSet(Tables, Views)` 묶음을 가리키며, 한 묶음으로 atomic 하게 교체된다. [3.5](./05-static-data-manager.md) 에서 봤듯이 매니저 서브클래스에서 뷰별 편의 프로퍼티를 풀어 두지 않는다 — 호출부에서 `var tables = staticData.Current;` 로 한 번 받아 그 안의 `Tables` / `Views` 로 꺼낸다.

## StaticDataView 구현

뷰는 CRTP 로 자기 자신을 첫 번째 인자에 넣고, **TableSet 하나만 받는 생성자** 를 가집니다. 생성자 안에서 필요한 인덱스, 집계를 미리 만들어 두는 것이 표준 패턴입니다.

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
        var weaponsByEvent = new MultiIndex<WeaponRecord, int>(t.WeaponTable!.Records, x => x.EventId);
        var armorsByEvent = new MultiIndex<ArmorRecord, int>(t.ArmorTable!.Records, x => x.EventId);

        var bundles = new List<EventBundle>();
        foreach (var ev in t.EventTable!.Records)
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
3. **생성자 안에서 빌드를 끝낸다** — 외부에 가변 상태를 두지 않고, 조회용 인덱스 (`UniqueIndex`, `MultiIndex`) 와 사전 생성된 컬렉션만 readonly 로 보관한다.

## disabledTables 와의 관계

위 `EventBundleView` 는 생성자 안에서 `t.EventTable`, `t.WeaponTable`, `t.ArmorTable` 을 모두 `!` 로 꺼냅니다. 이 셋 중 하나라도 [3.5](./05-static-data-manager.md#특정-테이블-건너뛰기) 의 `disabledTables` 옵션으로 비활성화되어 있다면, 빌드 도중 `NullReferenceException` 이 발생하고 **그 시도 전체가 `ViewsFailedToBuild` 안에 모입니다.**

따라서 다음 두 가지 중 하나로 정리합니다.

- **뷰가 의존하는 테이블은 항상 함께 로드한다.** disable 그룹을 짤 때 뷰의 의존성도 함께 빠지도록 묶는다.
- 어떤 테이블이 disable 됐을 때도 빌드를 통과시키고 싶다면 **뷰 생성자 안에서 `null` 분기를 명시** 한다. 예를 들어 `WeaponTable` 이 빠질 수 있는 환경이라면, 그 자리를 빈 시퀀스로 대체해 뷰가 빈 결과를 노출하도록 둘 수 있습니다.

```csharp
private static UniqueIndex<EventBundle, int> Build(GameStaticData.TableSet t)
{
    var weaponRecords = t.WeaponTable?.Records ?? ImmutableList<WeaponRecord>.Empty;
    var armorRecords = t.ArmorTable?.Records ?? ImmutableList<ArmorRecord>.Empty;

    var weaponsByEvent = new MultiIndex<WeaponRecord, int>(weaponRecords, x => x.EventId);
    var armorsByEvent = new MultiIndex<ArmorRecord, int>(armorRecords, x => x.EventId);

    var bundles = new List<EventBundle>();
    if (t.EventTable is not null)
    {
        foreach (var ev in t.EventTable.Records)
        {
            // 무기/방어구 테이블이 disable 되어 있어도 weaponsByEvent.Get(ev.Id) 는 빈 시퀀스를 돌려준다
            ...
        }
    }

    return new UniqueIndex<EventBundle, int>(bundles, b => b.Event.Id);
}
```

이런 분기는 "이 뷰가 어떤 환경에서도 빈 결과로 살아남아야 한다" 는 의도가 분명할 때만 유효합니다. 기본 권장은 첫 번째 — 뷰가 의존하는 테이블은 함께 로드한다 — 입니다. 뷰의 존재 자체가 "이 테이블들이 모두 있다" 는 가정에 깔리는 게 자연스럽습니다.

## 로드 흐름과 빌드 단계

`LoadAsync` 흐름은 [3.5](./05-static-data-manager.md) 의 단일 제네릭 매니저와 거의 같고, **검증을 모두 통과한 뒤 ViewSet 빌드 단계가 추가** 됩니다.

1. FK 타겟 검증.
2. TableSet 병렬 로드 (테이블별 `Trace` 로그 `LoadedTable`).
3. 실제 FK 값 검증.
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

호출부 패턴은 [3.5](./05-static-data-manager.md#일관된-스냅샷으로-조회하기) 와 동일합니다. 같은 작업 안에서 여러 테이블이나 뷰를 함께 다룰 때는 **`Current` 를 변수에 한 번만 받아** 그 안에서 꺼내 씁니다.

```csharp
var snapshot = staticData.Current;
var bundle = snapshot.Views.EventBundleView.Get(1);
var events = snapshot.Tables.EventTable!.Records;
```

## ASP.NET Core 에서 DI 로 사용

[3.5 의 등록 패턴](./05-static-data-manager.md#aspnet-core-에서-di-로-사용) 과 동일한 방식으로, Snapshot 을 Scoped 로 묶고 그 안에서 각 테이블과 뷰를 풀어 줍니다. Manager 는 Singleton.

```csharp
// Program.cs
services.AddSingleton<GameStaticData>();

// 한 요청 안에서 같은 (Tables, Views) 묶음을 공유한다
services.AddScoped(sp =>
    sp.GetRequiredService<GameStaticData>().Current);

// 각 테이블
services.AddScoped<EventTable>(sp =>
    sp.GetRequiredService<GameStaticData.TableAndViewSet>().Tables.EventTable!);

services.AddScoped<WeaponTable>(sp =>
    sp.GetRequiredService<GameStaticData.TableAndViewSet>().Tables.WeaponTable!);

services.AddScoped<ArmorTable>(sp =>
    sp.GetRequiredService<GameStaticData.TableAndViewSet>().Tables.ArmorTable!);

// 각 View (ViewSet 멤버는 non-nullable 이라 ! 가 없다)
services.AddScoped<EventBundleView>(sp =>
    sp.GetRequiredService<GameStaticData.TableAndViewSet>().Views.EventBundleView);

services.AddScoped<EventAttackTotalView>(sp =>
    sp.GetRequiredService<GameStaticData.TableAndViewSet>().Views.EventAttackTotalView);
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

응용 측에서는 보통 `GameStaticData` 전체나 `TableSet` 전체를 받지 않고, 실제로 쓰는 테이블, View 만 골라 주입받는 편이 호출부가 깔끔합니다.

## TableSet 만 쓰는 경우

뷰 합성이 필요 없으면 [3.5](./05-static-data-manager.md) 의 `StaticDataManager<TTableSet>` 를 그대로 사용하면 됩니다. 두 베이스는 별도로 존재하며, 뷰가 필요해진 시점에 2-제네릭 베이스로 옮기는 식으로 점진적으로 확장할 수 있습니다.

## 요약

- `StaticDataManager<TTableSet, TViewSet>` 가 뷰 합성을 담당한다.
- 뷰는 `StaticDataView<TSelf, TTableSet>` 를 상속하고 TableSet 한 개를 받는 생성자에서 빌드를 끝낸다.
- ViewSet 은 non-nullable 뷰들을 가진 record 로 두고, 호출부는 `Current.Views` 로 접근한다.
- TableSet 과 ViewSet 은 한 묶음으로 atomic 교체되므로 조회는 항상 정합한 묶음을 본다.
- 뷰는 검증 대상이 아니다. FK, SwitchForeignKey, Validate 는 모두 테이블 단에서 끝난다.

---

[← 이전: 3.6 외래 키](./06-foreign-keys.md) | [목차](../README.md) | [다음: 4.1 지원 타입 (Schemata) →](../04-advanced/01-schemata.md)
