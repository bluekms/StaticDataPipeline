# 5.2 Attribute 目录

按字母顺序整理 Sdp 提供的 Attribute。

各 Attribute 的验证时机标注中，“**扫描器**”指 `SchemaInfoScanner` (Roslyn 分析)，“**提取**”指 `ExcelColumnExtractor` 的单元格值验证阶段，“**加载**”指 `StaticDataManager.LoadAsync` 运行时。

## 目录

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

|项目|内容|
|-|-|
|目标|Record 参数|
|参数|`name` — 表头名称|
|允许多个|X|
|验证规则|无 — 仅用于确定和匹配表头名称 (扫描器的 `RecordFlattener`、表头生成、CSV 映射)|
|省略时|参数名直接作为表头名称|

当你希望表头名称与参数名不同时使用。附加到集合参数上时，它会成为展开后表头的**前缀**。

```csharp
[StaticDataRecord("GameItems", "Items")]
public sealed record ItemRecord(
    int Id,
    [ColumnName("ItemName")] string Name,
    [ColumnName("Scores")]
    [Length(3)] ImmutableArray<int> ScoreList);
```

上例中表头会展开为 `Id`、`ItemName`、`Scores[0]`、`Scores[1]`、`Scores[2]`。

---

<a id="attr-countrange"></a>
</br></br></br>

## `[CountRange(minCount, maxCount)]`

|项目|内容|
|-|-|
|目标|附加了 `[SingleColumnCollection]` 的集合参数|
|参数|`minCount` (≥ 1)、`maxCount`|
|允许多个|X|
|验证时机|扫描器 (一致性)、提取 / 加载 (拆分个数)|
|缺少 `[SingleColumnCollection]`|扫描器抛出 `CountRangeAttributeOnlyForSingleColumnCollection` 异常|
|同时附加 `[Length]`|扫描器抛出 `CountRangeAndLengthMutuallyExclusive` 异常|
|`minCount` 为 0 或更小|扫描器抛出 `CountRangeMinMustBePositive` 异常。`minCount=0` 等同于“无下限约束”，因此没有意义|

单列模式集合拆分后的元素个数必须落在 `[minCount, maxCount]` 范围内。拆分个数在提取阶段和运行时加载两侧都会检查。

```csharp
[StaticDataRecord("GameItems", "Items")]
public sealed record ItemRecord(
    int Id,
    string Name,
    [SingleColumnCollection(",")][CountRange(1, 5)] ImmutableArray<string> Tags);
```

`Tags` 单元格的值拆分后必须为 `1` ~ `5` 个。

---

<a id="attr-datetimeformat"></a>
</br></br></br>

## `[DateTimeFormat(format)]`

