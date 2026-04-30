# 4.2 Attribute 카탈로그

Sdp는 14개의 Attribute를 제공합니다. 용도에 따라 5개 그룹으로 묶어 설명합니다.

- **메타**: 어떤 Record가 어떤 Excel 시트에 대응하는지, 어느 파라미터가 키인지 표시.
- **컬럼 매핑**: 헤더 이름과 Record 파라미터 이름을 연결.
- **컬렉션 크기**: Array / Set / Dictionary 의 크기·구분 방식 지정.
- **타입 제약**: 값의 범위·형식·정규식·null 표현을 제약.
- **관계**: 테이블 간 FK.

각 Attribute의 검증 시점 표기 중 "**스캐너**" 는 `SchemaInfoScanner` (Roslyn 분석), "**로드**" 는 `StaticDataManager.LoadAsync` 런타임을 뜻합니다.

---

## 메타 Attribute

<a id="attr-staticdatarecord"></a>
### `[StaticDataRecord(excelFileName, sheetName)]`

|항목|내용|
|-|-|
|대상|Record 클래스|
|인자|`excelFileName` (확장자 제외), `sheetName`|
|다중 허용|X|
|검증 시점|스캐너 / ExcelColumnExtractor / 로드 각 단계에서 요구됨|
|누락 시|`ExcelColumnExtractor` 실행 시 `StaticDataRecordAttributeNotFound`. CSV 로드에서는 `StaticDataRecordAttributeRequired`.|

이 Attribute가 없는 Record는 "정적 데이터 테이블 대상이 아닌 보조 Record" 로 간주되어 추출/로드 대상에서 제외됩니다.

<a id="attr-key"></a>
### `[Key]`

|항목|내용|
|-|-|
|대상|Record 파라미터|
|인자|없음|
|다중 허용|X (Record 당 하나)|
|검증 시점|스캐너 (Dictionary Value Record 에서의 필수성), 로드|
|누락 시|Dictionary Value Record 에서 `KeyAttributeRequiredInDictionaryValue`.|

기본 키임을 표시합니다. `FrozenDictionary<K, V>` 처럼 Value 가 Record 인 컬렉션에서 어떤 파라미터가 키인지 식별하기 위해 사용됩니다. 일반 테이블 Record 에서는 의미 표시 (FK 대상이 될 키 컬럼임을 명시) 와 도메인 가독성을 위해 붙여 두는 것을 권장합니다.

또한, enum 파라미터에 `[Key]` 를 붙이면 매핑 시 `Enum.IsDefined` 검사가 생략됩니다. enum 을 ID 코드 공간으로 활용하는 [4.3 타입 브랜딩 패턴](./03-type-branding.md) 에서 사용됩니다.

<a id="attr-ignore"></a>
### `[Ignore]`

|항목|내용|
|-|-|
|대상|Record 클래스 **또는** Record 파라미터|
|인자|없음|
|다중 허용|X|
|검증 시점|스캐너 (적용 시 스킵)|
|누락 시|해당 없음|

스캐너가 해당 Record 또는 파라미터를 건너뜁니다. 작업 중인 Record를 임시로 빼거나, Record 내부의 계산용 파라미터를 제외할 때 사용합니다.

---

## 컬럼 매핑 Attribute

<a id="attr-columnname"></a>
### `[ColumnName(name)]`

|항목|내용|
|-|-|
|대상|Record 파라미터|
|인자|`name` — 헤더 이름|
|다중 허용|X|
|검증 시점|스캐너 / 헤더 생성 / CSV 매핑|
|누락 시|파라미터 이름이 그대로 헤더 이름|

헤더 이름을 파라미터 이름과 다르게 쓰고 싶을 때 사용합니다. 컬렉션 파라미터에 붙이면 확장된 헤더의 **접두사** 가 됩니다 (예: `[ColumnName("Scores")] [Length(3)]` → `Scores[0]`, `Scores[1]`, `Scores[2]`).

---

## 컬렉션 크기 Attribute

<a id="attr-length"></a>
### `[Length(length)]`

|항목|내용|
|-|-|
|대상|컬렉션 파라미터 (`ImmutableArray<T>`, `FrozenSet<T>`, `FrozenDictionary<K,V>`)|
|인자|`length` — 고정 길이|
|다중 허용|X|
|검증 시점|스캐너 / 헤더 생성 / CSV 매핑|
|누락 시|`[SingleColumnCollection]` 도 없으면 `LengthAttributeRequired`|
|배타 관계|`[SingleColumnCollection]`, `[CountRange]` 와 동시 사용 불가|

Excel 헤더가 `Col[0]`, `Col[1]`, ..., `Col[length-1]` 로 펼쳐지는 다중 컬럼 방식입니다.

<a id="attr-singlecolumncollection"></a>
### `[SingleColumnCollection(separator = ",")]`

