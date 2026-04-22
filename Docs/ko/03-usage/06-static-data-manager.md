# 3.6 StaticDataManager로 여러 테이블 관리

여러 테이블을 한꺼번에 로드하고, 그 사이의 FK까지 검증하려면 `StaticDataManager` 가 필요합니다. 이 장에서는 [3.5](./05-complex-record.md) 의 `ItemCategory` + `Item` 을 묶어 실제로 돌려 봅니다.

## Manager 와 TableSet 정의

`StaticDataManager` 는 "어떤 테이블들을 다루는지" 를 `TableSet` 타입으로 선언받습니다. TableSet 은 **각 테이블을 파라미터로 받는 record** 로 두며, **Manager 클래스 내부에 함께 선언** 하는 것을 권장합니다 (필수는 아닙니다 — 외부에 따로 두어도 동작합니다).

```csharp
using Microsoft.Extensions.Logging;
using Sdp.Manager;

public sealed class GameStaticData(ILogger<GameStaticData> logger)
    : StaticDataManager<GameStaticData.TableSet>(logger)
{
    public sealed record TableSet(
        ItemCategoryTable? Categories,
        ItemTable? Items);

    public ItemCategoryTable Categories => Current.Categories!;
    public ItemTable Items => Current.Items!;
}
```

세 가지 규칙이 있습니다.

1. **TableSet record 의 파라미터 이름이 곧 TableSet 속성 이름** 이 된다. 이 이름이 FK 의 첫 번째 인자 (`[ForeignKey("Categories", "Id")]` 의 `"Categories"`) 와 일치해야 한다.
2. 각 테이블은 Nullable 로 선언한다. 뒤에서 설명하는 `disabledTables` 옵션으로 특정 테이블을 건너뛸 수 있기 때문이다.
3. **`StaticDataManager<TTableSet>` 는 `ILogger` 를 생성자 인자로 요구한다.** 서브클래스에서 base 로 그대로 전달한다. 로드 진행 상황 (전체 완료, 테이블별 완료) 이 이 로거로 기록된다.

외부에서 쓰는 쪽은 `Categories` / `Items` 로 접근합니다. `Current` 도 public 이라 외부에서 직접 꺼낼 수 있지만, 상속받은 Manager 안에서 의미 있는 이름으로 풀어 두면 호출부가 깔끔해집니다. Nullable 이라 `!` 를 붙였지만, `LoadAsync` 이후에는 disabled 로 지정하지 않은 테이블은 항상 값이 들어 있습니다.

## 로드와 조회

```csharp
var game = new GameStaticData(logger);
await game.LoadAsync("./csv");

// 단건 조회는 테이블 클래스에 UniqueIndex 를 두고 노출하는 식으로 구성한다 (3.2 참조).
foreach (var item in game.Items.Records)
{
    Console.WriteLine($"{item.Id} {item.Name}");
}
```

`LoadAsync` 는 다음 순서로 동작합니다.

1. **FK 타겟 검증** — TableSet 에 적힌 `[ForeignKey]` / `[SwitchForeignKey]` 의 `tableSetName` 이 실제 TableSet 파라미터에 존재하는지, 가리키는 컬럼이 대상 Record 에 있는지, 대상이 `[SingleColumnCollection]` 이 아닌 스칼라 컬럼인지 확인 (`ForeignKeyTargetValidator`).
2. TableSet 의 생성자 파라미터를 하나씩 훑어 각 테이블의 CSV 를 **병렬** 로드. 테이블별 로드 시간은 `Trace` 레벨로 기록된다.
3. 어느 하나라도 실패하면 모든 실패를 모아 `AggregateException(Messages.TablesFailedToLoad, ...)` 로 throw.
4. 모두 성공하면 TableSet 을 조립하고, 그 위에서 `ForeignKey` / `SwitchForeignKey` 의 실제 값을 검증 (`ReferenceValidator`).
5. 검증 실패가 있으면 다시 `AggregateException(Messages.FkValidationFailed, ...)` 으로 throw.
6. 모두 통과하면 `Current` 에 세팅하고, 전체 로드 시간을 `Information` 레벨로 기록한다.

