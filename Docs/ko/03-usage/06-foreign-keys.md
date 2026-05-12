# 3.6 외래 키 (ForeignKey, SwitchForeignKey)

[3.5](./05-static-data-manager.md) 에서 카테고리 테이블과 아이템 테이블 두 개를 매니저로 로드했지만, 두 테이블 사이의 참조는 검증하지 않았습니다. 이 챕터는 그 참조를 자동으로 검증하도록 만드는 방법을 다룹니다.

Sdp 는 두 종류의 외래 키 Attribute 를 제공합니다.

- `[ForeignKey]` — 한 컬럼이 항상 같은 대상 테이블의 한 컬럼을 가리킬 때.
- `[SwitchForeignKey]` — 같은 컬럼이 다른 컬럼 값에 따라 가리키는 대상이 달라질 때.

두 Attribute 모두 같은 검증 흐름 (`LoadAsync` 안에서) 으로 처리됩니다.

## ForeignKey — 한 컬럼이 한 대상을 가리킨다

[3.5](./05-static-data-manager.md) 예제의 `Item.CategoryId` 가 `Categories.Id` 를 가리키도록 선언합니다.

```csharp
using Sdp.Attributes;

[StaticDataRecord("GameItems", "Categories")]
public sealed record ItemCategoryRecord(
    int Id,
    string Name,
    bool IsConsumable);

[StaticDataRecord("GameItems", "Items")]
public sealed record Item(
    int Id,
    string Name,
    [ForeignKey("Categories", "Id")] int CategoryId,
    int Price);
```

두 인자의 의미는 다음과 같습니다.

- 첫 번째 인자 `"Categories"` — **TableSet 의 속성 이름** (= TableSet record 의 파라미터 이름).
- 두 번째 인자 `"Id"` — **그 테이블 Record 의 파라미터 이름**.

검증 시 대상 테이블의 해당 컬럼 값을 모두 모아 `HashSet` 으로 한 번 만들어 두고, `CategoryId` 가 그 안에 있는지 확인합니다. 같은 (TableSet, Column) 조합은 이후 호출에서 캐시를 재사용합니다.

## 검증 단계

`LoadAsync` 의 FK 검증은 두 단계로 나뉘어 진행됩니다.

1. **스키마 단계 — `ForeignKeyTargetValidator`**
   - `tableSetName` 이 실제 TableSet 파라미터에 존재하는가? (`FkTargetNotFound`)
   - `recordColumnName` 이 대상 Record 의 파라미터에 존재하는가? (`IndexNotRegistered`)
   - 대상이 `[SingleColumnCollection]` 으로 묶인 컬럼은 아닌가? (`FkTargetIsSingleColumnCollection`)
2. **값 단계 — `ReferenceValidator`**
   - 실제 CSV 값이 대상 테이블의 해당 컬럼 값 집합 안에 있는가? (`FkValueNotFound`)

어느 단계든 실패하면 `AggregateException(Messages.FkValidationFailed, ...)` 에 모든 실패가 담겨 throw 됩니다.

## FK 가 어긋날 때

`Items` 시트에 존재하지 않는 `CategoryId=99` 인 행이 있다면 다음과 같이 떨어집니다.

```
AggregateException: FK 검증에 실패했습니다.
  - Item.CategoryId(99) 이(가) [Categories.Id] 중 어디에도 존재하지 않습니다.
```

여러 행이 어긋나 있으면 `InnerExceptions` 에 하나씩 담깁니다.

## 여러 ForeignKey — "한 곳에라도 있으면 유효"

같은 ID 체계를 여러 테이블이 나눠 갖는 경우가 있습니다. 예를 들어 `Reward.TargetId` 가 `Items` 또는 `Currencies` 어느 한쪽에 들어 있기만 하면 되는 상황입니다. `[ForeignKey]` 는 `AllowMultiple = true` 라서 같은 파라미터에 여러 번 붙일 수 있고, **둘 중 하나라도 일치하면 통과** 시킵니다.

```csharp
[StaticDataRecord("GameItems", "Currencies")]
public sealed record Currency(
    int Id,
    string Name);

[StaticDataRecord("GameItems", "Rewards")]
public sealed record Reward(
    int Id,
    [ForeignKey("Items", "Id")]
    [ForeignKey("Currencies", "Id")]
    int TargetId);
```

`Reward.TargetId` 가 `1` 이면, `Items.Id == 1` 또는 `Currencies.Id == 1` 어느 한쪽에 존재하면 됩니다. 두 테이블의 ID 체계가 완전히 분리되어 있어 겹치지 않는다는 보장이 있을 때만 깔끔하게 동작하는 패턴입니다.

## SwitchForeignKey — 조건에 따라 가리키는 대상이 달라진다

