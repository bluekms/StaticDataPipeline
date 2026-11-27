# 5.2 Attribute Catalog

This page lists the Attributes provided by Sdp in alphabetical order.

In each Attribute's validation-timing notation, "**scanner**" refers to `SchemaInfoScanner` (Roslyn analysis), "**extraction**" refers to the cell-value validation stage of `ExcelColumnExtractor`, and "**load**" refers to the `StaticDataManager.LoadAsync` runtime.

## Table of Contents

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

|Item|Details|
|-|-|
|Target|Record parameter|
|Argument|`name` — header name|
|Allow multiple|X|
|Validation rule|None — used only to determine and match the header name (the scanner's `RecordFlattener`, header generation, CSV mapping)|
|If omitted|The parameter name is used as the header name|

Use this when you want the header name to differ from the parameter name. When attached to a collection parameter, it becomes the **prefix** of the expanded headers.

```csharp
[StaticDataRecord("GameItems", "Items")]
public sealed record ItemRecord(
    int Id,
    [ColumnName("ItemName")] string Name,
    [ColumnName("Scores")]
    [Length(3)] ImmutableArray<int> ScoreList);
```

In the example above, the headers expand to `Id`, `ItemName`, `Scores[0]`, `Scores[1]`, `Scores[2]`.

---

<a id="attr-countrange"></a>
</br></br></br>

## `[CountRange(minCount, maxCount)]`

|Item|Details|
|-|-|
|Target|A collection parameter annotated with `[SingleColumnCollection]`|
|Arguments|`minCount` (≥ 1), `maxCount`|
|Allow multiple|X|
|Validation timing|Scanner (consistency), extraction / load (split count)|
|`[SingleColumnCollection]` missing|The scanner raises a `CountRangeAttributeOnlyForSingleColumnCollection` exception|
|`[Length]` attached simultaneously|The scanner raises a `CountRangeAndLengthMutuallyExclusive` exception|
|`minCount` is 0 or less|The scanner raises a `CountRangeMinMustBePositive` exception. `minCount=0` is equivalent to "no lower-bound constraint", so it is meaningless|

The number of split elements in a single-column-mode collection must fall within the range `[minCount, maxCount]`. The split count is checked at both the extraction stage and at runtime load.

```csharp
[StaticDataRecord("GameItems", "Items")]
public sealed record ItemRecord(
    int Id,
    string Name,
    [SingleColumnCollection(",")][CountRange(1, 5)] ImmutableArray<string> Tags);
```

The `Tags` cell value, once split, must produce `1` to `5` elements.

---

<a id="attr-datetimeformat"></a>
</br></br></br>

## `[DateTimeFormat(format)]`

|Item|Details|
|-|-|
|Target|A `DateTime` or `DateTime?` typed parameter (including collection elements)|
|Argument|`format` — a .NET standard date/time format string ([standard](https://learn.microsoft.com/dotnet/standard/base-types/standard-date-and-time-format-strings), [custom](https://learn.microsoft.com/dotnet/standard/base-types/custom-date-and-time-format-strings))|
|Allow multiple|X|
|Validation timing|Scanner (presence), extraction (`DateTime.TryParseExact`), load (`DateTime.ParseExact`)|
|If omitted|The scanner raises a `DateTimeFormatAttributeRequired` exception|
|Misuse|If attached to a non-`DateTime` type, the scanner raises a `DateTimeFormatAttributeNotApplicable` exception|

`DateTime` cannot be used without this Attribute. Because both the extraction-stage cell-value validation and the runtime mapping call `ParseExact` with the same `format`, a representation that deviates from the format fails on both sides.

```csharp
[StaticDataRecord("Events", "Schedules")]
public sealed record ScheduleRecord(
    int Id,
    string Title,
    [DateTimeFormat("yyyy-MM-dd")] DateTime StartAt);
```

---

<a id="attr-foreignkey"></a>
</br></br></br>

## `[ForeignKey(tableSetName, recordColumnName)]`

|Item|Details|
|-|-|
|Target|Record parameter|
|Arguments|`tableSetName` — the name of the TableSet property (= constructor parameter). `recordColumnName` — the property name of the target Record.|
|Allow multiple|X — for more than one target, add a discriminator column and use `[SwitchForeignKey]`|
|Validation timing|Scanner (blocks simultaneous FK/SFK attachment) + load (re-checks simultaneous FK/SFK attachment, target validation, reference validation)|
|Attached together with `[SwitchForeignKey]`|Both the scanner and load reject it with the `FkSwitchFkConflict` diagnostic|
|Target not present in the TableSet|Raises an `FkTargetNotFound` exception at load|
|Target is a `[SingleColumnCollection]` column|Raises an `FkTargetIsSingleColumnCollection` exception at load|
|Target column name does not exist|Raises an `FkTargetColumnNotFound` exception during the target resolution stage after table load|
|Value validation failure|Raises an `FkValueNotFound` exception inside `AggregateException(FkValidationFailed, ...)`|

The load-stage diagnostics above are not thrown individually — `FkTargetNotFound`, `FkTargetIsSingleColumnCollection`, `FkTargetColumnNotFound`, and `FkValueNotFound` are all gathered into the `InnerExceptions` of `AggregateException(FkValidationFailed, ...)` and reported at once.

For the detailed flow and examples, see [3.6 Foreign Keys](../03-usage/06-foreign-keys.md).

```csharp
[StaticDataRecord("GameItems", "Items")]
public sealed record ItemRecord(
    int Id,
    string Name,
    [ForeignKey("CategoryTable", "Id")] int CategoryId);
```

---

<a id="attr-ignore"></a>
</br></br></br>

## `[Ignore]`

|Item|Details|
|-|-|
|Target|Record class **or** Record parameter|
|Argument|None|
|Allow multiple|X|
|Validation timing|Scanner (skipped when applied)|

The scanner skips the affected Record or parameter. Use it to temporarily exclude a Record you are still working on, or to exclude a computation-only parameter inside a Record.

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
</br></br></br>

## `[Key]`

|Item|Details|
|-|-|
|Target|Record parameter|
|Argument|None|
|Allow multiple|X (one per Record)|
|Validation timing|Scanner (requirement in a Map Value Record), extraction (duplicate check), load|

There are two places where `[Key]` carries meaning.

- **The Value Record of a Map (`FrozenDictionary`)** — required to indicate where the Dictionary key should be drawn from. Without it, the scanner rejects it with the `KeyAttributeRequiredInDictionaryValue` diagnostic. For a detailed example, see [5.1 Map (FrozenDictionary)](./01-schemata.md#map-frozendictionary).
- **The duplicate check of `ExcelColumnExtractor`** — checks for duplicate values in the column marked with `[Key]` at the extraction stage. Without it, the check itself is skipped.

Additional rules:

- A Record may have at most one `[Key]` overall (the scanner raises a `StaticDataRecordMustHaveAtMostOneKey` exception).
- A parameter marked with `[Key]` must be non-nullable (the scanner raises a `KeyAttributeMustBeNonNullable` exception).
- Attaching `[Key]` to an enum parameter omits the `Enum.IsDefined` check during mapping — see [5.3 Type Branding Pattern](./03-type-branding.md).

---

<a id="attr-length"></a>
</br></br></br>

## `[Length(length)]`

|Item|Details|
|-|-|
|Target|Collection parameter (`ImmutableArray<T>`, `FrozenSet<T>`, `FrozenDictionary<K,V>`)|
|Argument|`length` — fixed length|
|Allow multiple|X|
|Validation timing|Scanner|
|If omitted|If `[SingleColumnCollection]` is also absent, the scanner raises a `LengthAttributeRequired` exception|
|Exclusivity|Cannot be used together with `[SingleColumnCollection]` or `[CountRange]`|

This is the multi-column scheme where Excel headers expand to `Col[0]`, `Col[1]`, ..., `Col[length-1]`.

```csharp
[StaticDataRecord("GameItems", "Items")]
public sealed record ItemRecord(
    int Id,
    string Name,
    [Length(3)] ImmutableArray<string> Tags);
```

For a detailed example, see [5.1 Collections](./01-schemata.md#collections).

---

<a id="attr-nullstring"></a>
</br></br></br>

## `[NullString(nullString)]`

|Item|Details|
|-|-|
|Target|Nullable parameter (or a collection with nullable elements)|
|Argument|`nullString` — the string representation that means null|
|Allow multiple|X|
|Validation timing|Scanner (presence), load (substitution)|
|If omitted|The scanner raises a `NullStringAttributeRequiredForNullable` (or ...Array, ...Set, ...Map) exception|
|Misuse|If attached to a non-nullable type, the scanner raises a `NullStringAttributeNotAllowed` exception|

If a CSV cell value matches this string, it is interpreted as `null`. Common values are `"NULL"`, `""`, `"N/A"`, and so on.

```csharp
[StaticDataRecord("GameItems", "Items")]
public sealed record ItemRecord(
    int Id,
    string Name,
    [NullString("NULL")] string? Description);
```

If the `Description` cell is `NULL`, it maps to `null`; any other string is mapped as-is.

---

<a id="attr-range"></a>
</br></br></br>

## `[Range(min, max)]`

|Item|Details|
|-|-|
|Target|Numeric, `char`, `DateTime`, `TimeSpan`, `string`, `enum` parameters (including each nullable variant)|
|Arguments|Three overloads: `(int, int)`, `(double, double)`, `(Type, string, string)`|
|Allow multiple|X|
|Validation timing|Extraction (`SchemaInfoScanner`'s `RangeAttributeChecker`), load (`Sdp.Csv.RangeValidator`)|
|Attached to an inapplicable type|The scanner raises a `RangeAttributeNotApplicable` exception (e.g. `bool` / `bool?` / collection / record)|
|If omitted|Proceeds without a range check|

This type inherits from `System.ComponentModel.DataAnnotations.RangeAttribute`. If a value falls outside the range, it fails with an `ArgumentOutOfRangeException` in both the cell-compatibility check of `ExcelColumnExtractor` and the runtime load. The two stages use the same boundary-interpretation rules — `string` uses ordinal comparison (`CompareOrdinal`), `DateTime` / `TimeSpan` use the format of `[DateTimeFormat]` / `[TimeSpanFormat]`, and `enum` uses the underlying integer — so the check results agree.

Numeric types use the two-argument overloads directly.

```csharp
[StaticDataRecord("GameItems", "Items")]
public sealed record ItemRecord(
    int Id,
    string Name,
    [Range(0, 1_000_000)] int Price);
```

Non-numeric types specify the boundaries with the `(Type, string, string)` overload. The boundary strings are interpreted according to the parsing rules of the corresponding type.

```csharp
// DateTime — write the boundaries in the format of [DateTimeFormat]
[DateTimeFormat("yyyy-MM-dd")]
[Range(typeof(DateTime), "2024-01-01", "2024-12-31")]
DateTime EventDate;

// TimeSpan — write the boundaries in the format of [TimeSpanFormat]
[TimeSpanFormat("c")]
[Range(typeof(TimeSpan), "00:00:00", "01:00:00")]
TimeSpan Duration;

// string — ordinal comparison (CompareOrdinal, culture-independent)
[Range(typeof(string), "apple", "zebra")]
string Tag;

// enum — specify boundaries by member name. Compared by the underlying integer
[Range(typeof(Tier), "Low", "High")]
Tier Grade;

// Key enum — specify boundaries as underlying integer strings
[Key]
[Range(typeof(ItemId), "100", "1000")]
ItemId Id;
```

Nullable variants (`int?`, `DateTime?`, `string?`, `Tier?`, etc.) are also supported as-is. If the cell value is matched by `[NullString]`, the Range check is skipped; for a non-null value, the Range check of the inner type is applied as usual.

---

<a id="attr-regularexpression"></a>
</br></br></br>

## `[RegularExpression(pattern)]`

|Item|Details|
|-|-|
|Target|`string` or `string?` parameter|
|Argument|`pattern` — a [.NET regular expression pattern](https://learn.microsoft.com/dotnet/standard/base-types/regular-expression-language-quick-reference)|
|Allow multiple|X|
|Validation timing|Scanner (type check), extraction / load (`Regex.IsMatch`)|
|If omitted|Proceeds without a regular expression check|
|Misuse|If attached to a type other than `string` / `string?`, the scanner raises a `RegularExpressionAttributeOnlyForString` exception|

Inherits from `System.ComponentModel.DataAnnotations.RegularExpressionAttribute`. If there is a value that does not match the pattern, it fails in both the cell-compatibility check of `ExcelColumnExtractor` and the runtime load. When attached to a `string?`, if the cell value is matched by `[NullString]`, it is interpreted as `null` and the pattern check is skipped.

```csharp
[StaticDataRecord("GameItems", "Items")]
public sealed record ItemRecord(
    int Id,
    string Name,
    [RegularExpression(@"^icons/[a-z]+\.png$")] string IconPath);
```

---

<a id="attr-singlecolumncollection"></a>
</br></br></br>

## `[SingleColumnCollection(separator = ",")]`

|Item|Details|
|-|-|
|Target|`ImmutableArray<T>` or `FrozenSet<T>` (not allowed on a Dictionary)|
|Argument|`separator` (default `","`)|
|Allow multiple|X|
|Validation timing|Scanner / load|
|If omitted|If `[Length]` is also absent, the scanner raises a `LengthAttributeRequired` exception|
|Exclusivity|Cannot be used together with `[Length]`|
|Note|If the element is a Record, the scanner raises a `SingleColumnArrayOnlyPrimitive` (Array) or `SingleColumnHashSetOnlyPrimitive` (Set) exception. If attached to a Map (FrozenDictionary), it raises `SingleColumnCollectionNotForMap`|

This scheme packs multiple values into a single cell in the form `"a,b,c"`. If you need a constraint on the number of split elements, also attach [`[CountRange]`](#attr-countrange) (optional).

```csharp
[StaticDataRecord("GameItems", "Items")]
public sealed record ItemRecord(
    int Id,
    string Name,
    [SingleColumnCollection(",")] ImmutableArray<string> Tags);
```

---

<a id="attr-staticdatarecord"></a>
</br></br></br>

## `[StaticDataRecord(excelFileName, sheetName, startCell?)]`

|Item|Details|
|-|-|
|Target|Record class|
|Arguments|`excelFileName` (without extension), `sheetName`, `startCell` (optional, default `null`)|
|Validation timing|Required at each stage: scanner / extraction / load|
|If omitted|`ExcelColumnExtractor` terminates with `StaticDataRecordAttributeNotFound` when there is no Record to extract. During CSV load, if the target table's Record does not have it, a `StaticDataRecordAttributeRequired` exception is raised.|

A Record without this Attribute is treated as an "auxiliary Record that is not a static data table target" and is excluded from extraction/load.

```csharp
[StaticDataRecord("GameItems", "Items")]
public sealed record ItemRecord(int Id, string Name);
```

The third argument, `startCell`, specifies the header start cell of the sheet this Record corresponds to, on a per-Record basis. If specified, it takes precedence over the `--start-cell` option of `ExcelColumnExtractor`. This is useful when most sheets in a project use the same start cell but only a few start at a different position.

```csharp
// Unlike the other sheets, the header of this sheet alone starts at B3
[StaticDataRecord("GameItems", "Quests", "B3")]
public sealed record QuestRecord(int Id, string Title);
```

---

<a id="attr-switchforeignkey"></a>
</br></br></br>

## `[SwitchForeignKey(conditionColumnName, conditionValue, tableSetName, recordColumnName)]`

|Item|Details|
|-|-|
|Target|Record parameter|
|Arguments|`conditionColumnName`, `conditionValue`, `tableSetName`, `recordColumnName`|
|Allow multiple|O (`AllowMultiple = true`)|
|Validation timing|Scanner (blocks simultaneous FK/SFK attachment, blocks duplicate conditions) + load (re-checks simultaneous FK/SFK attachment, re-checks duplicate conditions, target validation, reference validation)|
|Attached together with `[ForeignKey]`|Both the scanner and load reject it with the `FkSwitchFkConflict` diagnostic|
|The same `(conditionColumnName, conditionValue)` attached more than once|The scanner rejects it with `SwitchForeignKeyDuplicateCondition`, and load rejects it with `SwitchFkDuplicateConditionValue` (the message keys differ)|
|Target not present in the TableSet|Raises an `FkTargetNotFound` exception at load|
|Target is a `[SingleColumnCollection]` column|Raises an `FkTargetIsSingleColumnCollection` exception at load|
|`conditionColumnName` not present in the same Record|Raises a `SwitchFkConditionColumnNotFound` exception during the target resolution stage after table load|
|Target column name does not exist|Raises an `FkTargetColumnNotFound` exception during the target resolution stage after table load|
|The condition column value matches no branch|Raises a `SwitchFkConditionValueNotMatched` exception during the value validation stage|
|Value validation failure|Raises an `FkValueNotFound` exception (including the condition value) inside `AggregateException(FkValidationFailed, ...)`|

Use this when the same parameter value must **reference a different table depending on the value of another column**. For the detailed flow and examples, see [3.6 Foreign Keys](../03-usage/06-foreign-keys.md).

The load-stage diagnostics above are also not thrown individually — `FkTargetNotFound`, `FkTargetIsSingleColumnCollection`, `SwitchFkConditionColumnNotFound`, `FkTargetColumnNotFound`, `SwitchFkConditionValueNotMatched`, and `FkValueNotFound` are all gathered into the `InnerExceptions` of `AggregateException(FkValidationFailed, ...)` and reported at once.

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
</br></br></br>

## `[TimeSpanFormat(format)]`

|Item|Details|
|-|-|
|Target|A `TimeSpan` or `TimeSpan?` typed parameter (including collection elements)|
|Argument|`format` — a .NET standard TimeSpan format string ([standard](https://learn.microsoft.com/dotnet/standard/base-types/standard-timespan-format-strings), [custom](https://learn.microsoft.com/dotnet/standard/base-types/custom-timespan-format-strings))|
|Allow multiple|X|
|Validation timing|Scanner (presence), extraction (`TimeSpan.TryParseExact`), load (`TimeSpan.ParseExact`)|
|If omitted|The scanner raises a `TimeSpanFormatAttributeRequired` exception|
|Misuse|If attached to a non-`TimeSpan` type, the scanner raises a `TimeSpanFormatAttributeNotApplicable` exception|

`TimeSpan` likewise cannot be used without this Attribute. Because both the extraction-stage cell-value validation and the runtime mapping call `ParseExact` with the same `format`, a representation that deviates from the format fails on both sides.

```csharp
[StaticDataRecord("GameItems", "Items")]
public sealed record ItemRecord(
    int Id,
    string Name,
    [TimeSpanFormat(@"hh\:mm\:ss")] TimeSpan Cooldown);
```

---

[← Previous: 5.1 Supported Types (Schemata)](./01-schemata.md) | [Table of Contents](../README.md) | [Next: 5.3 Type Branding Pattern →](./03-type-branding.md)
