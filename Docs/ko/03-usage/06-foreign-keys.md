# 3.6 외래 키 (ForeignKey, SwitchForeignKey)

[3.5](./05-static-data-manager.md) 에서 카테고리 테이블과 아이템 테이블 두 개를 StaticDataManager 로 로드했지만, 두 테이블 사이의 참조는 검증하지 않았습니다. 이 챕터는 그 참조를 자동으로 검증하도록 만드는 방법을 다룹니다.

Sdp 는 두 종류의 외래 키 Attribute 를 제공합니다.

- `[ForeignKey]` — 한 컬럼이 항상 같은 대상 테이블의 한 컬럼을 가리킬 때.
- `[SwitchForeignKey]` — 같은 컬럼이 다른 컬럼 값에 따라 가리키는 대상이 달라질 때.

두 Attribute 모두 같은 흐름으로 처리됩니다. 선언의 구조는 빌드 시점에 소스 제너레이터가, 실제 참조 값은 `LoadAsync` 가 검증합니다.

## ForeignKey — 한 컬럼이 한 대상을 가리킨다

[3.5](./05-static-data-manager.md) 예제의 `ItemRecord.CategoryId` 가 `CategoryTable` 의 `Id` 를 가리키도록 선언합니다.

```csharp
using Sdp.Attributes;

[StaticDataRecord("GameItems", "Categories")]
public sealed partial record CategoryRecord(
    int Id,
    string Name,
    bool IsConsumable);

[StaticDataRecord("GameItems", "Items")]
public sealed partial record ItemRecord(
    int Id,
    string Name,
    [ForeignKey("CategoryTable", "Id")] int CategoryId,
    int Price);
```

두 인자의 의미는 다음과 같습니다.

- 첫 번째 인자 `"CategoryTable"` — **TableSet 의 속성 이름** (= TableSet record 의 파라미터 이름).
- 두 번째 인자 `"Id"` — **그 테이블 Record 의 파라미터 이름**.

검증 시 `CategoryId` 가 대상 테이블 안에 있는지 확인합니다.

</br></br></br>

## 검증 단계

ForeignKey 검증은 두 단계로 나뉩니다.

1. **빌드 시점 — 소스 제너레이터가 Record/TableSet 선언을 분석해 진단으로 보고**
   - `tableSetName` 이 실제 TableSet 파라미터에 존재하는가? (`SDP0205`)
   - `recordColumnName` 이 대상 Record 의 파라미터에 존재하는가? (`SDP0206`)
   - 대상이 `[SingleColumnCollection]` 으로 묶인 컬럼은 아닌가? (`SDP0207`)
   - 참조 컬럼과 대상 컬럼의 타입이 같은가? (`SDP0211`)
   - 같은 파라미터에 `[ForeignKey]` 와 `[SwitchForeignKey]` 가 함께 붙어 있지 않은가? (`SDP0204`)
   - 같은 파라미터에 같은 대상이 두 번 붙어 있지 않은가? (`SDP0226`)
   - `[SwitchForeignKey]` 의 조건 컬럼이 같은 Record 안에 존재하는가? (`SDP0209`)
   - `[SwitchForeignKey]` 의 같은 조건이 두 번 이상 등장하지 않는가? (`SDP0208`, 표기만 다른 같은 값은 `SDP0225`)
2. **로드 시점 — `LoadAsync` 가 실제 값을 확인**
   - 대상 테이블이 `disabledTables` 로 빠지지 않았는가? (`FkTargetNotFound`)
   - 실제 CSV 값이 대상 테이블의 해당 컬럼 값 집합 안에 있는가? (`FkValueNotFound`)
   - `[SwitchForeignKey]` 의 조건 컬럼 값이 분기 조건 중 하나에 매칭되는가? (`SwitchFkConditionValueNotMatched`)

빌드 시점 진단은 모두 오류라 그 자리에서 빌드가 멈춥니다. 로드 시점 실패는 모든 테이블의 실패를 모아 `AggregateException(Messages.FkValidationFailed, ...)` 로 한 번에 던집니다.

</br></br></br>

## FK 오류를 머지 전에 잡으려면

위 표에서 보듯 **실제 참조 값이 존재하는지는 런타임 `LoadAsync` 에서만 검사** 됩니다. `ExcelColumnExtractor` 는 시트 하나씩 처리하므로 다른 시트의 값을 알지 못하고, 소스 제너레이터는 선언만 봅니다. 아무것도 하지 않으면 데이터 작업자가 잘못 적은 참조 ID 는 서버나 클라이언트를 실제로 띄울 때 처음 드러납니다.

