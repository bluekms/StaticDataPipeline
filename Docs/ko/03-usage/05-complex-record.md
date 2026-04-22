# 3.5 복잡한 Record: 타입·컬렉션·FK

지금까지는 단일 테이블 예제였습니다. 실제 데이터는 외래 키, 날짜, 컬렉션, 선택적 필드가 함께 있는 경우가 많으므로, 이번 장에서는 이런 요소들을 Record 에 어떻게 담는지 살펴봅니다.

## 도메인

아이템 상점을 좀 더 현실적으로 만듭니다. 카테고리 테이블과 아이템 테이블 두 개입니다.

### Excel `ItemCatalog.xlsx`

`Categories` 시트:

|Id|Name|IsConsumable|
|-|-|-|
|1|Weapon|false|
|2|Armor|false|
|3|Consumable|true|

`Items` 시트:

|Id|Name|CategoryId|Price|ReleaseDate|Cooldown|Tags|IconPath|Description|
|-|-|-|-|-|-|-|-|-|
|1|Iron Sword|1|5000|2026-01-15|00:00:10|melee,iron|icons/iron.png|NULL|
|2|Steel Sword|1|8000|2026-03-10|00:00:12|melee,steel|icons/steel.png|Stronger edge|
|3|Potion|3|100|2026-01-01|00:00:01|heal,consumable|icons/potion.png|Heals 50 HP|

## Record

```csharp
using System.Collections.Immutable;
using Sdp.Attributes;

[StaticDataRecord("ItemCatalog", "Categories")]
public sealed record ItemCategory(
    [Key] int Id,
    string Name,
    bool IsConsumable);

[StaticDataRecord("ItemCatalog", "Items")]
public sealed record Item(
    [Key] int Id,
    string Name,
    [ForeignKey("Categories", "Id")] int CategoryId,
    [Range(0, 1_000_000)] int Price,
    [DateTimeFormat("yyyy-MM-dd")] DateTime ReleaseDate,
    [TimeSpanFormat(@"hh\:mm\:ss")] TimeSpan Cooldown,
    [SingleColumnCollection(",")][CountRange(1, 5)] ImmutableArray<string> Tags,
    [RegularExpression(@"^icons/[a-z]+\.png$")] string IconPath,
    [NullString("NULL")] string? Description);
```

각 Attribute 의 역할을 차례대로 설명합니다.

### `[ForeignKey("Categories", "Id")]`

`CategoryId` 가 다른 테이블의 컬럼을 가리키도록 지정합니다.

- 첫 번째 인자 `"Categories"` — 뒤에서 볼 **TableSet 의 속성 이름**
- 두 번째 인자 `"Id"` — **그 테이블 Record 의 속성 이름**

검증 시 대상 테이블의 해당 컬럼 값을 모두 모아 `HashSet` 으로 한 번 만들어 두고, `CategoryId` 가 그 안에 있는지 확인합니다. 같은 (TableSet, Column) 조합은 이후 호출에서 캐시를 재사용합니다.

FK 검증은 두 단계로 나뉩니다.

- **스캐너 단계** — 같은 파라미터에 `[ForeignKey]` 와 `[SwitchForeignKey]` 가 함께 붙어 있는지 같은 순수 스키마 규칙만 잡아냅니다. 인자가 가리키는 TableSet 파라미터나 대상 컬럼의 존재 여부는 TableSet 정의에 의존하므로 스캐너에서는 검사하지 않습니다.
- **로드 단계** (`StaticDataManager.LoadAsync`) — `ForeignKeyTargetValidator` 가 진입 직후에 타겟의 형태(`"Categories"` 가 실제 TableSet 파라미터인지, `"Id"` 컬럼이 대상 Record 에 있는지, 대상이 `[SingleColumnCollection]` 이 아닌 스칼라인지)를 확인합니다. 모든 테이블이 적재된 뒤에는 `ReferenceValidator` 가 실제 값(`CategoryId`)이 대상 테이블에 존재하는지 검사합니다. 어느 단계든 실패하면 `AggregateException` 에 모든 실패가 담겨 throw 됩니다.

### `[Range(0, 1_000_000)]`

값의 허용 범위를 지정합니다. `System.ComponentModel.DataAnnotations.RangeAttribute` 를 상속합니다. 범위를 벗어난 값은 로드 시점에 예외를 던집니다.

> **참고** — `1_000_000` 은 C# 7.0 부터 도입된 [숫자 리터럴 구분자](https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/integral-numeric-types#integer-literals) 표기로, `1000000` 과 동일합니다. 가독성 보조용일 뿐이므로 `[Range(0, 1000000)]` 로 적어도 됩니다.