|항목|내용|
|-|-|
|대상|`ImmutableArray<T>` 또는 `FrozenSet<T>` (Dictionary 에는 불가)|
|인자|`separator` (기본값 `","`)|
|다중 허용|X|
|검증 시점|스캐너 / CSV 매핑|
|누락 시|`[Length]` 도 없으면 `LengthAttributeRequired`|
|배타 관계|`[Length]` 와 동시 사용 불가|
|비고|원소가 Record 이면 불가 (`SingleColumnArrayOnlyPrimitive`)|

하나의 셀에 `"a,b,c"` 형태로 여러 값을 몰아 넣는 방식입니다.

<a id="attr-countrange"></a>
### `[CountRange(minCount, maxCount)]`

|항목|내용|
|-|-|
|대상|`[SingleColumnCollection]` 가 붙은 컬렉션 파라미터|
|인자|`minCount`, `maxCount`|
|다중 허용|X|
|검증 시점|스캐너 / CSV 매핑|
|조건|`[SingleColumnCollection]` 이 없으면 `CountRangeAttributeOnlyForSingleColumnCollection`|
|배타 관계|`[Length]` 와 동시 사용 불가|

분할된 원소 개수가 `[min, max]` 범위 안에 있어야 합니다. 예: `[CountRange(1, 5)]` 이면 1\~5개 허용.

---

## 타입 제약 Attribute

<a id="attr-nullstring"></a>
### `[NullString(nullString)]`

|항목|내용|
|-|-|
|대상|Nullable 파라미터 (또는 Nullable 원소를 가진 컬렉션)|
|인자|`nullString` — null 을 뜻하는 문자열 표현|
|다중 허용|X|
|검증 시점|스캐너 (존재 여부), 로드 (치환)|
|누락 시|`NullStringAttributeRequiredForNullable` / `...Array` / `...Set` / `...Map`|
|오사용|Non-nullable에 붙이면 `NullStringAttributeNotAllowed`|

CSV 셀 값이 이 문자열과 일치하면 `null` 로 해석합니다. 흔히 `"NULL"`, `""`, `"N/A"` 등을 씁니다.

<a id="attr-datetimeformat"></a>
### `[DateTimeFormat(format)]`

|항목|내용|
|-|-|
|대상|`DateTime` 또는 `DateTime?` 타입 파라미터 (컬렉션 원소 포함)|
|인자|`format` — .NET 표준 날짜/시간 포맷 문자열|
|다중 허용|X|
|검증 시점|스캐너 (존재 여부), 로드 (`DateTime.ParseExact`)|
|누락 시|`DateTimeFormatAttributeRequired`|
|오사용|비 DateTime 타입에 붙이면 `DateTimeFormatAttributeNotApplicable`|

`DateTime` 은 이 Attribute 없이는 사용할 수 없습니다.

<a id="attr-timespanformat"></a>
### `[TimeSpanFormat(format)]`

|항목|내용|
|-|-|
|대상|`TimeSpan` 또는 `TimeSpan?` 타입 파라미터 (컬렉션 원소 포함)|
|인자|`format` — .NET 표준 TimeSpan 포맷 문자열|
|다중 허용|X|
|검증 시점|스캐너 (존재 여부), 로드 (`TimeSpan.ParseExact`)|
|누락 시|`TimeSpanFormatAttributeRequired`|
|오사용|비 TimeSpan 타입에 붙이면 `TimeSpanFormatAttributeNotApplicable`|

`TimeSpan` 도 이 Attribute 없이는 사용할 수 없습니다.

<a id="attr-range"></a>
### `[Range(min, max)]`

|항목|내용|
|-|-|
|대상|숫자형 파라미터 (`int`, `double` 등)|
|인자|`(int, int)`, `(double, double)`, `(Type, string, string)` 세 가지 오버로드|
|다중 허용|X|
|검증 시점|로드 (`RangeAttributeChecker`)|
|누락 시|범위 검사 없이 진행|
|오사용|enum에 붙이면 `RangeAttributeCannotBeUsedInEnum`|

`System.ComponentModel.DataAnnotations.RangeAttribute` 를 상속한 타입입니다. 값이 범위를 벗어나면 `ArgumentOutOfRangeException` 이 발생합니다.

<a id="attr-regularexpression"></a>
### `[RegularExpression(pattern)]`

|항목|내용|
|-|-|
|대상|`string` 파라미터|
|인자|`pattern` — 정규식|
|다중 허용|X|
|검증 시점|스캐너 (타입 확인), 로드 (`Regex.IsMatch`)|
|누락 시|정규식 검사 없이 진행|
|오사용|비 string 타입에 붙이면 `RegularExpressionAttributeOnlyForString`|

`System.ComponentModel.DataAnnotations.RegularExpressionAttribute` 상속. 일치하지 않는 값이 있으면 로드가 실패합니다.

---

## 관계 Attribute

<a id="attr-foreignkey"></a>
### `[ForeignKey(tableSetName, recordColumnName)]`