그 시점을 앞당기는 방법은 하나입니다. **추출된 CSV 로 `LoadAsync` 를 한 번 돌리는 테스트** 를 두고 CI 에서 실행합니다.

```csharp
[Fact]
public async Task LoadAsync_WithExtractedCsv_Succeeds()
{
    var staticData = new GameStaticData(NullLogger<GameStaticData>.Instance);

    await staticData.LoadAsync("./csv");

    Assert.NotNull(staticData.Current.ItemTable);
}
```

- `./csv` 는 같은 CI 실행에서 추출기가 방금 만든 폴더를 가리키게 합니다. 추출 스텝 → 빌드 스텝 → 이 테스트 순서로 두면 Excel 변경과 Record 변경 어느 쪽도 FK 를 깨뜨린 채 머지되지 않습니다.
- 실패하면 `AggregateException.InnerExceptions` 에 어긋난 행이 하나씩 담기므로 (아래 절), 테스트 출력만으로 어느 시트의 어느 값을 고쳐야 하는지 알 수 있습니다.
- 이 테스트는 FK 뿐 아니라 `Validate` 교차 검증과 뷰 빌드 ([3.7](./07-static-data-view.md)) 까지 함께 거칩니다. 런타임에서만 검사되는 것 전부가 한 번에 걸립니다.

</br></br></br>

## ForeignKey 가 어긋날 때

`Items` 시트에 존재하지 않는 `CategoryId=99` 인 행이 있다면 다음과 같이 떨어집니다.

```
AggregateException: FK 검증에 실패했습니다.
  - ItemRecord.CategoryId(99)이(가) [CategoryTable.Id] 중 어디에도 존재하지 않습니다.
```

여러 행이 어긋나 있으면 `InnerExceptions` 에 하나씩 담깁니다.

</br></br></br>

## 여러 ForeignKey — "한 곳에라도 있으면 유효"

같은 ID 체계를 여러 테이블이 나눠 갖는 경우가 있습니다. 예를 들어 `RewardRecord.TargetId` 가 `ItemTable` 또는 `CurrencyTable` 어느 한쪽에 들어 있기만 하면 되는 상황입니다. `[ForeignKey]` 는 `AllowMultiple = true` 라서 같은 파라미터에 여러 번 붙일 수 있고, **둘 중 하나라도 일치하면 통과** 시킵니다.

```csharp
[StaticDataRecord("GameItems", "Currencies")]
public sealed partial record CurrencyRecord(
    int Id,
    string Name);

[StaticDataRecord("GameItems", "Rewards")]
public sealed partial record RewardRecord(
    int Id,
    [ForeignKey("ItemTable", "Id")]
    [ForeignKey("CurrencyTable", "Id")]
    int TargetId);
```

`RewardRecord.TargetId` 가 `1` 이면, `ItemTable.Id == 1` 또는 `CurrencyTable.Id == 1` 어느 한쪽에 존재하면 됩니다. 두 테이블의 ID 체계가 완전히 분리되어 있어 겹치지 않는다는 보장이 있을 때만 깔끔하게 동작하는 패턴입니다.

</br></br></br>

## SwitchForeignKey — 조건에 따라 가리키는 대상이 달라진다

같은 컬럼 값이 **다른 컬럼의 값에 따라** 다른 테이블을 참조해야 할 때 `[SwitchForeignKey]` 를 씁니다. 예를 들어 보상 종류가 `Item` 일 때는 `ItemTable.Id` 를 가리키고, `Currency` 일 때는 `CurrencyTable.Id` 를 가리키도록 합니다.

```csharp
public enum RewardKind
{
    Item,
    Currency,
}

[StaticDataRecord("GameItems", "Rewards")]
public sealed partial record RewardRecord(
    int Id,
    RewardKind Kind,

    [SwitchForeignKey(nameof(Kind), "Item",     "ItemTable",     "Id")]
    [SwitchForeignKey(nameof(Kind), "Currency", "CurrencyTable", "Id")]
    int TargetId);
```

네 인자의 의미는 다음과 같습니다.