같은 컬럼 값이 **다른 컬럼의 값에 따라** 다른 테이블을 참조해야 할 때 `[SwitchForeignKey]` 를 씁니다. 예를 들어 보상 종류가 `"Item"` 일 때는 `Items.Id` 를 가리키고, `"Currency"` 일 때는 `Currencies.Id` 를 가리키도록 합니다.

```csharp
[StaticDataRecord("GameItems", "Rewards")]
public sealed record Reward(
    int Id,
    string Kind, // "Item" | "Currency"

    [SwitchForeignKey(nameof(Kind), "Item",     "Items",      "Id")]
    [SwitchForeignKey(nameof(Kind), "Currency", "Currencies", "Id")]
    int TargetId);
```

네 인자의 의미는 다음과 같습니다.

- `conditionColumnName` — 같은 Record 안의 어떤 컬럼이 분기 조건인지.
- `conditionValue` — 그 컬럼이 어떤 값일 때 이 FK 를 적용할지.
- `tableSetName` — 그 조건일 때 가리킬 TableSet 의 속성 이름.
- `recordColumnName` — 그 테이블 Record 의 파라미터 이름.

위 예제의 행별 검증 결과는 다음과 같이 정해집니다.

|Kind|TargetId|검사 대상|
|-|-|-|
|Item|10|Items.Id 에 10 이 있어야 함|
|Currency|5|Currencies.Id 에 5 가 있어야 함|

조건에 매칭되는 `SwitchForeignKey` 가 하나도 없으면 (`Kind` 가 `"Item"`, `"Currency"` 가 아닌 값이라면) 해당 행의 검증이 실패합니다.

## 두 테이블에 걸친 SFK 예제

좀 더 현실적인 묶음으로 보겠습니다. 보상 시트는 종류와 대상 ID 만 갖고, 종류에 따라 다른 데이터 테이블을 가리킵니다.

```csharp
[StaticDataRecord("GameItems", "Items")]
public sealed record Item(
    int Id,
    string Name,
    int Price);

[StaticDataRecord("GameItems", "Currencies")]
public sealed record Currency(
    int Id,
    string Name);

[StaticDataRecord("GameItems", "Rewards")]
public sealed record Reward(
    int Id,
    string Kind,

    [SwitchForeignKey(nameof(Kind), "Item",     "Items",      "Id")]
    [SwitchForeignKey(nameof(Kind), "Currency", "Currencies", "Id")]
    int TargetId,

    int Amount);

public sealed class ItemTable(ImmutableList<Item> records)
    : StaticDataTable<ItemTable, Item>(records);

public sealed class CurrencyTable(ImmutableList<Currency> records)
    : StaticDataTable<CurrencyTable, Currency>(records);

public sealed class RewardTable(ImmutableList<Reward> records)
    : StaticDataTable<RewardTable, Reward>(records);

public sealed class GameStaticData(ILogger<GameStaticData> logger)
    : StaticDataManager<GameStaticData.TableSet>(logger)
{
    public sealed record TableSet(
        ItemTable? Items,
        CurrencyTable? Currencies,
        RewardTable? Rewards);
}
```

CSV 가 다음과 같다고 합시다.

`GameItems.Items.csv`
```
Id,Name,Price
1,Potion,100
2,Sword,5000
```

`GameItems.Currencies.csv`
```
Id,Name
1,Gold
2,Gem
```

`GameItems.Rewards.csv`
```
Id,Kind,TargetId,Amount
1,Item,1,10
2,Currency,1,500
3,Item,2,1
4,Currency,9,100
```

`Rewards` 의 4번 행 `Currency / TargetId=9` 는 `Currencies.Id` 에 없으므로 검증이 실패합니다. 다른 세 행은 통과합니다.

## 정리

- `[ForeignKey(tableSet, column)]` — 단일 대상. 여러 번 붙이면 "어느 하나에 있으면 유효" 가 된다.
- `[SwitchForeignKey(conditionColumn, conditionValue, tableSet, column)]` — 분기 대상. 같은 파라미터에 여러 번 붙여 분기 표를 만든다.
- 검증은 `LoadAsync` 안에서 두 단계 — 스키마(타겟 존재 여부), 값(실제 참조 존재) — 로 처리되며, 실패는 `AggregateException` 으로 한 번에 통보된다.
- `disabledTables` 로 대상 테이블을 건너뛰면 그 테이블을 참조하는 FK 검증은 실패한다 — 독립된 그룹만 disable 하는 것이 안전하다.

`[ForeignKey]` 와 `[SwitchForeignKey]` 자체의 Attribute 명세 (오사용 진단, 다중 허용 등) 는 [4.2 Attribute 카탈로그](../04-advanced/02-attributes.md) 에 정리되어 있습니다.

---

[← 이전: 3.5 StaticDataManager 로 여러 테이블 관리](./05-static-data-manager.md) | [목차](../README.md) | [다음: 3.7 StaticDataView 로 사전 생성 뷰 합성 →](./07-static-data-view.md)
