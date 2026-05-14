# 3.3 첫 Record 정의하기

> 여기서부터는 **프로그래머** 관점으로 전환됩니다. 데이터 작업자 관점의 Excel 작성은 [3.1](./01-record-to-excel.md), [3.2](./02-header-generator.md) 에서 다루었고, 이번 챕터부터는 C# 쪽에서 Record 와 Table, Manager 를 어떻게 짜는지 봅니다.

이미 채워진 시트가 있고, 거기에 맞는 C# Record 를 처음 작성하는 시나리오입니다. 예시 시트는 다음과 같다고 합시다.

|       | **A**  | **B**    | **C**       | **D**   | **E**        |
|-------|--------|----------|-------------|---------|--------------|
| **1** | Id     | Name     | Memo        | Price   | Category     |
| **2** | 1      | Potion   | 회복 아이템   | 100     | Consumable   |
| **3** | 2      | Sword    | 기본 검      | 5000    | Weapon       |
| **4** | 3      | Shield   | 기본 방패    | 4000    | Armor        |

`Memo` 는 데이터 작업자의 참고용 컬럼입니다. C# 쪽에서는 사용하지 않습니다. **Record 가 요구하지 않은 컬럼은 CSV 로 추출되지 않습니다** — 아래 결과 CSV 에서 `Memo` 가 빠진다는 점을 미리 봐 두세요.

## Record 정의

```csharp
using System.ComponentModel.DataAnnotations;
using Sdp.Attributes;

public enum ItemCategory
{
    Consumable,
    Weapon,
    Armor,
}

[StaticDataRecord("GameItems", "Items")]
public sealed record ItemRecord(
    int Id,
    string Name,
    [Range(0, 1_000_000)] int Price,
    ItemCategory Category);
```

짧지만 필요한 정보가 모두 들어 있습니다. 하나씩 보겠습니다.

### `[StaticDataRecord("GameItems", "Items")]`

이 Record 가 어느 Excel 파일의 어느 시트에 대응하는지 지정합니다. 첫 번째 인자가 **Excel 파일 이름** (확장자 제외), 두 번째 인자가 **시트 이름** 입니다. 두 가지 용도로 쓰입니다.

- `ExcelColumnExtractor` 가 CSV 를 뽑을 때 대상 파일과 시트를 찾는다.
- 추출 결과 CSV 의 파일 이름 — `{파일}.{시트}.csv` — 에 사용된다. 위 예제에서는 `GameItems.Items.csv`.

### `int Id`, `string Name`

특별한 Attribute 가 없으면 컬럼 이름은 **파라미터 이름과 동일** 합니다. 시트의 헤더에 `Id`, `Name` 컬럼이 있어야 매핑됩니다.

### `[Range(0, 1_000_000)] int Price`

`[Range(min, max)]` 는 값이 지정된 범위 안에 있는지 로드 시점에 검사합니다. `System.ComponentModel.DataAnnotations.RangeAttribute` 를 상속한 Attribute 입니다. 범위를 벗어난 값이 셀에 들어 있으면 로드가 실패합니다.

> `1_000_000` 은 C# 의 [숫자 리터럴 구분자](https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/integral-numeric-types#integer-literals) 표기로, `1000000` 과 같은 값입니다. 가독성 보조용일 뿐이므로 `[Range(0, 1000000)]` 로 적어도 됩니다.

### `ItemCategory Category`

`enum` 은 **문자열로 매칭** 됩니다. CSV 셀에 `Consumable` 이라고 적혀 있어야 `ItemCategory.Consumable` 로 파싱됩니다. 정수 값이 아니며, 대소문자도 정확히 일치해야 합니다 (`consumable`, `CONSUMABLE` 은 실패). 정의되지 않은 이름도 마찬가지로 로드 실패입니다.

## 추출된 CSV

`ExcelColumnExtractor` 를 돌리면 위 Record 가 요구하는 컬럼만 추려서 `GameItems.Items.csv` 를 만듭니다.

```
Id,Name,Price,Category
1,Potion,100,Consumable
2,Sword,5000,Weapon
3,Shield,4000,Armor
```

원본 시트에 있던 `Memo` 는 Record 가 요구하지 않으므로 CSV 에 포함되지 않습니다. 같은 Excel 을 서버, 클라이언트, 툴이 각자 다른 Record 정의로 소비할 수 있는 이유가 여기에 있습니다.

## 다음 단계

- 실제로 메모리에 적재하고 조회하려면 **StaticDataTable** 을 만듭니다. [3.4](./04-static-data-table.md) 에서 다룹니다.
- 사용 가능한 타입의 전체 목록과 각 타입에 필수로 따라붙는 Attribute 는 [4.1 지원 타입](../04-advanced/01-schemata.md) 에서 정리합니다.
- Attribute 카탈로그는 [4.2](../04-advanced/02-attributes.md) 에 모여 있습니다.

---

[← 이전: 3.2 표준 헤더 생성기](./02-header-generator.md) | [목차](../README.md) | [다음: 3.4 StaticDataTable 구현 →](./04-static-data-table.md)
