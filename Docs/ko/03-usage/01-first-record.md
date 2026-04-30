# 3.1 첫 Record 정의하기

이미 정리된 데이터 테이블이 있고, 거기에 맞는 C# Record 를 처음 작성해 보는 시나리오입니다. Excel 파일이 어떻게 구성되어야 하는지는 [3.3 Record를 먼저 쓰고 Excel 작업하기](./03-record-to-excel.md) 와 [3.4 표준 헤더 생성기](./04-header-generator.md) 에서 다루므로, 이 장에서는 **개념적인 테이블 데이터** 가 다음과 같이 들어온다고 가정합니다.

## 테이블 데이터 (개념)

|Id|Name|Price|Category|Memo|
|-|-|-|-|-|
|1|Potion|100|Consumable|회복 아이템|
|2|Sword|5000|Weapon|기본 검|
|3|Shield|4000|Armor|기본 방패|

`Memo` 는 기획자 참고용 컬럼입니다. C# 쪽에서는 사용하지 않지만 원본 테이블에는 남아 있다고 합시다. **Record 가 요구하지 않은 컬럼은 CSV 로 추출되지 않습니다** — 아래 결과 CSV 에서 `Memo` 가 빠진다는 점을 미리 봐 두세요.

## Record 정의

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
- 추출된 CSV 파일 이름 규칙 — `{ExcelFileName}.{SheetName}.csv` — 에 사용된다. 위 예제에서는 `Items.Items.csv` 가 된다.

### `[Key] int Id`

이 파라미터가 **기본 키** 임을 표시합니다. FK 검증에서 대상 컬럼을 빠르게 찾기 위해 사용되며, 같은 `Id` 가 둘 이상 있는지의 검사는 `StaticDataTable` 서브클래스에서 `UniqueIndex` 로 표현합니다 ([3.2](./02-static-data-table.md)).

### `int Price`, `string Name`

특별한 Attribute 가 없으면 컬럼 이름은 **파라미터 이름과 동일** 합니다. 헤더에 `Name`, `Price` 컬럼이 있어야 매핑됩니다.

### `ItemCategory Category`

`enum` 은 **문자열로 매칭** 됩니다. CSV 에 `Consumable` 이라고 적혀 있어야 `ItemCategory.Consumable` 로 파싱됩니다. 정수 값이 아닙니다. 정의되지 않은 이름이면 로드가 실패합니다.

## 추출된 CSV (개념적 결과)

`ExcelColumnExtractor` 를 돌리면 위 Record 가 요구하는 컬럼만 추려서 `Items.Items.csv` 를 만듭니다.

```
Id,Name,Price,Category
1,Potion,100,Consumable
2,Sword,5000,Weapon
3,Shield,4000,Armor
```

원본 테이블에 있던 `Memo` 는 Record 가 요구하지 않으므로 CSV 에 포함되지 않습니다. 같은 Excel 을 서버·클라이언트·툴이 각자 다른 Record 정의로 소비할 수 있는 이유가 여기에 있습니다.

## 네이밍 규약 정리

|Record 쪽|테이블·CSV 쪽|
|-|-|
|`[StaticDataRecord]` 첫 번째 인자|Excel 파일 이름 (확장자 제외)|
|`[StaticDataRecord]` 두 번째 인자|시트 이름|
|Record 이름|(Sdp 에서는 별도로 쓰이지 않음)|
|파라미터 이름|헤더 컬럼 이름 (기본)|

## 다음 단계

- 실제로 메모리에 적재하고 조회하려면 **StaticDataTable** 을 만듭니다. [3.2](./02-static-data-table.md) 에서 다룹니다.
- Excel 시트를 어떻게 작성해야 추출이 동작하는지는 [3.3](./03-record-to-excel.md) 과 [3.4](./04-header-generator.md) 에서 다룹니다.
- 복잡한 타입(DateTime, 컬렉션, FK, Nullable 등)은 [3.5 복잡한 Record](./05-complex-record.md) 로 이어집니다.

---

[← 이전: 2. 설치](../02-installation.md) | [목차](../README.md) | [다음: 3.2 StaticDataTable 구현 →](./02-static-data-table.md)