|항목|내용|
|-|-|
|대상|Record 파라미터|
|인자|`tableSetName` — TableSet 의 속성(=생성자 파라미터) 이름. `recordColumnName` — 대상 Record 의 속성 이름.|
|다중 허용|O (`AllowMultiple = true`) — "여러 대상 중 하나라도 일치하면 유효" 방식|
|검증 시점|스캐너 (`TableSetSchemaChecker` / `ForeignKeySchemaChecker`) + 로드 (`ForeignKeyValidator`)|
|누락 시|FK 검증 수행 안 됨|
|오사용 (스캐너)|`tableSetName` 이 어떤 TableSet 파라미터에도 매칭되지 않으면 `ForeignKeyTableSetNameNotFound`. `recordColumnName` 이 대상 Record 에 선언되지 않았으면 `ForeignKeyColumnNotFound`. 모두 `InvalidAttributeUsageException` 으로 묶여 `AggregateException` 으로 throw.|
|오사용 (로드)|`tableSetName` 이 없으면 `FkTargetNotFound`. `recordColumnName` 이 존재하지 않으면 `IndexNotRegistered`.|
|실패 시|`AggregateException` 내부에 `FkValueNotFound`|

- 검증 시 대상 테이블의 `recordColumnName` 컬럼 값을 모두 모아 `HashSet` 을 한 번 만든 뒤 (이후 같은 (TableSet, Column) 조합은 캐시 재사용) 그 안에 값이 있는지 확인합니다.
- **여러 FK 를 같은 파라미터에 붙이면** 그 중 하나에만 존재해도 유효. 서로 다른 테이블에서 공유되는 키 ID 체계에 유용합니다.
- 스캐너 단계 검증은 인자가 가리키는 **TableSet 파라미터 이름**·**대상 컬럼 이름** 이 실제로 존재하는지만 본다. 값이 실제로 있는지 (`FkValueNotFound` 등) 는 로드 단계의 검사에 맡긴다.

<a id="attr-switchforeignkey"></a>
### `[SwitchForeignKey(conditionColumnName, conditionValue, tableSetName, recordColumnName)]`

|항목|내용|
|-|-|
|대상|Record 파라미터|
|인자|`conditionColumnName`, `conditionValue`, `tableSetName`, `recordColumnName`|
|다중 허용|O (`AllowMultiple = true`)|
|검증 시점|스캐너 (`TableSetSchemaChecker` / `ForeignKeySchemaChecker`) + 로드 (`SwitchForeignKeyValidator`)|
|오사용 (스캐너)|`conditionColumnName` 이 같은 Record 안에 없으면 `SwitchForeignKeyConditionColumnNotFound`. `tableSetName` 이 어떤 TableSet 파라미터에도 매칭되지 않으면 `SwitchForeignKeyTableSetNameNotFound`. `recordColumnName` 이 대상 Record 에 선언되지 않았으면 `SwitchForeignKeyColumnNotFound`.|
|실패 시|`AggregateException` 내부에 `FkValueNotFound` (조건값 포함)|

같은 파라미터 값이 **다른 컬럼의 값에 따라 다른 테이블을 참조** 해야 할 때 씁니다. 예:

```csharp
public sealed record Reward(
    [Key] int Id,
    string Kind, // "Item" | "Currency"
    [SwitchForeignKey(nameof(Kind), "Item",     "Items",      "Id")]
    [SwitchForeignKey(nameof(Kind), "Currency", "Currencies", "Id")]
    int TargetId);
```

`Kind == "Item"` 이면 `Items.Id` 에, `Kind == "Currency"` 이면 `Currencies.Id` 에 `TargetId` 가 있어야 합니다.

---

## 요약 — 언제 어떤 Attribute 를 쓰는가

|상황|권장 Attribute|
|-|-|
|기본 키 지정|`[Key]`|
|Excel / 시트 / Record 연결|`[StaticDataRecord(excel, sheet)]`|
|헤더 이름과 파라미터 이름이 다를 때|`[ColumnName]`|
|고정 길이 컬렉션|`[Length(n)]`|
|한 셀에 여러 값 (구분자)|`[SingleColumnCollection(sep)]` + `[CountRange]`|
|Nullable 타입|`[NullString(...)]`|
|DateTime / TimeSpan|`[DateTimeFormat(fmt)]` / `[TimeSpanFormat(fmt)]`|
|수치 범위 제약|`[Range(min, max)]`|
|문자열 형식 제약|`[RegularExpression(pattern)]`|
|다른 테이블 참조|`[ForeignKey(tableSet, column)]`|
|조건부 참조|`[SwitchForeignKey(...)]`|
|분석·검증에서 임시 제외|`[Ignore]`|

---

[← 이전: 4.1 지원 타입 (Schemata)](./01-schemata.md) | [목차](../README.md) | [다음: 4.3 타입 브랜딩 패턴 →](./03-type-branding.md)