- `conditionColumnName` — 같은 Record 안의 어떤 컬럼이 분기 조건인지.
- `conditionValue` — 그 컬럼이 어떤 값일 때 이 SwitchForeignKey 를 적용할지.
- `tableSetName` — 그 조건일 때 가리킬 TableSet 의 속성 이름.
- `recordColumnName` — 그 테이블 Record 의 파라미터 이름.

위 예제의 행별 검증 결과는 다음과 같이 정해집니다.

|Kind|TargetId|검사 대상|
|-|-|-|
|Item|10|ItemTable.Id 에 10 이 있어야 함|
|Currency|5|CurrencyTable.Id 에 5 가 있어야 함|

조건에 매칭되는 `SwitchForeignKey` 가 하나도 없으면 (`Kind` 가 `Item`, `Currency` 가 아닌 값이라면) 해당 행의 검증이 실패합니다.

같은 `(conditionColumnName, conditionValue)` 조합을 가진 `SwitchForeignKey` 를 한 Record 안에 두 번 이상 붙이면 추출 단계와 빌드 시점 (`SDP0208`) 양쪽에서 거부됩니다. 같은 조건에 대한 대상 테이블은 정확히 하나여야 하므로, 분기 표를 자기 자신과 충돌하게 작성한 경우 그 자리에서 잡습니다.

</br></br></br>

## 두 테이블에 걸친 SwitchForeignKey 예제

좀 더 현실적인 묶음으로 보겠습니다. 보상 시트는 종류와 대상 ID 만 갖고, 종류에 따라 다른 데이터 테이블을 가리킵니다.

```csharp
[StaticDataRecord("GameItems", "Items")]
public sealed partial record ItemRecord(
    int Id,
    string Name,
    int Price);

[StaticDataRecord("GameItems", "Currencies")]
public sealed partial record CurrencyRecord(
    int Id,
    string Name);

[StaticDataRecord("GameItems", "Rewards")]
public sealed partial record RewardRecord(
    int Id,
    RewardKind Kind,

    [SwitchForeignKey(nameof(Kind), "Item",     "ItemTable",     "Id")]
    [SwitchForeignKey(nameof(Kind), "Currency", "CurrencyTable", "Id")]
    int TargetId,

    int Amount);

public sealed partial class ItemTable(ImmutableArray<ItemRecord> records)
    : StaticDataTable<ItemRecord>(records);

public sealed partial class CurrencyTable(ImmutableArray<CurrencyRecord> records)
    : StaticDataTable<CurrencyRecord>(records);

public sealed partial class RewardTable(ImmutableArray<RewardRecord> records)
    : StaticDataTable<RewardRecord>(records);

public sealed partial class GameStaticData(ILogger<GameStaticData> logger)
    : StaticDataManager<GameStaticData.TableSet>(logger)
{
    public sealed partial record TableSet(
        ItemTable? ItemTable,
        CurrencyTable? CurrencyTable,
        RewardTable? RewardTable);
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

`Rewards` 의 4번 행 `Currency / TargetId=9` 는 `CurrencyTable.Id` 에 없으므로 검증이 실패합니다. 다른 세 행은 통과합니다.

</br></br></br>

## 정리

- `[ForeignKey(tableSet, column)]` — 단일 대상. 여러 번 붙이면 "어느 하나에 있으면 유효" 가 된다.
- `[SwitchForeignKey(conditionColumn, conditionValue, tableSet, column)]` — 분기 대상. 같은 파라미터에 여러 번 붙여 분기 표를 만든다.
- 선언의 구조 (타겟 테이블·컬럼 존재, 타입 일치, 조건 중복) 는 빌드 시점 진단으로, 실제 참조 값은 `LoadAsync` 에서 검증되며, 로드 시점 실패는 `AggregateException` 으로 한 번에 통보된다.
- `disabledTables` 로 대상 테이블을 건너뛰면 그 테이블을 참조하는 ForeignKey 검증은 실패한다 — 독립된 그룹만 disable 하는 것이 안전하다.

`[ForeignKey]` 와 `[SwitchForeignKey]` 자체의 Attribute 명세 (오사용 진단, 다중 허용 등) 는 [5.2 Attribute 카탈로그](../05-advanced/02-attributes.md) 에 정리되어 있습니다.

---

[← 이전: 3.5 StaticDataManager 로 여러 테이블 관리](./05-static-data-manager.md) | [목차](../README.md) | [다음: 3.7 StaticDataView 사전 생성 뷰 →](./07-static-data-view.md)
