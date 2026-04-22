# 3.5 복잡한 Record: 타입·컬렉션·FK

세트 A 만으로는 Sdp의 진짜 효용을 느끼기 어렵습니다. 실제 게임·서비스 데이터는 외래 키, 날짜, 컬렉션, 선택적 필드가 섞여 있기 마련입니다. 이번 장은 그런 상황을 다루는 **세트 B** 를 소개합니다.

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

각 Attribute의 역할을 차례대로 설명합니다.

### `[ForeignKey("Categories", "Id")]`

`CategoryId` 가 다른 테이블의 컬럼을 가리키도록 지정합니다. 첫 번째 인자 `"Categories"` 는 뒤에 볼 **TableSet 의 속성 이름** 과 동일해야 하고, 두 번째 인자 `"Id"` 는 **그 테이블 Record의 속성 이름** 입니다. 두 번째 인자가 대상 테이블의 기본 키(`[Key]`) 라면 기본 키 인덱스로 빠르게 검증되고, 기본 키가 아니면 대상 테이블을 한 번 스캔해 값 목록을 만듭니다.

FK 는 두 단계에서 검증됩니다. **스캐너 단계** (`SchemaInfoScanner.ForeignKeySchemaChecker`, `TableSetSchemaChecker`) 에서는 인자로 적은 `"Categories"` 라는 TableSet 파라미터가 실제로 존재하는지, `"Id"` 가 대상 Record 의 컬럼인지를 확인해 오타·잘못된 참조를 빌드 시점에 잡아냅니다. **로드 단계** (`StaticDataManager.LoadAsync`) 에서는 그 위에서 실제 값 (`CategoryId`) 이 대상 테이블에 존재하는지를 검증합니다. 잘못된 값이 하나라도 있으면 로드 전체가 실패하며 `AggregateException` 에 모든 실패 건이 담깁니다.

### `[Range(0, 1_000_000)]`

값의 허용 범위를 지정합니다. `System.ComponentModel.DataAnnotations.RangeAttribute` 를 상속합니다. 범위를 벗어난 값은 로드 시점에 예외를 던집니다.

### `[DateTimeFormat("yyyy-MM-dd")]`

`DateTime` 타입에는 **반드시** 이 Attribute가 필요합니다. 포맷 문자열은 `DateTime.ParseExact` 가 이해하는 .NET 표준 형식입니다. 붙이지 않고 로드를 시도하면 `DateTimeFormatAttributeRequired` 오류가 발생합니다.

### `[TimeSpanFormat(@"hh\:mm\:ss")]`

`TimeSpan` 타입도 동일합니다. 포맷 문자열은 `TimeSpan.ParseExact` 형식을 따릅니다. `DateTime` 과 마찬가지로 **반드시** 필요합니다.

### `[SingleColumnCollection(",")] [CountRange(1, 5)] ImmutableArray<string> Tags`

컬렉션은 두 가지 방식 중 하나로 저장됩니다.

1. **단일 컬럼** 에 구분자로 여러 값을 몰아 넣는다. `Tags` 가 이 방식이다.
2. **여러 컬럼** 으로 고정 길이만큼 펼친다 (`[Length(n)]`). 예: `Tags[0]`, `Tags[1]`, `Tags[2]`.

세트 B에서는 `"melee,iron"` 처럼 한 셀에 여러 태그를 넣기 위해 `SingleColumnCollection` 을 썼습니다. `CountRange(1, 5)` 는 "잘린 뒤의 원소 개수가 1개 이상 5개 이하" 임을 보장합니다.

`SingleColumnCollection` 과 `Length` 는 같이 쓸 수 없습니다. 둘 다 붙으면 `CountRangeAndLengthMutuallyExclusive` 오류가 납니다. `CountRange` 는 `SingleColumnCollection` 과만 결합합니다.

### `[RegularExpression(@"^icons/[a-z]+\.png$")]`

문자열 값이 정규식을 만족하는지 로드 시점에 검사합니다. `string` 에만 쓸 수 있고, 다른 타입에 붙이면 `RegularExpressionAttributeOnlyForString` 으로 걸러집니다.

### `[NullString("NULL")] string? Description`

`Description` 은 `string?` 즉 Nullable입니다. Sdp는 Nullable 타입을 쓰려면 **반드시** `NullString` 으로 "이 문자열이면 null로 간주" 라는 규칙을 지정해야 한다고 요구합니다. 위 예제에서는 셀 값이 `NULL` 문자열이면 `Description` 이 `null` 이 됩니다. Attribute를 붙이지 않고 Nullable 타입을 쓰면 `NullStringAttributeRequiredForNullable` 오류가 납니다.

## 테이블 클래스

각 Record에 대해 `StaticDataTable` 서브클래스를 하나씩 만들어 둡니다.

```csharp
public sealed class ItemCategoryTable
    : StaticDataTable<ItemCategoryTable, ItemCategory, int>
{
    public ItemCategoryTable(ImmutableList<ItemCategory> records)
        : base(records, x => x.Id)
    {
    }
}

public sealed class ItemTable
    : StaticDataTable<ItemTable, Item, int>
{
    public ItemTable(ImmutableList<Item> records)
        : base(records, x => x.Id)
    {
    }
}
```

## 아직 다루지 않은 것

두 테이블을 묶어 FK 검증까지 한꺼번에 처리하려면 `StaticDataManager` 가 필요합니다. 다음 장에서 마무리합니다.

---

[← 이전: 3.4 표준 헤더 생성기](./04-header-generator.md) | [목차](../README.md) | [다음: 3.6 StaticDataManager로 여러 테이블 관리 →](./06-static-data-manager.md)
