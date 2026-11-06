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

선행 퀘스트처럼 같은 테이블의 키를 가리키는 FK 도 그대로 쓸 수 있습니다.

```csharp
[NullString("")] [ForeignKey("QuestTable", "Id")] int? PrevQuestId
```

</br></br></br>

## 검증 단계

ForeignKey 검증은 두 단계로 나뉩니다.

1. **빌드 시점 — 소스 제너레이터가 Record/TableSet 선언을 분석해 진단으로 보고**
   - `tableSetName` 이 실제 TableSet 파라미터에 존재하는가? (`SDP0205`)
   - `recordColumnName` 이 대상 Record 의 파라미터에 존재하는가? (`SDP0206`)
   - 대상이 `[SingleColumnCollection]` 으로 묶인 컬럼은 아닌가? (`SDP0207`)
   - 대상 컬럼이 컬렉션 타입은 아닌가? (`SDP0214`)
   - 참조 컬럼과 대상 컬럼의 타입이 같은가? (`SDP0211`) — nullable 은 비대칭이라 `int?` 참조 → `int` 대상은 허용되지만 `int` 참조 → `int?` 대상은 거부된다. 기본 타입 참조 컬럼은 **같은 타입을 감싼 브랜딩 키** (`int` → `record struct ItemId(int Value)` 인 `Id`) 도 가리킬 수 있고, 이때 로더는 대상 키의 안쪽 값으로 비교한다 (한 컬럼이 키 타입이 다른 두 테이블을 가리킬 때 쓴다). 참조 컬럼이 `ImmutableArray<int>` 처럼 원소가 단일 값인 컬렉션이면 원소 타입으로 비교하고 로드 시 원소마다 검사한다 (nullable 원소의 `null` 은 건너뜀). 원소가 브랜딩 record 인 컬렉션 (`FrozenSet<ItemRecord.ItemId>`) 도 같다. Map 이나 원소가 일반 Record 인 컬렉션은 참조 컬럼이 될 수 없다.
   - 같은 파라미터에 `[ForeignKey]` 와 `[SwitchForeignKey]` 가 함께 붙어 있지 않은가? (`SDP0204`)
   - 같은 파라미터에 같은 대상이 두 번 붙어 있지 않은가? (`SDP0226`)
   - `[SwitchForeignKey]` 의 조건 컬럼이 같은 Record 안에 존재하는가? (`SDP0209`) — 중첩 record 안에 붙였다면 그 중첩 record 안에서 찾는다.
   - 한 파라미터에 붙은 `[SwitchForeignKey]` 들이 모두 같은 조건 컬럼을 쓰는가? (`SDP0210`)
   - `[SwitchForeignKey]` 의 조건 컬럼이 단일 값 타입인가? (컬렉션 `SDP0224`, 타입 브랜딩 record `SDP0227`)
   - `[SwitchForeignKey]` 의 `conditionValue` 가 조건 컬럼 타입의 값으로 해석되는가? (enum 멤버명·정수 `SDP0212`, 정수 `SDP0220`, bool `SDP0221`, char `SDP0222`, Guid `SDP0223`, 실수·날짜·시간 `SDP0228`)
   - `[SwitchForeignKey]` 의 같은 조건이 두 번 이상 등장하지 않는가? (`SDP0208`, 표기만 다른 같은 값은 `SDP0225`)
   - 타입 브랜딩 record 의 안쪽 파라미터에 붙어 있지 않은가? (`SDP0229`)
   - `[Ignore]` 파라미터에 붙어 있지 않은가? (`SDP0230`) — 읽지 않는 컬럼은 검사할 값이 없다.

   위 검사는 테이블 Record 의 파라미터뿐 아니라 그 안에 중첩된 record 의 파라미터에도 똑같이 적용된다 ([중첩 record 안의 외래 키](#nested-foreign-keys)).
2. **로드 시점 — `LoadAsync` 가 실제 값을 확인**
   - 대상 테이블이 `disabledTables` 로 빠지지 않았는가? (검사할 값이 있는데 대상이 하나도 로드되지 않았으면 `FkTargetNotFound`, 파라미터당 한 번만 보고)
   - 실제 CSV 값이 대상 테이블의 해당 컬럼 값 집합 안에 있는가? (`FkValueNotFound`)
   - `[SwitchForeignKey]` 의 조건 컬럼 값이 분기 조건 중 하나에 매칭되는가? (`SwitchFkConditionValueNotMatched`)

로드 시점 검사가 생략되는 경우가 있습니다. 참조 컬럼을 가진 테이블 자체가 `disabledTables` 로 빠지면 그 테이블의 FK 검사는 전부 생략되고, nullable 참조 컬럼이 `null` 인 행은 검사하지 않습니다. 대상 테이블이 빠진 경우에는 로드된 대상에서 값이 발견되면 통과하고, 어느 로드된 대상에서도 발견되지 않을 때만 `FkTargetNotFound` 를 파라미터당 한 번 보고합니다. 참조할 값이 하나도 없으면 (행이 없거나 전부 `null`) 대상이 빠져 있어도 통과합니다.

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

기본 문구는 영어이고 (`Sdp/Resources/Messages.resx`), 위처럼 한글로 나오는 것은 `CultureInfo.CurrentUICulture` 가 `ko` 일 때입니다. Record 에 `[Key]` 컬럼이 있으면 `ItemRecord[Id=4].CategoryId(99)` 처럼 키 값이 함께 표시되고, `[SwitchForeignKey]` 불일치는 값 뒤에 `(when Kind=Currency)` 가 붙습니다.

메시지에 적히는 값과 자리는 다음 규칙을 따릅니다.

|참조 컬럼|메시지 예|설명|
|-|-|-|
|`ImmutableArray<int>` (여러 칸)|`HeroRecord[Id=1].SkillIds[1](9999)`|대괄호 숫자는 헤더 `SkillIds[1]` 의 그 번호|
|`[SingleColumnCollection]` 배열 (한 셀)|`PartyRecord[Id=1].HeroIds[1](7)`|셀 안에서 구분자로 나눈 순서. 0 부터 센다|
|`FrozenSet<int>`|`GachaRecord[Grade=SSR].PickupItemIds(19999)`|집합은 순서가 없어 번호를 적지 않는다|
|브랜딩 타입 컬럼 (`ItemId ItemId`)|`ShopRecord[Id=3].ItemId(19999)`|브랜딩의 안쪽 값을 적는다|
|`bool` 조건의 `[SwitchForeignKey]`|`… (when IsPremium=true)`|선언에 적는 표기와 같이 소문자|

여러 행이 어긋나 있으면 `InnerExceptions` 에 하나씩 담깁니다. 배열은 어긋난 원소마다 한 줄입니다.

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

`conditionValue` 는 문자열로 적지만 조건 컬럼의 타입에 맞춰 해석됩니다.

|조건 컬럼 타입|`conditionValue` 표기|
|-|-|
|enum|멤버 이름 (`"Item"`) 또는 정수 리터럴 (`"1"`). `[Flags]` enum 이어도 같고, `"Read, Write"` 같은 조합은 받지 않는다 (여러 값을 가지는 컬럼은 `[SingleColumnCollection]` 컬렉션으로 선언한다)|
|정수 (`int`, `long` 등)|그 타입의 범위 안에 있는 숫자|
|`bool`|`"true"` / `"false"`|
|`char`|한 글자|
|`Guid`|`Guid.TryParse` 가 받는 어떤 표기든 가능 (`{...}`, 대문자, 하이픈 없는 형식 등). 빌드·로드 모두 Guid 값으로 비교하므로 CSV 의 표기와 달라도 같은 Guid 면 매칭|
|`string`|그대로 비교|
|실수 (`float`, `double`, `decimal`)|숫자 표기. 값으로 비교하므로 `1.5` 와 `1.50` 은 같은 조건이고, 둘을 함께 쓰면 `SDP0225`|
|`DateTime` / `DateTimeOffset` / `DateOnly` / `TimeOnly`|조건 컬럼의 `[DateTimeFormat]` 과 같은 포맷으로 적은 값|
|`TimeSpan`|조건 컬럼의 `[TimeSpanFormat]` 과 같은 포맷으로 적은 값|
|타입 브랜딩 record · 컬렉션|조건 컬럼으로 쓸 수 없음 (빌드 시 `SDP0227` / `SDP0224`)|

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

<a id="nested-foreign-keys"></a>
## 중첩 record 안의 외래 키

`[ForeignKey]` 와 `[SwitchForeignKey]` 는 테이블 Record 의 파라미터뿐 아니라 **그 안에 중첩된 record 의 파라미터** 에도 붙일 수 있습니다. 중첩 record 하나, record 를 원소로 하는 컬렉션, Map 의 Value record 가 모두 대상이고 깊이 제한은 없습니다.

```csharp
public sealed record Drop(
    [ForeignKey("ItemTable", "Id")] int ItemId,
    decimal Rate);

[StaticDataRecord("GameItems", "Monsters")]
public sealed partial record MonsterRecord(
    [Key] int Id,
    string Name,
    [Length(3)] ImmutableArray<Drop> Drops);
```

`Drops[2].ItemId` 칸에 `ItemTable` 에 없는 `9999` 를 적으면 로드가 다음과 같이 실패합니다.

```
AggregateException: FK 검증에 실패했습니다.
  - MonsterRecord[Id=1001].Drops[2].ItemId(9999)이(가) [ItemTable.Id] 중 어디에도 존재하지 않습니다.
```

메시지의 경로는 중첩된 자리에 따라 다음과 같이 적힙니다.

|중첩된 자리|선언 예|경로 표기|
|-|-|-|
|record 하나|`Reward FirstKill`|`MonsterRecord[Id=1001].FirstKill.ItemId(9999)`|
|`ImmutableArray<record>`|`ImmutableArray<Drop> Drops`|`MonsterRecord[Id=1001].Drops[2].ItemId(9999)` — 대괄호 숫자는 헤더 `Drops[2]` 의 그 번호|
|`FrozenSet<record>`|`FrozenSet<Objective> Objectives`|`MonsterRecord[Id=1001].Objectives.TargetId(9999)` — 집합은 순서가 없어 번호를 적지 않는다|
|Map 의 Value record|`FrozenDictionary<int, LootSlot> Lootbox`|`MonsterRecord[Id=1001].Lootbox[Slot=5].ItemId(9999)` — Value record 의 `[Key]` 파라미터 이름과 값|

대상 테이블이 `disabledTables` 로 빠졌을 때의 `FkTargetNotFound` 는 원소를 가리키지 않으므로 `MonsterRecord.Drops.ItemId` 처럼 번호 없이 파라미터당 한 번 보고합니다.

지켜야 할 규칙은 두 가지입니다.

- **`[SwitchForeignKey]` 의 조건 컬럼은 그 Attribute 가 붙은 record 안에 있어야 합니다.** 중첩 record `Drop` 안에 붙였다면 조건 컬럼도 `Drop` 의 파라미터여야 하고, 바깥 `MonsterRecord` 의 컬럼은 가리킬 수 없습니다 (`SDP0209`).
- **타입 브랜딩 record 의 안쪽 파라미터에는 붙일 수 없습니다** (`SDP0229`). 브랜딩 타입으로 선언한 파라미터 앞에 붙입니다. [5.3 타입 브랜딩](../05-advanced/03-type-branding.md) 의 "Attribute 부착 위치" 를 참고하세요.

```csharp
public sealed record Drop(
    RewardKind Kind,
    [SwitchForeignKey(nameof(Kind), "Item",     "ItemTable",     "Id")]
    [SwitchForeignKey(nameof(Kind), "Currency", "CurrencyTable", "Id")]
    int TargetId);
```

nullable 중첩 record (`Drop?`) 는 Sdp 가 지원하지 않는 선언이라 빌드가 거부하므로 그 안의 FK 도 대상이 아닙니다.

</br></br></br>

## 정리

- `[ForeignKey(tableSet, column)]` — 단일 대상. 여러 번 붙이면 "어느 하나에 있으면 유효" 가 된다.
- `[SwitchForeignKey(conditionColumn, conditionValue, tableSet, column)]` — 분기 대상. 같은 파라미터에 여러 번 붙여 분기 표를 만든다.
- 두 Attribute 모두 중첩 record 의 파라미터에도 붙일 수 있고, 어긋난 자리는 `Drops[2].ItemId` 같은 경로로 보고된다. `[SwitchForeignKey]` 의 조건 컬럼은 같은 record 안에서만 찾는다.
- 선언의 구조 (타겟 테이블·컬럼 존재, 타입 일치 — nullable 은 참조 컬럼 쪽에만 허용되고 컬렉션 참조는 원소 타입 기준, 조건 중복) 는 빌드 시점 진단으로, 실제 참조 값은 `LoadAsync` 에서 검증되며 (컬렉션 참조는 원소마다), 로드 시점 실패는 `AggregateException` 으로 한 번에 통보된다.
- `disabledTables` 로 대상 테이블을 건너뛰면 그 테이블을 참조하는 값은 검증할 수 없으므로, 로드된 다른 대상에서도 찾지 못한 값이 하나라도 있으면 `FkTargetNotFound` 로 실패한다 (참조 값이 없으면 통과하고, 참조하는 쪽 테이블도 함께 빠지면 검사 자체가 생략된다) — 독립된 그룹만 disable 하는 것이 안전하다.

`[ForeignKey]` 와 `[SwitchForeignKey]` 자체의 Attribute 명세 (오사용 진단, 다중 허용 등) 는 [5.2 Attribute 카탈로그](../05-advanced/02-attributes.md) 에 정리되어 있습니다.

---

[← 이전: 3.5 StaticDataManager 로 여러 테이블 관리](./05-static-data-manager.md) | [목차](../README.md) | [다음: 3.7 StaticDataView 사전 생성 뷰 →](./07-static-data-view.md)