### `[DateTimeFormat("yyyy-MM-dd")]`

`DateTime` 타입에는 **반드시** 이 Attribute 가 필요합니다. 포맷 문자열은 `DateTime.ParseExact` 가 이해하는 .NET 표준 형식입니다. 붙이지 않고 로드를 시도하면 `DateTimeFormatAttributeRequired` 오류가 발생합니다.

사용 가능한 포맷 문자열은 .NET 공식 문서를 참고하세요.

- [표준 날짜 및 시간 형식 문자열](https://learn.microsoft.com/dotnet/standard/base-types/standard-date-and-time-format-strings)
- [사용자 지정 날짜 및 시간 형식 문자열](https://learn.microsoft.com/dotnet/standard/base-types/custom-date-and-time-format-strings)

### `[TimeSpanFormat(@"hh\:mm\:ss")]`

`TimeSpan` 타입도 동일합니다. 포맷 문자열은 `TimeSpan.ParseExact` 형식을 따릅니다. `DateTime` 과 마찬가지로 **반드시** 필요합니다.

- [표준 TimeSpan 형식 문자열](https://learn.microsoft.com/dotnet/standard/base-types/standard-timespan-format-strings)
- [사용자 지정 TimeSpan 형식 문자열](https://learn.microsoft.com/dotnet/standard/base-types/custom-timespan-format-strings)

### `[SingleColumnCollection(",")] [CountRange(1, 5)] ImmutableArray<string> Tags`

컬렉션은 두 가지 방식 중 하나로 저장됩니다.

1. **단일 컬럼** 에 구분자로 여러 값을 몰아 넣는다. `Tags` 가 이 방식이다.
2. **여러 컬럼** 으로 고정 길이만큼 펼친다 (`[Length(n)]`). 예: `Tags[0]`, `Tags[1]`, `Tags[2]`.

이 예제에서는 `"melee,iron"` 처럼 한 셀에 여러 태그를 넣기 위해 `SingleColumnCollection` 을 썼습니다. `CountRange(1, 5)` 는 "잘린 뒤의 원소 개수가 1개 이상 5개 이하" 임을 보장합니다.

`SingleColumnCollection` 과 `Length` 는 같이 쓸 수 없습니다. 둘 다 붙으면 `CountRangeAndLengthMutuallyExclusive` 오류가 납니다. `CountRange` 는 `SingleColumnCollection` 과만 결합합니다.

### `[RegularExpression(@"^icons/[a-z]+\.png$")]`

문자열 값이 정규식을 만족하는지 로드 시점에 검사합니다. `string` 에만 쓸 수 있고, 다른 타입에 붙이면 `RegularExpressionAttributeOnlyForString` 으로 걸러집니다.

### `[NullString("NULL")] string? Description`

`Description` 은 `string?` 즉 Nullable 입니다. Sdp 는 Nullable 타입을 쓰려면 **반드시** `NullString` 으로 "이 문자열이면 null 로 간주" 라는 규칙을 지정해야 한다고 요구합니다. 위 예제에서는 셀 값이 `NULL` 문자열이면 `Description` 이 `null` 이 됩니다. Attribute 를 붙이지 않고 Nullable 타입을 쓰면 `NullStringAttributeRequiredForNullable` 오류가 납니다.

## 테이블 클래스

각 Record 에 대해 `StaticDataTable` 서브클래스를 하나씩 만들어 둡니다.

```csharp
using System.Collections.Immutable;
using Sdp.Table;

public sealed class ItemCategoryTable(ImmutableList<ItemCategory> records)
    : StaticDataTable<ItemCategoryTable, ItemCategory>(records);

public sealed class ItemTable(ImmutableList<Item> records)
    : StaticDataTable<ItemTable, Item>(records);
```

단건 조회가 필요하면 [3.2](./02-static-data-table.md) 처럼 `UniqueIndex` 를 추가합니다. 여기서는 FK 검증 흐름에 집중하기 위해 가장 단순한 형태로 둡니다.

## 아직 다루지 않은 것

두 테이블을 묶어 FK 검증까지 한꺼번에 처리하려면 `StaticDataManager` 가 필요합니다. 다음 장에서 마무리합니다.

---

[← 이전: 3.4 표준 헤더 생성기](./04-header-generator.md) | [목차](../README.md) | [다음: 3.6 StaticDataManager로 여러 테이블 관리 →](./06-static-data-manager.md)
