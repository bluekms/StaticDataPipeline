# 5.2 Attribute 카탈로그

Sdp 가 제공하는 Attribute 를 사전순으로 정리합니다.

각 Attribute 의 검증 시점 표기 중 "**스캐너**" 는 `SchemaInfoScanner` (Roslyn 분석), "**빌드**" 는 Record 를 포함한 프로젝트를 컴파일할 때 동작하는 소스 제너레이터 진단 (`SDPxxxx`), "**추출**" 은 `ExcelColumnExtractor` 의 셀 값 검증 단계, "**로드**" 는 `StaticDataManager.LoadAsync` 런타임을 뜻합니다.

## 목차

- [`[ColumnName]`](#attr-columnname)
- [`[CountRange]`](#attr-countrange)
- [`[DateTimeFormat]`](#attr-datetimeformat)
- [`[ForeignKey]`](#attr-foreignkey)
- [`[Ignore]`](#attr-ignore)
- [`[Key]`](#attr-key)
- [`[Length]`](#attr-length)
- [`[NullString]`](#attr-nullstring)
- [`[Range]`](#attr-range)
- [`[RegularExpression]`](#attr-regularexpression)
- [`[SingleColumnCollection]`](#attr-singlecolumncollection)
- [`[StaticDataRecord]`](#attr-staticdatarecord)
- [`[SwitchForeignKey]`](#attr-switchforeignkey)
- [`[TimeSpanFormat]`](#attr-timespanformat)

---

<a id="attr-columnname"></a>
</br></br></br>

## `[ColumnName(name)]`

|항목|내용|
|-|-|
|대상|Record 파라미터|
|인자|`name` — 헤더 이름|
|다중 허용|X|
|검증 룰|없음 — 헤더 이름을 결정·매칭하는 데만 사용 (스캐너의 `RecordFlattener`, 헤더 생성, CSV 매핑)|
|누락 시|파라미터 이름이 그대로 헤더 이름|

헤더 이름을 파라미터 이름과 다르게 쓰고 싶을 때 사용합니다. 컬렉션 파라미터에 붙이면 확장된 헤더의 **접두사** 가 됩니다.

```csharp
[StaticDataRecord("GameItems", "Items")]
public sealed partial record ItemRecord(
    int Id,
    [ColumnName("ItemName")] string Name,
    [ColumnName("Scores")]
    [Length(3)] ImmutableArray<int> ScoreList);
```

위 예제는 헤더가 `Id`, `ItemName`, `Scores[0]`, `Scores[1]`, `Scores[2]` 로 펼쳐집니다.

---

<a id="attr-countrange"></a>
</br></br></br>

## `[CountRange(minCount, maxCount)]`

|항목|내용|
|-|-|
|대상|`[SingleColumnCollection]` 가 붙은 컬렉션 파라미터|
|인자|`minCount` (≥ 1), `maxCount`|
|다중 허용|X|
|검증 시점|스캐너 (정합성), 추출 / 로드 (분할 개수)|
|`[SingleColumnCollection]` 누락|스캐너가 `CountRangeAttributeOnlyForSingleColumnCollection` 예외를 발생|
|`[Length]` 동시 부착|스캐너가 `CountRangeAndLengthMutuallyExclusive` 예외를 발생|
|`minCount` 가 0 이하|스캐너가 `CountRangeMinMustBePositive` 예외를 발생. `minCount=0` 은 "하한 제약 없음" 과 동치라 의미가 없기 때문|

단일 컬럼 모드 컬렉션의 분할된 원소 개수가 `[minCount, maxCount]` 범위 안에 있어야 합니다. 분할 개수는 추출 단계와 런타임 로드 양쪽에서 검사합니다.

```csharp
[StaticDataRecord("GameItems", "Items")]
public sealed partial record ItemRecord(
    int Id,
    string Name,
    [SingleColumnCollection(",")][CountRange(1, 5)] ImmutableArray<string> Tags);
```

`Tags` 셀 값이 분할되어 `1` ~ `5` 개여야 합니다.

---

<a id="attr-datetimeformat"></a>
</br></br></br>

## `[DateTimeFormat(format)]`

|항목|내용|
|-|-|
|대상|`DateTime` 또는 `DateTime?` 타입 파라미터 (컬렉션 원소 포함)|
|인자|`format` — .NET 표준 날짜/시간 포맷 문자열 ([표준](https://learn.microsoft.com/dotnet/standard/base-types/standard-date-and-time-format-strings), [사용자 지정](https://learn.microsoft.com/dotnet/standard/base-types/custom-date-and-time-format-strings))|
|다중 허용|X|
|검증 시점|스캐너 (존재 여부), 추출 (`DateTime.TryParseExact`), 로드 (`DateTime.ParseExact`)|
|누락 시|스캐너가 `DateTimeFormatAttributeRequired` 예외를 발생|
|오사용|비 `DateTime` 타입에 붙이면 스캐너가 `DateTimeFormatAttributeNotApplicable` 예외를 발생|

`DateTime` 은 이 Attribute 없이는 사용할 수 없습니다. 추출 시 셀 값 검증과 런타임 매핑 모두 동일한 `format` 으로 `ParseExact` 를 호출하므로, format 과 어긋난 표기는 양쪽에서 실패합니다.

```csharp
[StaticDataRecord("Events", "Schedules")]
public sealed partial record ScheduleRecord(
    int Id,
    string Title,
    [DateTimeFormat("yyyy-MM-dd")] DateTime StartAt);
```

---

<a id="attr-foreignkey"></a>
</br></br></br>

## `[ForeignKey(tableSetName, recordColumnName)]`

|항목|내용|
|-|-|
|대상|Record 파라미터|
|인자|`tableSetName` — TableSet 의 속성 (= 생성자 파라미터) 이름. `recordColumnName` — 대상 Record 의 속성 이름.|
|다중 허용|O (`AllowMultiple = true`) — "여러 대상 중 하나라도 일치하면 유효" 방식|
|검증 시점|스캐너 (FK/SFK 동시 부착 차단) + 빌드 (타겟 테이블·컬럼 존재, 타입 일치, 중복 타겟) + 로드 (참조 값 검증)|
|`[SwitchForeignKey]` 와 동시 부착|스캐너는 `FkSwitchFkConflict`, 빌드는 `SDP0204` 로 거부|
|타겟이 TableSet 에 없음|빌드 시 `SDP0205`|
|타겟이 `[SingleColumnCollection]` 컬럼|빌드 시 `SDP0207`|
|타겟 컬럼명이 존재하지 않음|빌드 시 `SDP0206`|
|참조 컬럼과 타겟 컬럼의 타입이 다름|빌드 시 `SDP0211`|
|같은 타겟을 두 번 지정|빌드 시 `SDP0226`|
|타겟 테이블이 `disabledTables` 로 빠짐|로드 시 `FkTargetNotFound` 예외를 발생|
|값 검증 실패|`AggregateException(FkValidationFailed, ...)` 내부에 `FkValueNotFound` 예외를 발생|

로드 단계 예외는 단독으로 던져지지 않습니다 — `FkTargetNotFound`, `FkValueNotFound` 모두 `AggregateException(FkValidationFailed, ...)` 의 `InnerExceptions` 로 모여 한 번에 통보됩니다.

자세한 흐름과 예제는 [3.6 외래 키](../03-usage/06-foreign-keys.md) 를 참고하세요.

```csharp
[StaticDataRecord("GameItems", "Items")]
public sealed partial record ItemRecord(
    int Id,
    string Name,
    [ForeignKey("CategoryTable", "Id")] int CategoryId);

// 여러 대상 중 하나라도 일치하면 유효
[StaticDataRecord("GameItems", "Rewards")]
public sealed partial record RewardRecord(
    int Id,
    [ForeignKey("ItemTable", "Id")]
    [ForeignKey("CurrencyTable", "Id")]
    int TargetId);
```

---

<a id="attr-ignore"></a>
</br></br></br>

## `[Ignore]`

|항목|내용|
|-|-|
|대상|Record 클래스 **또는** Record 파라미터|
|인자|없음|
|다중 허용|X|
|검증 시점|스캐너 (적용 시 스킵)|

스캐너가 해당 Record 또는 파라미터를 건너뜁니다. 작업 중인 Record 를 임시로 빼거나, Record 내부의 계산용 파라미터를 제외할 때 사용합니다.

```csharp
[Ignore]
[StaticDataRecord("GameItems", "Items")]
public sealed partial record DraftItemRecord(int Id, string Name);

[StaticDataRecord("GameItems", "Items")]
public sealed partial record ItemRecord(
    int Id,
    string Name,
    [Ignore] int InternalCacheKey);
```

---

<a id="attr-key"></a>
</br></br></br>

## `[Key]`

|항목|내용|
|-|-|
|대상|Record 파라미터|
|인자|없음|
|다중 허용|X (Record 당 하나)|
|검증 시점|스캐너 (Map Value Record 에서의 필수성), 추출 (중복 검사), 로드|

`[Key]` 가 의미를 갖는 자리는 두 곳입니다.

- **Map (`FrozenDictionary`) 의 Value Record** — Dictionary 의 키를 어디서 뽑을지 알리기 위해 필수. 없으면 스캐너가 `KeyAttributeRequiredInDictionaryValue` 진단으로 거부. 자세한 예는 [5.1 Map (FrozenDictionary)](./01-schemata.md#map-frozendictionary) 참고.
- **ExcelColumnExtractor 의 중복 검사** — `[Key]` 가 붙은 컬럼의 값 중복을 추출 단계에서 검사합니다. 없으면 검사 자체를 스킵.

부수 규칙:

- Record 전체로 `[Key]` 는 최대 하나입니다 (스캐너가 `StaticDataRecordMustHaveAtMostOneKey` 예외를 발생).
- `[Key]` 가 붙은 파라미터는 non-nullable 이어야 합니다 (스캐너가 `KeyAttributeMustBeNonNullable` 예외를 발생).
- enum 파라미터에 `[Key]` 를 붙이면 매핑 시 정의된 멤버인지 검사하지 않습니다 — [5.3 타입 브랜딩 패턴](./03-type-branding.md) 참고.

---

<a id="attr-length"></a>
</br></br></br>

## `[Length(length)]`

|항목|내용|
|-|-|
|대상|컬렉션 파라미터 (`ImmutableArray<T>`, `FrozenSet<T>`, `FrozenDictionary<K,V>`)|
|인자|`length` — 고정 길이|
|다중 허용|X|
|검증 시점|스캐너|
|누락 시|`[SingleColumnCollection]` 도 없으면 스캐너가 `LengthAttributeRequired` 예외를 발생|
|배타 관계|`[SingleColumnCollection]`, `[CountRange]` 와 동시 사용 불가|

Excel 헤더가 `Col[0]`, `Col[1]`, ..., `Col[length-1]` 로 펼쳐지는 다중 컬럼 방식입니다.

```csharp
[StaticDataRecord("GameItems", "Items")]
public sealed partial record ItemRecord(
    int Id,
    string Name,
    [Length(3)] ImmutableArray<string> Tags);
```

자세한 예는 [5.1 컬렉션](./01-schemata.md#컬렉션) 을 참고하세요.

---

<a id="attr-nullstring"></a>
</br></br></br>

## `[NullString(nullString)]`

|항목|내용|
|-|-|
|대상|Nullable 파라미터 (또는 Nullable 원소를 가진 컬렉션)|
|인자|`nullString` — null 을 뜻하는 문자열 표현|
|다중 허용|X|
|검증 시점|스캐너 (존재 여부), 로드 (치환)|
|누락 시|스캐너가 `NullStringAttributeRequiredForNullable` (또는 ...Array, ...Set, ...Map) 예외를 발생|
|오사용|Non-nullable 에 붙이면 스캐너가 `NullStringAttributeNotAllowed` 예외를 발생|

CSV 셀 값이 이 문자열과 일치하면 `null` 로 해석합니다. 흔히 `"NULL"`, `""`, `"N/A"` 등을 씁니다.

```csharp
[StaticDataRecord("GameItems", "Items")]
public sealed partial record ItemRecord(
    int Id,
    string Name,
    [NullString("NULL")] string? Description);
```

`Description` 셀이 `NULL` 이면 `null`, 다른 문자열이면 그대로 매핑됩니다.

---

<a id="attr-range"></a>
</br></br></br>

## `[Range(min, max)]`

|항목|내용|
|-|-|
|대상|숫자형, `char`, `DateTime`, `TimeSpan`, `string`, `enum` 파라미터 (각 nullable 변형 포함)|
|인자|`(int, int)`, `(double, double)`, `(Type, string, string)` 세 가지 오버로드|
|다중 허용|X|
|검증 시점|추출 (`SchemaInfoScanner` 의 `RangeAttributeChecker`), 로드 (`Sdp.Csv.RangeValidator`)|
|적용 불가 타입에 부착|스캐너가 `RangeAttributeNotApplicable` 예외를 발생 (예: `bool` / `bool?` / 컬렉션 / record)|
|누락 시|범위 검사 없이 진행|

값이 범위를 벗어나면 `ExcelColumnExtractor` 의 셀 호환성 검사와 런타임 로드 양쪽에서 `ArgumentOutOfRangeException` 으로 실패합니다. 두 단계는 같은 경계 해석 규칙 — `string` 은 사전순 (`CompareOrdinal`), `DateTime` / `TimeSpan` 은 `[DateTimeFormat]` / `[TimeSpanFormat]` 의 포맷, `enum` 은 underlying 정수 — 을 사용하므로 검사 결과가 일치합니다.

숫자형은 두 인자 오버로드를 그대로 씁니다.

```csharp
[StaticDataRecord("GameItems", "Items")]
public sealed partial record ItemRecord(
    int Id,
    string Name,
    [Range(0, 1_000_000)] int Price);
```

비-숫자 타입은 `(Type, string, string)` 오버로드로 경계를 명시합니다. 경계 문자열은 해당 타입의 파싱 규칙에 맞춰 해석됩니다.

```csharp
// DateTime — [DateTimeFormat] 의 형식으로 경계를 적습니다
[DateTimeFormat("yyyy-MM-dd")]
[Range(typeof(DateTime), "2024-01-01", "2024-12-31")]
DateTime EventDate;

// TimeSpan — [TimeSpanFormat] 의 형식으로 경계를 적습니다
[TimeSpanFormat("c")]
[Range(typeof(TimeSpan), "00:00:00", "01:00:00")]
TimeSpan Duration;

// string — 사전순 비교 (CompareOrdinal, 문화권 무관)
[Range(typeof(string), "apple", "zebra")]
string Tag;

// enum — 멤버 이름으로 경계 지정. underlying 정수로 비교됩니다
[Range(typeof(Tier), "Low", "High")]
Tier Grade;

// Key enum — underlying 정수 문자열로 경계 지정
[Key]
[Range(typeof(ItemId), "100", "1000")]
ItemId Id;
```

Nullable 변형 (`int?`, `DateTime?`, `string?`, `Tier?` 등) 도 그대로 지원합니다. cell value 가 `[NullString]` 으로 매칭되면 Range 검사는 생략되고, non-null 값에는 inner 타입의 Range 검사가 그대로 적용됩니다.

---

<a id="attr-regularexpression"></a>
</br></br></br>

## `[RegularExpression(pattern)]`

|항목|내용|
|-|-|
|대상|`string` 또는 `string?` 파라미터|
|인자|`pattern` — [.NET 정규식 패턴](https://learn.microsoft.com/dotnet/standard/base-types/regular-expression-language-quick-reference)|
|다중 허용|X|
|검증 시점|스캐너 (타입 확인), 추출 / 로드 (`Regex.IsMatch`)|
|누락 시|정규식 검사 없이 진행|
|오사용|`string` / `string?` 외의 타입에 붙이면 스캐너가 `RegularExpressionAttributeOnlyForString` 예외를 발생|

패턴과 일치하지 않는 값이 있으면 `ExcelColumnExtractor` 의 셀 호환성 검사와 런타임 로드 양쪽에서 실패합니다. `string?` 에 붙은 경우, 셀 값이 `[NullString]` 으로 매칭되면 `null` 로 해석되어 패턴 검사를 건너뜁니다.

```csharp
[StaticDataRecord("GameItems", "Items")]
public sealed partial record ItemRecord(
    int Id,
    string Name,
    [RegularExpression(@"^icons/[a-z]+\.png$")] string IconPath);
```

---

<a id="attr-singlecolumncollection"></a>
</br></br></br>

## `[SingleColumnCollection(separator = ",")]`

|항목|내용|
|-|-|
|대상|`ImmutableArray<T>` 또는 `FrozenSet<T>` (Dictionary 에는 불가)|
|인자|`separator` (기본값 `","`)|
|다중 허용|X|
|검증 시점|스캐너 / 로드|
|누락 시|`[Length]` 도 없으면 스캐너가 `LengthAttributeRequired` 예외를 발생|
|배타 관계|`[Length]` 와 동시 사용 불가|
|비고|원소가 Record 이면 스캐너가 `SingleColumnArrayOnlyPrimitive` (Array), `SingleColumnHashSetOnlyPrimitive` (Set) 예외를 발생. Map (FrozenDictionary) 에 붙이면 `SingleColumnCollectionNotForMap` 발생|

하나의 셀에 `"a,b,c"` 형태로 여러 값을 몰아 넣는 방식입니다. 분할된 원소 개수에 제약이 필요하면 [`[CountRange]`](#attr-countrange) 를 함께 붙입니다 (선택).

```csharp
[StaticDataRecord("GameItems", "Items")]
public sealed partial record ItemRecord(
    int Id,
    string Name,
    [SingleColumnCollection(",")] ImmutableArray<string> Tags);
```

---

<a id="attr-staticdatarecord"></a>
</br></br></br>

## `[StaticDataRecord(excelFileName, sheetName, startCell?)]`

|항목|내용|
|-|-|
|대상|Record 클래스|
|인자|`excelFileName` (확장자 제외), `sheetName`, `startCell` (선택, 기본 `null`)|
|다중 허용|X|
|검증 시점|스캐너 / 추출 / 로드 각 단계에서 요구됨|
|누락 시|`ExcelColumnExtractor` 는 추출 대상 Record 가 하나도 없을 때 `StaticDataRecordAttributeNotFound` 로 종료. `StaticDataTable<TRecord>` 의 Record 에 없으면 빌드 시 `SDP0215`.|

이 Attribute 가 없는 Record 는 "정적 데이터 테이블 대상이 아닌 보조 Record" 로 간주되어 추출/로드 대상에서 제외됩니다.

```csharp
[StaticDataRecord("GameItems", "Items")]
public sealed partial record ItemRecord(int Id, string Name);
```

세 번째 인자 `startCell` 은 이 Record 가 대응하는 시트의 헤더 시작 셀을 Record 단위로 지정합니다. 지정하면 `ExcelColumnExtractor` 의 `--start-cell` 옵션보다 우선합니다. 한 프로젝트 안에서 대부분의 시트가 같은 시작 셀을 쓰되 일부만 다른 자리에서 시작할 때 유용합니다.

```csharp
// 다른 시트와 달리 이 시트만 B3 에서 헤더가 시작
[StaticDataRecord("GameItems", "Quests", "B3")]
public sealed partial record QuestRecord(int Id, string Title);
```

---

<a id="attr-switchforeignkey"></a>
</br></br></br>

## `[SwitchForeignKey(conditionColumnName, conditionValue, tableSetName, recordColumnName)]`

|항목|내용|
|-|-|
|대상|Record 파라미터|
|인자|`conditionColumnName`, `conditionValue`, `tableSetName`, `recordColumnName`|
|다중 허용|O (`AllowMultiple = true`)|
|검증 시점|스캐너 (FK/SFK 동시 부착 차단, 중복 조건 차단) + 빌드 (조건 컬럼·타겟 존재, 조건값 형식, 중복 조건) + 로드 (조건 매칭, 참조 값 검증)|
|`[ForeignKey]` 와 동시 부착|스캐너는 `FkSwitchFkConflict`, 빌드는 `SDP0204` 로 거부|
|같은 `(conditionColumnName, conditionValue)` 가 두 번 이상 부착|스캐너는 `SwitchForeignKeyDuplicateCondition`, 빌드는 `SDP0208` 로 거부. 표기만 다른 같은 값 (예: `1` 과 `01`) 은 `SDP0225`|
|타겟이 TableSet 에 없음|빌드 시 `SDP0205`|
|타겟이 `[SingleColumnCollection]` 컬럼|빌드 시 `SDP0207`|
|`conditionColumnName` 이 같은 Record 안에 없음|빌드 시 `SDP0209`|
|타겟 컬럼명이 존재하지 않음|빌드 시 `SDP0206`|
|`conditionValue` 가 조건 컬럼 타입으로 해석되지 않음|빌드 시 `SDP0212` (정수·bool·char·Guid 는 `SDP0220` ~ `SDP0223`)|
|타겟 테이블이 `disabledTables` 로 빠짐|로드 시 `FkTargetNotFound` 예외를 발생|
|조건 컬럼 값이 어느 분기에도 매칭되지 않음|값 검증 단계에서 `SwitchFkConditionValueNotMatched` 예외를 발생|
|값 검증 실패|`AggregateException(FkValidationFailed, ...)` 내부에 `FkValueNotFound` (조건값 포함) 예외를 발생|

같은 파라미터 값이 **다른 컬럼의 값에 따라 다른 테이블을 참조** 해야 할 때 씁니다. 자세한 흐름과 예제는 [3.6 외래 키](../03-usage/06-foreign-keys.md) 를 참고하세요.

로드 단계 예외도 단독으로 던져지지 않습니다 — `FkTargetNotFound`, `SwitchFkConditionValueNotMatched`, `FkValueNotFound` 모두 `AggregateException(FkValidationFailed, ...)` 의 `InnerExceptions` 로 모여 한 번에 통보됩니다.

```csharp
[StaticDataRecord("GameItems", "Rewards")]
public sealed partial record RewardRecord(
    int Id,
    string Kind, // "Item" | "Currency"

    [SwitchForeignKey(nameof(Kind), "Item",     "ItemTable",     "Id")]
    [SwitchForeignKey(nameof(Kind), "Currency", "CurrencyTable", "Id")]
    int TargetId);
```

---

<a id="attr-timespanformat"></a>
</br></br></br>

## `[TimeSpanFormat(format)]`

|항목|내용|
|-|-|
|대상|`TimeSpan` 또는 `TimeSpan?` 타입 파라미터 (컬렉션 원소 포함)|
|인자|`format` — .NET 표준 TimeSpan 포맷 문자열 ([표준](https://learn.microsoft.com/dotnet/standard/base-types/standard-timespan-format-strings), [사용자 지정](https://learn.microsoft.com/dotnet/standard/base-types/custom-timespan-format-strings))|
|다중 허용|X|
|검증 시점|스캐너 (존재 여부), 추출 (`TimeSpan.TryParseExact`), 로드 (`TimeSpan.ParseExact`)|
|누락 시|스캐너가 `TimeSpanFormatAttributeRequired` 예외를 발생|
|오사용|비 `TimeSpan` 타입에 붙이면 스캐너가 `TimeSpanFormatAttributeNotApplicable` 예외를 발생|

`TimeSpan` 도 이 Attribute 없이는 사용할 수 없습니다. 추출 시 셀 값 검증과 런타임 매핑 모두 동일한 `format` 으로 `ParseExact` 를 호출하므로, format 과 어긋난 표기는 양쪽에서 실패합니다.

```csharp
[StaticDataRecord("GameItems", "Items")]
public sealed partial record ItemRecord(
    int Id,
    string Name,
    [TimeSpanFormat(@"hh\:mm\:ss")] TimeSpan Cooldown);
```

---

[← 이전: 5.1 지원 타입 (Schemata)](./01-schemata.md) | [목차](../README.md) | [다음: 5.3 타입 브랜딩 패턴 →](./03-type-branding.md)