## FK가 어긋날 때

`Items.Items.csv` 에 존재하지 않는 `CategoryId=99` 인 행이 들어 있다면, `LoadAsync` 는 다음과 비슷한 메시지와 함께 예외를 던집니다.

```
AggregateException: FK 검증에 실패했습니다.
  - Item.CategoryId(99)이(가) [Categories.Id] 중 어디에도 존재하지 않습니다.
```

여러 행이 어긋나 있으면 `InnerExceptions` 에 하나씩 담깁니다. 로드가 실패한 경우 `Current` 는 갱신되지 않고 직전 상태가 유지됩니다 (첫 로드 중이라면 아직 `null`).

## 특정 테이블 건너뛰기

CI 에서 일부 테이블만 빠르게 검증하고 싶거나, 개발 서버에서 아직 준비되지 않은 테이블을 임시로 제외하고 싶을 때 `disabledTables` 를 씁니다.

```csharp
await game.LoadAsync("./csv", disabledTables: ["Items"]);
```

- 파라미터 이름 기준으로 매칭합니다. `"Items"` 는 TableSet 의 파라미터 `Items` 에 대응합니다.
- 해당 테이블은 로드되지 않고 TableSet 속성에 `null` 이 들어갑니다.
- 해당 테이블을 대상으로 한 FK 검증은 *대상 테이블이 맵에 없으므로* 실패할 수 있습니다. 즉, "disabled 된 테이블을 참조하는 FK" 는 오류가 됩니다. 따라서 실제로는 서로 독립된 그룹 단위로만 disable 하는 것이 안전합니다.

## 로드 시간 측정

`StaticDataManager` 는 주입받은 `ILogger` 로 두 단계를 기록합니다.

- 테이블별 로드 완료 — `Trace` 레벨, 메시지 키 `LoadedTable` (`테이블 {Name} 로드 완료 ({ElapsedMs}ms)`).
- 전체 `LoadAsync` 완료 — `Information` 레벨, 메시지 키 `LoadAsyncCompleted` (`LoadAsync 완료 ({ElapsedMs}ms)`).

각 테이블 로드 시간을 따로 보고 싶다면 호스트 측에서 최소 로그 레벨을 `Trace` 로 낮춥니다. 정상 시 운영 로그를 깔끔하게 유지하려면 `Information` 으로 두면 전체 완료 시간만 남습니다. FK 검증 자체는 별도 로그를 남기지 않고, 실패 시 예외로만 통보합니다.

## 매니저 수준의 추가 검증

`StaticDataManager.Validate(TTableSet)` 을 override 하면 모든 FK 검증이 끝난 뒤 한 번 호출됩니다. 테이블 간 교차 제약 (예: "무기 카테고리의 아이템은 최소 한 개는 있어야 한다") 처럼 개별 테이블에서 표현하기 어려운 규칙을 여기에서 점검합니다.

```csharp
public sealed class GameStaticData(ILogger<GameStaticData> logger)
    : StaticDataManager<GameStaticData.TableSet>(logger)
{
    public sealed record TableSet(
        ItemCategoryTable? Categories,
        ItemTable? Items);

    public ItemCategoryTable Categories => Current.Categories!;
    public ItemTable Items => Current.Items!;

    protected override void Validate(TableSet tableSet)
    {
        var weaponCategoryId = tableSet.Categories!.Records
            .First(c => c.Name == "Weapon").Id;

        var hasAnyWeapon = tableSet.Items!.Records
            .Any(i => i.CategoryId == weaponCategoryId);

        if (!hasAnyWeapon)
        {
            throw new InvalidOperationException("무기 카테고리의 아이템이 하나도 없습니다.");
        }
    }
}
```

## LoadAsync 의 동시성

