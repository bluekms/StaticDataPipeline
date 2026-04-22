# 3.6 StaticDataManager로 여러 테이블 관리

여러 테이블을 한꺼번에 로드하고, 그 사이의 FK까지 검증하려면 `StaticDataManager` 가 필요합니다. 이 장에서는 세트 B의 `ItemCategory` + `Item` 을 묶어 실제로 돌려 봅니다.

## TableSet 정의

`StaticDataManager` 는 "어떤 테이블들을 다루는지" 를 `TableSet` 타입으로 선언받습니다. TableSet은 **각 테이블을 파라미터로 받는 record** 로 두는 것을 권장합니다.

```csharp
public sealed record GameTableSet(
    ItemCategoryTable? Categories,
    ItemTable? Items);
```

두 가지 규칙이 있습니다.

1. **파라미터 이름이 곧 TableSet 속성 이름** 이 된다. 이 이름이 FK의 첫 번째 인자(`[ForeignKey("Categories", "Id")]` 의 `"Categories"`)와 일치해야 한다.
2. 각 테이블은 Nullable 로 선언한다. 뒤에서 설명하는 `disabledTables` 옵션으로 특정 테이블을 건너뛸 수 있기 때문이다.

## Manager 클래스

```csharp
using Sdp.Manager;

public sealed class GameStaticData : StaticDataManager<GameTableSet>
{
    public ItemCategoryTable Categories => Current.Categories!;
    public ItemTable Items => Current.Items!;
}
```

외부에서 쓰는 쪽은 `Categories` / `Items` 로 접근합니다. `Current` 는 `protected` 이므로 상속받은 곳에서만 풀어서 노출합니다. Nullable 이라 `!` 를 붙였지만, `LoadAsync` 이후에는 disabled로 지정하지 않은 테이블은 항상 값이 들어 있습니다.

## 로드와 조회

```csharp
var game = new GameStaticData();
await game.LoadAsync("./csv");

var sword = game.Items.Get(1);
var category = game.Categories.Get(sword.CategoryId);

Console.WriteLine($"{sword.Name} ({category.Name})");
```

`LoadAsync` 는 다음 순서로 동작합니다.

1. TableSet의 생성자 파라미터를 하나씩 훑어 각 테이블의 `CreateAsync(csvDir)` 를 **병렬** 호출.
2. 어느 하나라도 실패하면 모든 실패를 모아 `AggregateException(Messages.TablesFailedToLoad, ...)` 로 throw.
3. 모두 성공하면 TableSet을 조립하고, 그 위에서 `ForeignKey` / `SwitchForeignKey` 를 검증.
4. 검증 실패가 있으면 다시 `AggregateException` 으로 throw.
5. 모두 통과하면 `Current` 에 세팅.

## FK가 어긋날 때

세트 B의 `Items.Items.csv` 에 존재하지 않는 `CategoryId=99` 인 행이 들어 있다면, `LoadAsync` 는 다음과 비슷한 메시지와 함께 예외를 던집니다.

```
AggregateException: 외래 키 검증 실패
  - Item.CategoryId='99' 는 (Categories.Id) 에 존재하지 않습니다.
```

여러 행이 어긋나 있으면 `InnerExceptions` 에 하나씩 들어갑니다. 로드 자체가 실패하므로 `Current` 는 갱신되지 않고, 직전 상태가 유지됩니다 (첫 로드라면 아직 `null` 인 상태).

## 특정 테이블 건너뛰기

CI에서 일부 테이블만 빠르게 검증하고 싶거나, 개발 서버에서 아직 준비되지 않은 테이블을 임시로 제외하고 싶을 때 `disabledTables` 를 씁니다.

```csharp
await game.LoadAsync("./csv", disabledTables: ["Items"]);
```

- 파라미터 이름 기준으로 매칭합니다. `"Items"` 는 `GameTableSet` 의 파라미터 `Items` 에 대응합니다.
- 해당 테이블은 `CreateAsync` 를 호출하지 않고 TableSet 속성에 `null` 이 들어갑니다.
- 해당 테이블을 대상으로 한 FK 검증은 *대상 테이블이 맵에 없으므로* 실패할 수 있습니다. 즉, "disabled 된 테이블을 참조하는 FK" 는 오류가 됩니다. 따라서 실제로는 서로 독립된 그룹 단위로만 disable 하는 것이 안전합니다.

## 매니저 수준의 추가 검증

`StaticDataManager.Validate(TTableSet)` 을 override 하면 모든 FK 검증이 끝난 뒤 한 번 호출됩니다. 테이블 간 교차 제약(예: "무기 카테고리의 아이템은 최소 한 개는 있어야 한다") 처럼 개별 테이블에서 표현하기 어려운 규칙을 여기에서 점검합니다.

```csharp
public sealed class GameStaticData : StaticDataManager<GameTableSet>
{
    public ItemCategoryTable Categories => Current.Categories!;
    public ItemTable Items => Current.Items!;

    protected override void Validate(GameTableSet tableSet)
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

## 재로드

`LoadAsync` 를 다시 호출하면 새로운 TableSet을 만들어 `Current` 를 교체합니다. `Current` 는 `volatile` 필드에 담기므로 다른 스레드에서 동시에 조회 중이어도 교체 시점의 경합이 없습니다. 단, 로드 **도중** 에 조회가 실행되면 로드 전의 이전 스냅샷이 반환됩니다. 로드 트랜잭션 동안 조회를 막고 싶다면 상위 레이어에서 별도의 락을 씁니다.

## 이번 장까지의 정리

- Record 와 Excel이 대응한다 (3.1, 3.3).
- Record 하나당 `StaticDataTable` 하나를 만든다 (3.2).
- Record가 복잡해져도 Attribute로 대부분 표현할 수 있다 (3.5).
- 여러 테이블은 `StaticDataManager` 로 묶고 FK는 자동 검증된다 (3.6).

여기까지가 Sdp를 실무에서 쓸 때 필요한 80 % 입니다. 나머지 20 % — 지원 타입의 세부 사항, Attribute 전체 목록, 검증 타이밍 — 은 고급 기능 장에서 다룹니다.

---

[← 이전: 3.5 복잡한 Record](./05-complex-record.md) | [목차](../README.md) | [다음: 4.1 지원 타입 (Schemata) →](../04-advanced/01-schemata.md)
