# 3.3 Record를 먼저 쓰고 Excel 작업하기

앞서 3.1 \~ 3.2는 Excel이 이미 존재한다는 전제였습니다. 이번 장은 반대입니다. C# Record가 먼저 확정되었고, 이제 그것에 맞춰 Excel을 만드는 상황입니다.

이 흐름이 중요한 이유는, Excel 담당자가 빈 시트 위에 어디서부터 어떻게 채워야 하는지 모르기 때문입니다. 규칙을 정리해 둡니다.

## A1 셀부터 헤더

Sdp는 **A1 셀이 헤더의 시작 위치** 라고 가정합니다.

- A1 = 첫 번째 컬럼의 헤더 이름
- A2 = 첫 번째 데이터 행의 첫 번째 값
- 헤더 위에 타이틀 행을 두거나 몇 줄을 비워두면 추출이 실패합니다.

## 파일·시트 이름 규칙

`[StaticDataRecord("Items", "Items")]` 를 붙인 Record라면, 추출기는 다음을 찾습니다.

- `Items.xlsx` 파일 (또는 `.xls`)
- 그 안의 `Items` 시트

파일 이름은 `ExcelColumnExtractor` 에게 `--excel-path` 로 디렉터리를 주면 해당 디렉터리에서 `Items.xlsx` 를 찾는 식입니다. 시트는 지정된 이름이 정확히 일치해야 합니다.

## 컬럼 이름 규칙

### 기본

파라미터 이름이 곧 헤더 이름입니다. 세트 A의 `Item` 을 예로 들면 A1 \~ D1 셀에 다음과 같이 적어 두면 됩니다.

|A|B|C|D|
|-|-|-|-|
|Id|Name|Price|Category|

### `[ColumnName("...")]` 로 덮어쓰기

헤더 이름을 별도로 두고 싶다면 Record 쪽에 적용합니다.

```csharp
public sealed record Item(
    [Key] int Id,
    [ColumnName("DisplayName")] string Name,
    int Price,
    ItemCategory Category);
```

이제 Excel 헤더는 `Name` 대신 `DisplayName` 이 됩니다.

### 컬렉션이 있을 때

컬렉션을 쓰려면 Record에서 `[Length(n)]` 으로 크기를 지정하고, Excel에는 확장된 헤더를 둡니다. 예를 들어 `[Length(3)] ImmutableArray<int> Scores` 라면 Excel 헤더는 다음과 같이 세 개로 확장됩니다.

|Scores[0]|Scores[1]|Scores[2]|
|-|-|-|

헤더를 이렇게 일일이 손으로 맞추기는 번거로우므로, 다음 장의 [표준 헤더 생성기](./04-header-generator.md) 가 Record로부터 이 헤더를 자동으로 뽑아 줍니다.

## 필요 없는 컬럼은 두어도 된다

Excel 시트에 Record 가 모르는 컬럼이 더 있어도 `ExcelColumnExtractor` 는 그것을 무시하고 필요한 컬럼만 CSV로 뽑습니다. 기획자가 참고용 컬럼을 붙여 두는 경우가 흔하므로 일부러 허용됩니다.

반대로 Record가 요구하는 컬럼이 Excel 에 **없으면** 추출이 실패합니다.

## CSV 추출: ExcelColumnExtractor

CLI 사용 예:

```bash
ExcelColumnExtractor.exe ^
  --record-path ./Records ^
  --excel-path  ./Excel ^
  --output-path ./Csv
```

주요 옵션:

|옵션|의미|기본값|
|-|-|-|
|`-r`, `--record-path`|Record `.cs` 파일(또는 디렉터리) 경로|필수|
|`-e`, `--excel-path`|`.xlsx` / `.xls` 파일 또는 디렉터리|필수|
|`-o`, `--output-path`|CSV 출력 디렉터리|필수|
|`-v`, `--version`|출력 하위 폴더 이름으로 버전을 남기고 싶을 때|없음|
|`-c`, `--encoding`|출력 인코딩 (UTF-8 기본, BOM 없음)|UTF-8|
|`-f`, `--force`|출력 폴더에 파일이 있어도 덮어쓰기 허용|false|
|`-l`, `--log-path`|로그 파일 경로|없음 (콘솔만)|

실행 결과로 `./Csv/Items.Items.csv` 가 생성됩니다. 이 파일은 그대로 `ItemTable.CreateAsync` 가 읽어가는 입력이 됩니다.

## 추출 시점에 드러나는 오류

`ExcelColumnExtractor` 는 단순히 컬럼을 뽑는 것을 넘어, 다음 상황에서 오류를 보고합니다.

- 요구 컬럼이 시트에 없음
- 시트 자체가 없음
- 헤더에 중복 이름
- 셀 값이 Record 타입과 호환되지 않음 (`DateTime` 포맷 불일치 등 일부는 CSV 로드 시점까지 미뤄질 수 있음)

따라서 **CI에서 `ExcelColumnExtractor` 를 돌리는 것만으로도** 많은 실수를 사전에 잡을 수 있습니다. 어떤 오류가 어느 단계에서 드러나는지는 각 Attribute 설명의 "검증 시점" 컬럼을 참고하세요 ([4.2 Attribute 카탈로그](../04-advanced/02-attributes.md)).

---

[← 이전: 3.2 StaticDataTable 구현](./02-static-data-table.md) | [목차](../README.md) | [다음: 3.4 표준 헤더 생성기 →](./04-header-generator.md)