|项目|内容|
|-|-|
|目标|`DateTime` 或 `DateTime?` 类型的参数 (包括集合元素)|
|参数|`format` — .NET 标准日期/时间格式字符串 ([标准](https://learn.microsoft.com/dotnet/standard/base-types/standard-date-and-time-format-strings)、[自定义](https://learn.microsoft.com/dotnet/standard/base-types/custom-date-and-time-format-strings))|
|允许多个|X|
|验证时机|扫描器 (是否存在)、提取 (`DateTime.TryParseExact`)、加载 (`DateTime.ParseExact`)|
|省略时|扫描器抛出 `DateTimeFormatAttributeRequired` 异常|
|误用|附加到非 `DateTime` 类型时，扫描器抛出 `DateTimeFormatAttributeNotApplicable` 异常|

没有此 Attribute 就无法使用 `DateTime`。由于提取阶段的单元格值验证和运行时映射都使用相同的 `format` 调用 `ParseExact`，因此与 format 不符的写法会在两侧都失败。

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

|项目|内容|
|-|-|
|目标|Record 参数|
|参数|`tableSetName` — TableSet 的属性 (= 构造函数参数) 名。`recordColumnName` — 目标 Record 的属性名。|
|允许多个|X — 目标不止一个时，增加区分列并使用 `[SwitchForeignKey]`|
|验证时机|扫描器 (阻止 FK/SFK 同时附加) + 加载 (重新检查 FK/SFK 同时附加、目标验证、引用验证)|
|与 `[SwitchForeignKey]` 同时附加|扫描器和加载两侧均以 `FkSwitchFkConflict` 诊断拒绝|
|目标不存在于 TableSet 中|加载时抛出 `FkTargetNotFound` 异常|
|目标是 `[SingleColumnCollection]` 列|加载时抛出 `FkTargetIsSingleColumnCollection` 异常|
|目标列名不存在|在表加载后的目标解析阶段抛出 `FkTargetColumnNotFound` 异常|
|值验证失败|在 `AggregateException(FkValidationFailed, ...)` 内部抛出 `FkValueNotFound` 异常|

上述加载阶段诊断不会单独抛出 — `FkTargetNotFound`、`FkTargetIsSingleColumnCollection`、`FkTargetColumnNotFound`、`FkValueNotFound` 全部汇集到 `AggregateException(FkValidationFailed, ...)` 的 `InnerExceptions` 中并一次性通知。

详细流程和示例请参考 [3.6 外键](../03-usage/06-foreign-keys.md)。

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

|项目|内容|
|-|-|
|目标|Record 类 **或** Record 参数|
|参数|无|
|允许多个|X|
|验证时机|扫描器 (应用时跳过)|

扫描器会跳过受影响的 Record 或参数。用于临时排除你仍在处理中的 Record，或排除 Record 内部仅用于计算的参数。

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

|项目|内容|
|-|-|
|目标|Record 参数|
|参数|无|
|允许多个|X (每个 Record 一个)|
|验证时机|扫描器 (在 Map Value Record 中的必需性)、提取 (重复检查)、加载|

`[Key]` 具有意义的地方有两处。

- **Map (`FrozenDictionary`) 的 Value Record** — 必需，用于告知 Dictionary 的键应从何处提取。若缺失，扫描器以 `KeyAttributeRequiredInDictionaryValue` 诊断拒绝。详细示例请参考 [5.1 Map (FrozenDictionary)](./01-schemata.md#map-frozendictionary)。
- **ExcelColumnExtractor 的重复检查** — 在提取阶段检查附加了 `[Key]` 的列的值是否重复。若缺失，则跳过检查本身。

附加规则：

- 一个 Record 整体最多只能有一个 `[Key]` (扫描器抛出 `StaticDataRecordMustHaveAtMostOneKey` 异常)。
- 附加了 `[Key]` 的参数必须是 non-nullable (扫描器抛出 `KeyAttributeMustBeNonNullable` 异常)。
- 给 enum 参数附加 `[Key]` 会在映射时省略 `Enum.IsDefined` 检查 — 请参考 [5.3 类型品牌化模式](./03-type-branding.md)。

---

<a id="attr-length"></a>
</br></br></br>

## `[Length(length)]`

|项目|内容|
|-|-|
|目标|集合参数 (`ImmutableArray<T>`、`FrozenSet<T>`、`FrozenDictionary<K,V>`)|
|参数|`length` — 固定长度|
|允许多个|X|
|验证时机|扫描器|
|省略时|若 `[SingleColumnCollection]` 也缺失，扫描器抛出 `LengthAttributeRequired` 异常|
|排他关系|不能与 `[SingleColumnCollection]`、`[CountRange]` 同时使用|

这是 Excel 表头展开为 `Col[0]`、`Col[1]`、...、`Col[length-1]` 的多列方式。

```csharp
[StaticDataRecord("GameItems", "Items")]
public sealed record ItemRecord(
    int Id,
    string Name,
    [Length(3)] ImmutableArray<string> Tags);
```

详细示例请参考 [5.1 集合](./01-schemata.md#集合)。

---

<a id="attr-nullstring"></a>
</br></br></br>

## `[NullString(nullString)]`

|项目|内容|
|-|-|
|目标|Nullable 参数 (或含有 Nullable 元素的集合)|
|参数|`nullString` — 表示 null 的字符串表现|
|允许多个|X|
|验证时机|扫描器 (是否存在)、加载 (替换)|
|省略时|扫描器抛出 `NullStringAttributeRequiredForNullable` (或 ...Array、...Set、...Map) 异常|
|误用|附加到 non-nullable 类型时，扫描器抛出 `NullStringAttributeNotAllowed` 异常|

如果 CSV 单元格的值与此字符串匹配，则解释为 `null`。常用值有 `"NULL"`、`""`、`"N/A"` 等。

```csharp
[StaticDataRecord("GameItems", "Items")]
public sealed record ItemRecord(
    int Id,
    string Name,
    [NullString("NULL")] string? Description);
```

如果 `Description` 单元格为 `NULL`，则映射为 `null`；其他字符串则按原样映射。

---

<a id="attr-range"></a>
</br></br></br>

## `[Range(min, max)]`

|项目|内容|
|-|-|
|目标|数值类型、`char`、`DateTime`、`TimeSpan`、`string`、`enum` 参数 (包括各自的 nullable 变体)|
|参数|三种重载：`(int, int)`、`(double, double)`、`(Type, string, string)`|
|允许多个|X|
|验证时机|提取 (`SchemaInfoScanner` 的 `RangeAttributeChecker`)、加载 (`Sdp.Csv.RangeValidator`)|
|附加到不适用的类型|扫描器抛出 `RangeAttributeNotApplicable` 异常 (例如 `bool` / `bool?` / 集合 / record)|
|省略时|不做范围检查继续进行|

此类型继承自 `System.ComponentModel.DataAnnotations.RangeAttribute`。如果值超出范围，会在 `ExcelColumnExtractor` 的单元格兼容性检查和运行时加载两侧均以 `ArgumentOutOfRangeException` 失败。两个阶段使用相同的边界解释规则 — `string` 使用序数比较 (`CompareOrdinal`)，`DateTime` / `TimeSpan` 使用 `[DateTimeFormat]` / `[TimeSpanFormat]` 的格式，`enum` 使用 underlying 整数 — 因此检查结果一致。

数值类型直接使用双参数重载。

```csharp
[StaticDataRecord("GameItems", "Items")]
public sealed record ItemRecord(
    int Id,
    string Name,
    [Range(0, 1_000_000)] int Price);
```

非数值类型用 `(Type, string, string)` 重载显式指定边界。边界字符串会按对应类型的解析规则进行解释。

```csharp
// DateTime — 以 [DateTimeFormat] 的格式书写边界
[DateTimeFormat("yyyy-MM-dd")]
[Range(typeof(DateTime), "2024-01-01", "2024-12-31")]
DateTime EventDate;

// TimeSpan — 以 [TimeSpanFormat] 的格式书写边界
[TimeSpanFormat("c")]
[Range(typeof(TimeSpan), "00:00:00", "01:00:00")]
TimeSpan Duration;

// string — 序数比较 (CompareOrdinal，与文化无关)
[Range(typeof(string), "apple", "zebra")]
string Tag;

// enum — 用成员名指定边界。按 underlying 整数比较
[Range(typeof(Tier), "Low", "High")]
Tier Grade;

// Key enum — 用 underlying 整数字符串指定边界
[Key]
[Range(typeof(ItemId), "100", "1000")]
ItemId Id;
```

Nullable 变体 (`int?`、`DateTime?`、`string?`、`Tier?` 等) 同样原样支持。如果 cell value 被 `[NullString]` 匹配，则跳过 Range 检查；对于 non-null 值，则照常应用 inner 类型的 Range 检查。

---

<a id="attr-regularexpression"></a>
</br></br></br>

## `[RegularExpression(pattern)]`

|项目|内容|
|-|-|
|目标|`string` 或 `string?` 参数|
|参数|`pattern` — [.NET 正则表达式模式](https://learn.microsoft.com/dotnet/standard/base-types/regular-expression-language-quick-reference)|
|允许多个|X|
|验证时机|扫描器 (类型确认)、提取 / 加载 (`Regex.IsMatch`)|
|省略时|不做正则表达式检查继续进行|
|误用|附加到 `string` / `string?` 以外的类型时，扫描器抛出 `RegularExpressionAttributeOnlyForString` 异常|

继承自 `System.ComponentModel.DataAnnotations.RegularExpressionAttribute`。如果存在与模式不匹配的值，会在 `ExcelColumnExtractor` 的单元格兼容性检查和运行时加载两侧均失败。附加到 `string?` 时，如果单元格值被 `[NullString]` 匹配，则解释为 `null` 并跳过模式检查。

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

|项目|内容|
|-|-|
|目标|`ImmutableArray<T>` 或 `FrozenSet<T>` (不允许用于 Dictionary)|
|参数|`separator` (默认值 `","`)|
|允许多个|X|
|验证时机|扫描器 / 加载|
|省略时|若 `[Length]` 也缺失，扫描器抛出 `LengthAttributeRequired` 异常|
|排他关系|不能与 `[Length]` 同时使用|
|备注|如果元素是 Record，扫描器抛出 `SingleColumnArrayOnlyPrimitive` (Array)、`SingleColumnHashSetOnlyPrimitive` (Set) 异常。附加到 Map (FrozenDictionary) 时抛出 `SingleColumnCollectionNotForMap`|

这是以 `"a,b,c"` 形式将多个值塞入一个单元格的方式。如果需要对拆分后元素个数加以约束，可一并附加 [`[CountRange]`](#attr-countrange) (可选)。

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

|项目|内容|
|-|-|
|目标|Record 类|
|参数|`excelFileName` (不含扩展名)、`sheetName`、`startCell` (可选，默认 `null`)|
|验证时机|在扫描器 / 提取 / 加载各阶段均需要|
|省略时|当没有任何可提取的 Record 时，`ExcelColumnExtractor` 以 `StaticDataRecordAttributeNotFound` 终止。CSV 加载时，如果目标表的 Record 没有它，则抛出 `StaticDataRecordAttributeRequired` 异常。|

没有此 Attribute 的 Record 被视为“不是静态数据表目标的辅助 Record”，会从提取/加载对象中排除。

```csharp
[StaticDataRecord("GameItems", "Items")]
public sealed record ItemRecord(int Id, string Name);
```

第三个参数 `startCell` 按 Record 为单位指定此 Record 对应工作表的表头起始单元格。指定后，它优先于 `ExcelColumnExtractor` 的 `--start-cell` 选项。当一个项目中大多数工作表使用相同的起始单元格，但只有少数从不同位置开始时，此参数很有用。

```csharp
// 与其他工作表不同，仅此工作表的表头从 B3 开始
[StaticDataRecord("GameItems", "Quests", "B3")]
public sealed record QuestRecord(int Id, string Title);
```

---

<a id="attr-switchforeignkey"></a>
</br></br></br>

## `[SwitchForeignKey(conditionColumnName, conditionValue, tableSetName, recordColumnName)]`

|项目|内容|
|-|-|
|目标|Record 参数|
|参数|`conditionColumnName`、`conditionValue`、`tableSetName`、`recordColumnName`|
|允许多个|O (`AllowMultiple = true`)|
|验证时机|扫描器 (阻止 FK/SFK 同时附加、阻止重复条件) + 加载 (重新检查 FK/SFK 同时附加、重新检查重复条件、目标验证、引用验证)|
|与 `[ForeignKey]` 同时附加|扫描器和加载两侧均以 `FkSwitchFkConflict` 诊断拒绝|
|同一 `(conditionColumnName, conditionValue)` 附加两次以上|扫描器以 `SwitchForeignKeyDuplicateCondition`、加载以 `SwitchFkDuplicateConditionValue` 诊断拒绝 (消息键互不相同)|
|目标不存在于 TableSet 中|加载时抛出 `FkTargetNotFound` 异常|
|目标是 `[SingleColumnCollection]` 列|加载时抛出 `FkTargetIsSingleColumnCollection` 异常|
|`conditionColumnName` 不存在于同一 Record 中|在表加载后的目标解析阶段抛出 `SwitchFkConditionColumnNotFound` 异常|
|目标列名不存在|在表加载后的目标解析阶段抛出 `FkTargetColumnNotFound` 异常|
|条件列的值不匹配任何分支|在值验证阶段抛出 `SwitchFkConditionValueNotMatched` 异常|
|值验证失败|在 `AggregateException(FkValidationFailed, ...)` 内部抛出 `FkValueNotFound` (包含条件值) 异常|

当同一参数值需要 **根据另一列的值引用不同的表** 时使用。详细流程和示例请参考 [3.6 外键](../03-usage/06-foreign-keys.md)。

上述加载阶段诊断同样不会单独抛出 — `FkTargetNotFound`、`FkTargetIsSingleColumnCollection`、`SwitchFkConditionColumnNotFound`、`FkTargetColumnNotFound`、`SwitchFkConditionValueNotMatched`、`FkValueNotFound` 全部汇集到 `AggregateException(FkValidationFailed, ...)` 的 `InnerExceptions` 中并一次性通知。

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

|项目|内容|
|-|-|
|目标|`TimeSpan` 或 `TimeSpan?` 类型的参数 (包括集合元素)|
|参数|`format` — .NET 标准 TimeSpan 格式字符串 ([标准](https://learn.microsoft.com/dotnet/standard/base-types/standard-timespan-format-strings)、[自定义](https://learn.microsoft.com/dotnet/standard/base-types/custom-timespan-format-strings))|
|允许多个|X|
|验证时机|扫描器 (是否存在)、提取 (`TimeSpan.TryParseExact`)、加载 (`TimeSpan.ParseExact`)|
|省略时|扫描器抛出 `TimeSpanFormatAttributeRequired` 异常|
|误用|附加到非 `TimeSpan` 类型时，扫描器抛出 `TimeSpanFormatAttributeNotApplicable` 异常|

`TimeSpan` 同样在没有此 Attribute 的情况下无法使用。由于提取阶段的单元格值验证和运行时映射都使用相同的 `format` 调用 `ParseExact`，因此与 format 不符的写法会在两侧都失败。

```csharp
[StaticDataRecord("GameItems", "Items")]
public sealed record ItemRecord(
    int Id,
    string Name,
    [TimeSpanFormat(@"hh\:mm\:ss")] TimeSpan Cooldown);
```

---

[← 上一篇：5.1 支持的类型 (Schemata)](./01-schemata.md) | [目录](../README.md) | [下一篇：5.3 类型品牌化模式 →](./03-type-branding.md)
