# 3.1 첫 Record 정의하기 — Excel에서 시작

가장 흔한 상황부터 다룹니다. Excel 시트는 이미 존재하고, 이제 거기에 맞는 C# Record를 쓰려 합니다.

## 이번 장의 예제 — 세트 A

간단한 아이템 테이블 하나입니다. Excel 파일 이름은 `Items.xlsx`, 시트 이름은 `Items` 라고 가정합니다.

`Items` 시트 (A1 셀부터 헤더, 2행부터 데이터):

|Id|Name|Price|Category|
|-|-|-|-|
|1|Potion|100|Consumable|
|2|Sword|5000|Weapon|
|3|Shield|4000|Armor|

## Record 정의

이 시트를 표현하는 Record 는 다음과 같이 씁니다.

```csharp
using Sdp.Attributes;

public enum ItemCategory
{
    Consumable,
    Weapon,
    Armor,
}

[StaticDataRecord("Items", "Items")]
public sealed record Item(
    [Key] int Id,
    string Name,
    int Price,
    ItemCategory Category);
```

짧지만 필요한 정보가 모두 들어 있습니다. 하나씩 보겠습니다.

### `[StaticDataRecord("Items", "Items")]`

이 Record가 어느 Excel 파일의 어느 시트에 대응하는지 지정합니다. 첫 번째 인자가 **Excel 파일 이름** (확장자 제외), 두 번째 인자가 **시트 이름** 입니다. 이 정보는 두 가지 용도로 쓰입니다.

- `ExcelColumnExtractor` 가 CSV를 뽑을 때 대상 시트를 찾는다.
- 추출된 CSV 파일 이름 규칙 — `{ExcelFileName}.{SheetName}.csv` — 에 사용된다. 세트 A에서는 `Items.Items.csv` 가 된다.

### `[Key] int Id`

이 파라미터가 **기본 키** 임을 표시합니다. Sdp는 이 키로 유일성 인덱스를 만들고, `Get(1)` 같은 조회를 지원합니다. 같은 `Id` 가 둘 이상 있으면 로드 시점에 `InvalidOperationException` 이 발생합니다.

### `int Price`, `string Name`

속성에 특별한 Attribute가 없으면 컬럼 이름은 **파라미터 이름과 동일** 합니다. Excel 헤더에 `Name`, `Price` 컬럼이 있어야 로드됩니다.

### `ItemCategory Category`

`enum` 은 **문자열로 매칭** 됩니다. CSV에 `Consumable` 이라고 적혀 있어야 `ItemCategory.Consumable` 로 파싱됩니다. 정수 값이 아닙니다. 시트에 있는 값이 `ItemCategory` 에 정의되지 않은 이름이면 로드가 실패합니다.

## 네이밍 규약 정리

|Record 쪽|Excel 쪽|
|-|-|
|`[StaticDataRecord]` 첫 번째 인자|파일 이름 (확장자 제외)|
|`[StaticDataRecord]` 두 번째 인자|시트 이름|
|Record 이름|(Sdp에서는 별도로 쓰이지 않음)|
|파라미터 이름|헤더 컬럼 이름 (기본)|

## 아직 다루지 않은 것

- CSV 추출까지는 하지 않았습니다. Record 정의가 완성되면 `ExcelColumnExtractor` 로 `Items.Items.csv` 를 뽑게 됩니다. 추출기 사용법은 [3.3](./03-record-to-excel.md) 후반부에 등장합니다.
- 실제 메모리에 적재하고 조회하려면 **StaticDataTable** 을 하나 만들어야 합니다. 다음 장에서 다룹니다.
- 복잡한 타입(DateTime, 컬렉션, FK, Nullable 등)은 [3.5 복잡한 Record](./05-complex-record.md) 에서 세트 B로 이어집니다.

---

[← 이전: 2. 설치](../02-installation.md) | [목차](../README.md) | [다음: 3.2 StaticDataTable 구현 →](./02-static-data-table.md)