`LoadAsync` 는 동시 진입을 가드합니다. 이미 로딩 중일 때 다른 스레드에서 호출하면 즉시 `InvalidOperationException` 으로 거부됩니다. 로딩이 끝난 뒤의 `Current` 교체는 `volatile` 필드 한 번의 쓰기로 이루어지므로, 조회 측이 부분 갱신된 중간 상태를 보는 일은 없습니다.

## 일관된 스냅샷으로 조회하기

`Current` 는 호출할 때마다 그 시점의 `current` 필드 값을 반환합니다. 첫 로드가 완료된 직후이거나, 응용 측이 어떤 이유로 `LoadAsync` 를 다시 호출하는 흐름이라면 호출 시점에 따라 스냅샷이 달라질 수 있으므로, 한 작업 안에서 `Current` 를 여러 번 호출하지 않는 것이 안전합니다.

```csharp
// 위험 - 두 줄 사이에 LoadAsync 가 새 값을 세팅하면 categories 와 items 가 서로 다른 버전을 본다
var categories = game.Current.Categories!;
var items = game.Current.Items!;
```

같은 작업에서 여러 테이블을 함께 다룰 때는 `Current` 를 변수에 한 번만 받아 쓰는 것이 안전합니다.

```csharp
// 안전 - 한 스냅샷에서 두 테이블을 함께 꺼낸다
var snapshot = game.Current;
var categories = snapshot.Categories!;
var items = snapshot.Items!;
```

Manager 가 외부에 노출하는 편의 프로퍼티 (`Categories`, `Items`) 도 내부에서 `Current.X` 를 매번 호출하므로 같은 주의가 필요합니다. 한 테이블만 보는 짧은 조회라면 굳이 변수에 받지 않아도 됩니다.

## ASP.NET Core 에서 DI 로 사용

요청 단위 수명이 있는 환경에서는 **Manager 는 Singleton, 각 테이블은 Scoped** 로 등록하는 패턴을 권장합니다. 컨트롤러나 핸들러는 `GameStaticData` 전체가 아니라 필요한 테이블만 직접 주입받습니다.

```csharp
// Program.cs
services.AddSingleton<GameStaticData>();

services.AddScoped<ItemCategoryTable>(sp =>
    sp.GetRequiredService<GameStaticData>().Current.Categories!);

services.AddScoped<ItemTable>(sp =>
    sp.GetRequiredService<GameStaticData>().Current.Items!);
```

```csharp
public sealed class ItemController(
    ItemCategoryTable categories,
    ItemTable items) : ControllerBase
{
    [HttpGet("{id:int}")]
    public IActionResult Get(int id)
    {
        // categories 와 items 는 이 요청에 묶인 인스턴스로 고정되어 있다
        ...
    }
}
```

Scoped 가 핵심입니다. Singleton 으로 등록하면 첫 해석 시점의 스냅샷이 영구히 고정되고, Transient 로 등록하면 한 요청 안에서 호출마다 다시 해석되어 일관성이 깨집니다.

`LoadAsync` 자체는 Manager 가 Singleton 이라 어디서든 같은 인스턴스에서 부르면 됩니다. 일반적으로 호스팅 시작 시 한 번 호출합니다.

## 이번 장까지의 정리

- Record 와 Excel 이 대응한다 (3.1, 3.3).
- Record 하나당 `StaticDataTable` 하나를 만든다 (3.2).
- Record 가 복잡해져도 Attribute 로 대부분 표현할 수 있다 (3.5).
- 여러 테이블은 `StaticDataManager` 로 묶고 FK 는 자동 검증된다 (3.6).

여기까지가 Sdp 를 실무에서 쓸 때 필요한 80 % 입니다. 나머지 20 % — 지원 타입의 세부 사항, Attribute 전체 목록 — 은 고급 기능 장에서 다룹니다.

---

[← 이전: 3.5 복잡한 Record](./05-complex-record.md) | [목차](../README.md) | [다음: 3.7 StaticDataView로 사전계산 뷰 합성 →](./07-static-data-view.md)
