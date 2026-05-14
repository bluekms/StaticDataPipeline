# 4.2 Attribute 카탈로그

Sdp 가 제공하는 Attribute 를 사전순으로 정리합니다.

각 Attribute 의 검증 시점 표기 중 "**스캐너**" 는 `SchemaInfoScanner` (Roslyn 분석), "**로드**" 는 `StaticDataManager.LoadAsync` 런타임을 뜻합니다.

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
## `[ColumnName(name)]`

|항목|내용|
|-|-|
|대상|Record 파라미터|
|인자|`name` — 헤더 이름|
|다중 허용|X|
|검증 시점|스캐너 / 헤더 생성 / CSV 매핑|
|누락 시|파라미터 이름이 그대로 헤더 이름|

헤더 이름을 파라미터 이름과 다르게 쓰고 싶을 때 사용합니다. 컬렉션 파라미터에 붙이면 확장된 헤더의 **접두사** 가 됩니다.

```csharp
[StaticDataRecord("GameItems", "Items")]
public sealed record ItemRecord(
    int Id,
    [ColumnName("ItemName")] string Name,
    [ColumnName("Scores")]
    [Length(3)] ImmutableArray<int> ScoreList);
```

위 예제는 헤더가 `Id`, `ItemName`, `Scores[0]`, `Scores[1]`, `Scores[2]` 로 펼쳐집니다.

---

<a id="attr-countrange"></a>
## `[CountRange(minCount, maxCount)]`

|항목|내용|
|-|-|
|대상|`[SingleColumnCollection]` 가 붙은 컬렉션 파라미터|
|인자|`minCount` (≥ 1), `maxCount`|
|다중 허용|X|
|검증 시점|스캐너 / CSV 매핑|
|`[SingleColumnCollection]` 누락|스캐너가 `CountRangeAttributeOnlyForSingleColumnCollection` 예외를 발생|
|`[Length]` 동시 부착|스캐너가 `CountRangeAndLengthMutuallyExclusive` 예외를 발생|
|`minCount` 가 0 이하|스캐너가 `CountRangeMinMustBePositive` 예외를 발생. `minCount=0` 은 "하한 제약 없음" 과 동치라 의미가 없기 때문|

단일 컬럼 모드 컬렉션의 분할된 원소 개수가 `[minCount, maxCount]` 범위 안에 있어야 합니다.

```csharp
[StaticDataRecord("GameItems", "Items")]
public sealed record ItemRecord(
    int Id,
    string Name,
    [SingleColumnCollection(",")][CountRange(1, 5)] ImmutableArray<string> Tags);
```

`Tags` 셀 값이 분할되어 `1` ~ `5` 개여야 합니다.

---

<a id="attr-datetimeformat"></a>
## `[DateTimeFormat(format)]`

|항목|내용|
|-|-|
|대상|`DateTime` 또는 `DateTime?` 타입 파라미터 (컬렉션 원소 포함)|
|인자|`format` — .NET 표준 날짜/시간 포맷 문자열 ([표준](https://learn.microsoft.com/dotnet/standard/base-types/standard-date-and-time-format-strings), [사용자 지정](https://learn.microsoft.com/dotnet/standard/base-types/custom-date-and-time-format-strings))|
|다중 허용|X|
|검증 시점|스캐너 (존재 여부), 로드 (`DateTime.ParseExact`)|
|누락 시|스캐너가 `DateTimeFormatAttributeRequired` 예외를 발생|
|오사용|비 `DateTime` 타입에 붙이면 스캐너가 `DateTimeFormatAttributeNotApplicable` 예외를 발생|

`DateTime` 은 이 Attribute 없이는 사용할 수 없습니다.

```csharp
[StaticDataRecord("Events", "Schedules")]
public sealed record ScheduleRecord(
    int Id,
    string Title,
    [DateTimeFormat("yyyy-MM-dd")] DateTime StartAt);
```

---

<a id="attr-foreignkey"></a>
## `[ForeignKey(tableSetName, recordColumnName)]`

|항목|내용|
|-|-|
|대상|Record 파라미터|
|인자|`tableSetName` — TableSet 의 속성 (= 생성자 파라미터) 이름. `recordColumnName` — 대상 Record 의 속성 이름.|
|다중 허용|O (`AllowMultiple = true`) — "여러 대상 중 하나라도 일치하면 유효" 방식|
|검증 시점|스캐너 (FK/SFK 동시 부착만) + 로드 (타겟 검증 → 참조 검증)|
|`[SwitchForeignKey]` 와 동시 부착|스캐너가 `FkSfkConflict` 예외를 발생|
|타겟이 TableSet 에 없음|로드 시 `FkTargetNotFound` 예외를 발생|
|타겟이 `[SingleColumnCollection]` 컬럼|로드 시 `FkTargetIsSingleColumnCollection` 예외를 발생|
|타겟 컬럼명이 존재하지 않음|로드 시 `IndexNotRegistered` 예외를 발생|
|값 검증 실패|`AggregateException(FkValidationFailed, ...)` 내부에 `FkValueNotFound` 예외를 발생|

자세한 흐름과 예제는 [3.6 외래 키](../03-usage/06-foreign-keys.md) 를 참고하세요.

```csharp
[StaticDataRecord("GameItems", "Items")]
public sealed record ItemRecord(
    int Id,
    string Name,
    [ForeignKey("CategoryTable", "Id")] int CategoryId);

// 여러 대상 중 하나라도 일치하면 유효
[StaticDataRecord("GameItems", "Rewards")]
public sealed record RewardRecord(
    int Id,
    [ForeignKey("ItemTable", "Id")]
    [ForeignKey("CurrencyTable", "Id")]
    int TargetId);
```

---

<a id="attr-ignore"></a>
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
public sealed record DraftItemRecord(int Id, string Name);

[StaticDataRecord("GameItems", "Items")]
public sealed record ItemRecord(
    int Id,
    string Name,
    [Ignore] int InternalCacheKey);
```

---

<a id="attr-key"></a>
## `[Key]`

|항목|내용|
|-|-|
|대상|Record 파라미터|
|인자|없음|
|다중 허용|X (Record 당 하나)|
|검증 시점|스캐너 (Map Value Record 에서의 필수성), 로드, ExcelColumnExtractor (중복 검사)|

`[Key]` 가 의미를 갖는 자리는 두 곳입니다.

- **Map (`FrozenDictionary`) 의 Value Record** — Dictionary 의 키를 어디서 뽑을지 알리기 위해 필수. 없으면 로드 시 `KeyAttributeRequiredInDictionaryValue` 예외를 발생. 자세한 예는 [4.1 Map (FrozenDictionary)](./01-schemata.md#map-frozendictionary) 참고.
- **ExcelColumnExtractor 의 중복 검사** — `[Key]` 가 붙은 컬럼의 값 중복을 추출 단계에서 검사합니다. 없으면 검사 자체를 스킵.

부수 규칙:

- Record 전체로 `[Key]` 는 최대 하나입니다 (스캐너가 `StaticDataRecordMustHaveAtMostOneKey` 예외를 발생).
- `[Key]` 가 붙은 파라미터는 non-nullable 이어야 합니다 (스캐너가 `KeyAttributeMustBeNonNullable` 예외를 발생).
- enum 파라미터에 `[Key]` 를 붙이면 매핑 시 `Enum.IsDefined` 검사가 생략됩니다 — [4.3 타입 브랜딩 패턴](./03-type-branding.md) 참고.

---

<a id="attr-length"></a>
## `[Length(length)]`

|항목|내용|
|-|-|
|대상|컬렉션 파라미터 (`ImmutableArray<T>`, `FrozenSet<T>`, `FrozenDictionary<K,V>`)|
|인자|`length` — 고정 길이|
|다중 허용|X|
|검증 시점|스캐너 / 헤더 생성 / CSV 매핑|
|누락 시|`[SingleColumnCollection]` 도 없으면 스캐너가 `LengthAttributeRequired` 예외를 발생|
|배타 관계|`[SingleColumnCollection]`, `[CountRange]` 와 동시 사용 불가|

Excel 헤더가 `Col[0]`, `Col[1]`, ..., `Col[length-1]` 로 펼쳐지는 다중 컬럼 방식입니다.

```csharp
[StaticDataRecord("GameItems", "Items")]
public sealed record ItemRecord(
    int Id,
    string Name,
    [Length(3)] ImmutableArray<string> Tags);
```

자세한 예는 [4.1 컬렉션](./01-schemata.md#컬렉션) 을 참고하세요.

---

<a id="attr-nullstring"></a>
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
public sealed record ItemRecord(
    int Id,
    string Name,
    [NullString("NULL")] string? Description);
```

`Description` 셀이 `NULL` 이면 `null`, 다른 문자열이면 그대로 매핑됩니다.

---

<a id="attr-range"></a>
## `[Range(min, max)]`

|항목|내용|
|-|-|
|대상|숫자형 파라미터 (`int`, `double` 등)|
|인자|`(int, int)`, `(double, double)`, `(Type, string, string)` 세 가지 오버로드|
|다중 허용|X|
|검증 시점|로드|
|누락 시|범위 검사 없이 진행|
|오사용|enum 에 붙이면 스캐너가 `RangeAttributeCannotBeUsedInEnum` 예외를 발생|

`System.ComponentModel.DataAnnotations.RangeAttribute` 를 상속한 타입입니다. 값이 범위를 벗어나면 로드 시 `ArgumentOutOfRangeException` 을 발생.

```csharp
[StaticDataRecord("GameItems", "Items")]
public sealed record ItemRecord(
    int Id,
    string Name,
    [Range(0, 1_000_000)] int Price);
```

---

<a id="attr-regularexpression"></a>
## `[RegularExpression(pattern)]`

|항목|내용|
|-|-|
|대상|`string` 파라미터|
|인자|`pattern` — [.NET 정규식 패턴](https://learn.microsoft.com/dotnet/standard/base-types/regular-expression-language-quick-reference)|
|다중 허용|X|
|검증 시점|스캐너 (타입 확인), 로드 (`Regex.IsMatch`)|
|누락 시|정규식 검사 없이 진행|
|오사용|비 `string` 타입에 붙이면 스캐너가 `RegularExpressionAttributeOnlyForString` 예외를 발생|

`System.ComponentModel.DataAnnotations.RegularExpressionAttribute` 상속. 일치하지 않는 값이 있으면 로드가 실패합니다.

```csharp
[StaticDataRecord("GameItems", "Items")]
public sealed record ItemRecord(
    int Id,
    string Name,
    [RegularExpression(@"^icons/[a-z]+\.png$")] string IconPath);
```

---

<a id="attr-singlecolumncollection"></a>
## `[SingleColumnCollection(separator = ",")]`

|항목|내용|
|-|-|
|대상|`ImmutableArray<T>` 또는 `FrozenSet<T>` (Dictionary 에는 불가)|
|인자|`separator` (기본값 `","`)|
|다중 허용|X|
|검증 시점|스캐너 / CSV 매핑|
|누락 시|`[Length]` 도 없으면 스캐너가 `LengthAttributeRequired` 예외를 발생|
|배타 관계|`[Length]` 와 동시 사용 불가|
|비고|원소가 Record 이면 스캐너가 `SingleColumnArrayOnlyPrimitive` 예외를 발생|

하나의 셀에 `"a,b,c"` 형태로 여러 값을 몰아 넣는 방식입니다. 길이 제약은 [`[CountRange]`](#attr-countrange) 로 함께 지정합니다.

```csharp
[StaticDataRecord("GameItems", "Items")]
public sealed record ItemRecord(
    int Id,
    string Name,
    [SingleColumnCollection(",")] ImmutableArray<string> Tags);
```

---

<a id="attr-staticdatarecord"></a>
## `[StaticDataRecord(excelFileName, sheetName)]`

|항목|내용|
|-|-|
|대상|Record 클래스|
|인자|`excelFileName` (확장자 제외), `sheetName`|
|다중 허용|X|
|검증 시점|스캐너 / ExcelColumnExtractor / 로드 각 단계에서 요구됨|
|누락 시|ExcelColumnExtractor 실행 시 `StaticDataRecordAttributeNotFound` 예외를 발생. CSV 로드에서는 `StaticDataRecordAttributeRequired` 예외를 발생.|

이 Attribute 가 없는 Record 는 "정적 데이터 테이블 대상이 아닌 보조 Record" 로 간주되어 추출/로드 대상에서 제외됩니다.

```csharp
[StaticDataRecord("GameItems", "Items")]
public sealed record ItemRecord(int Id, string Name);
```

---

<a id="attr-switchforeignkey"></a>
## `[SwitchForeignKey(conditionColumnName, conditionValue, tableSetName, recordColumnName)]`

|항목|내용|
|-|-|
|대상|Record 파라미터|
|인자|`conditionColumnName`, `conditionValue`, `tableSetName`, `recordColumnName`|
|다중 허용|O (`AllowMultiple = true`)|
|검증 시점|스캐너 (FK/SFK 동시 부착만) + 로드 (타겟 검증 → 참조 검증)|
|`[ForeignKey]` 와 동시 부착|스캐너가 `FkSfkConflict` 예외를 발생|
|타겟이 TableSet 에 없음|로드 시 `FkTargetNotFound` 예외를 발생|
|타겟이 `[SingleColumnCollection]` 컬럼|로드 시 `FkTargetIsSingleColumnCollection` 예외를 발생|
|`conditionColumnName` 이 같은 Record 안에 없음|로드 시 `SwitchFkConditionColumnNotFound` 예외를 발생|
|타겟 컬럼명이 존재하지 않음|로드 시 `IndexNotRegistered` 예외를 발생|
|값 검증 실패|`AggregateException(FkValidationFailed, ...)` 내부에 `FkValueNotFound` (조건값 포함) 예외를 발생|

같은 파라미터 값이 **다른 컬럼의 값에 따라 다른 테이블을 참조** 해야 할 때 씁니다. 자세한 흐름과 예제는 [3.6 외래 키](../03-usage/06-foreign-keys.md) 를 참고하세요.

```csharp
[StaticDataRecord("GameItems", "Rewards")]
public sealed record RewardRecord(
    int Id,
    string Kind, // "Item" | "Currency"

    [SwitchForeignKey(nameof(Kind), "Item",     "ItemTable",     "Id")]
    [SwitchForeignKey(nameof(Kind), "Currency", "CurrencyTable", "Id")]
    int TargetId);
```

---

<a id="attr-timespanformat"></a>
## `[TimeSpanFormat(format)]`

|항목|내용|
|-|-|
|대상|`TimeSpan` 또는 `TimeSpan?` 타입 파라미터 (컬렉션 원소 포함)|
|인자|`format` — .NET 표준 TimeSpan 포맷 문자열 ([표준](https://learn.microsoft.com/dotnet/standard/base-types/standard-timespan-format-strings), [사용자 지정](https://learn.microsoft.com/dotnet/standard/base-types/custom-timespan-format-strings))|
|다중 허용|X|
|검증 시점|스캐너 (존재 여부), 로드 (`TimeSpan.ParseExact`)|
|누락 시|스캐너가 `TimeSpanFormatAttributeRequired` 예외를 발생|
|오사용|비 `TimeSpan` 타입에 붙이면 스캐너가 `TimeSpanFormatAttributeNotApplicable` 예외를 발생|

`TimeSpan` 도 이 Attribute 없이는 사용할 수 없습니다.

```csharp
[StaticDataRecord("GameItems", "Items")]
public sealed record ItemRecord(
    int Id,
    string Name,
    [TimeSpanFormat(@"hh\:mm\:ss")] TimeSpan Cooldown);
```

---

[← 이전: 4.1 지원 타입 (Schemata)](./01-schemata.md) | [목차](../README.md) | [다음: 4.3 타입 브랜딩 패턴 →](./03-type-branding.md)
