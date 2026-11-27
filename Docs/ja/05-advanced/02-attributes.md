# 5.2 Attribute カタログ

Sdp が提供する Attribute をアルファベット順に整理します。

各 Attribute の検証タイミング表記のうち「**スキャナ**」は `SchemaInfoScanner` (Roslyn 解析)、「**抽出**」は `ExcelColumnExtractor` のセル値検証ステージ、「**ロード**」は `StaticDataManager.LoadAsync` ランタイムを意味します。

## 目次

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

|項目|内容|
|-|-|
|対象|Record パラメーター|
|引数|`name` — ヘッダー名|
|複数許可|X|
|検証ルール|なし — ヘッダー名を決定・マッチングするためだけに使用 (スキャナの `RecordFlattener`、ヘッダー生成、CSV マッピング)|
|省略時|パラメーター名がそのままヘッダー名|

ヘッダー名をパラメーター名と異なるものにしたいときに使用します。コレクションパラメーターに付けると、展開されたヘッダーの **接頭辞** になります。

```csharp
[StaticDataRecord("GameItems", "Items")]
public sealed record ItemRecord(
    int Id,
    [ColumnName("ItemName")] string Name,
    [ColumnName("Scores")]
    [Length(3)] ImmutableArray<int> ScoreList);
```

上の例ではヘッダーが `Id`、`ItemName`、`Scores[0]`、`Scores[1]`、`Scores[2]` に展開されます。

---

<a id="attr-countrange"></a>
</br></br></br>

## `[CountRange(minCount, maxCount)]`

|項目|内容|
|-|-|
|対象|`[SingleColumnCollection]` が付いたコレクションパラメーター|
|引数|`minCount` (≥ 1)、`maxCount`|
|複数許可|X|
|検証タイミング|スキャナ (整合性)、抽出 / ロード (分割個数)|
|`[SingleColumnCollection]` 欠落|スキャナが `CountRangeAttributeOnlyForSingleColumnCollection` 例外を発生|
|`[Length]` 同時付与|スキャナが `CountRangeAndLengthMutuallyExclusive` 例外を発生|
|`minCount` が 0 以下|スキャナが `CountRangeMinMustBePositive` 例外を発生。`minCount=0` は「下限制約なし」と等価で意味がないため|

単一カラムモードのコレクションの分割された要素数が `[minCount, maxCount]` の範囲内になければなりません。分割個数は抽出ステージとランタイムロードの両方で検査します。

```csharp
[StaticDataRecord("GameItems", "Items")]
public sealed record ItemRecord(
    int Id,
    string Name,
    [SingleColumnCollection(",")][CountRange(1, 5)] ImmutableArray<string> Tags);
```

`Tags` セルの値が分割されて `1` ～ `5` 個でなければなりません。

---

<a id="attr-datetimeformat"></a>
</br></br></br>

## `[DateTimeFormat(format)]`

|項目|内容|
|-|-|
|対象|`DateTime` または `DateTime?` 型のパラメーター (コレクション要素を含む)|
|引数|`format` — .NET 標準の日付/時刻フォーマット文字列 ([標準](https://learn.microsoft.com/dotnet/standard/base-types/standard-date-and-time-format-strings)、[カスタム](https://learn.microsoft.com/dotnet/standard/base-types/custom-date-and-time-format-strings))|
|複数許可|X|
|検証タイミング|スキャナ (存在有無)、抽出 (`DateTime.TryParseExact`)、ロード (`DateTime.ParseExact`)|
|省略時|スキャナが `DateTimeFormatAttributeRequired` 例外を発生|
|誤用|非 `DateTime` 型に付けるとスキャナが `DateTimeFormatAttributeNotApplicable` 例外を発生|

`DateTime` はこの Attribute なしでは使用できません。抽出時のセル値検証とランタイムマッピングの両方が同じ `format` で `ParseExact` を呼び出すため、format から外れた表記は両方で失敗します。

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

|項目|内容|
|-|-|
|対象|Record パラメーター|
|引数|`tableSetName` — TableSet のプロパティ (= コンストラクターパラメーター) 名。`recordColumnName` — 対象 Record のプロパティ名。|
|複数許可|X — 対象が複数ある場合は区分カラムを置いて `[SwitchForeignKey]` を使う|
|検証タイミング|スキャナ (FK/SFK 同時付与のブロック) + ロード (FK/SFK 同時付与の再確認、ターゲット検証、参照検証)|
|`[SwitchForeignKey]` と同時付与|スキャナとロードの両方が `FkSwitchFkConflict` 診断で拒否|
|ターゲットが TableSet に存在しない|ロード時に `FkTargetNotFound` 例外を発生|
|ターゲットが `[SingleColumnCollection]` カラム|ロード時に `FkTargetIsSingleColumnCollection` 例外を発生|
|ターゲットのカラム名が存在しない|テーブルロード後のターゲット解決ステージで `FkTargetColumnNotFound` 例外を発生|
|値検証の失敗|`AggregateException(FkValidationFailed, ...)` の内部に `FkValueNotFound` 例外を発生|

上記のロードステージ診断は単独でスローされません — `FkTargetNotFound`、`FkTargetIsSingleColumnCollection`、`FkTargetColumnNotFound`、`FkValueNotFound` はすべて `AggregateException(FkValidationFailed, ...)` の `InnerExceptions` に集約され一度に通知されます。

詳細なフローと例は [3.6 外部キー](../03-usage/06-foreign-keys.md) を参照してください。

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

|項目|内容|
|-|-|
|対象|Record クラス **または** Record パラメーター|
|引数|なし|
|複数許可|X|
|検証タイミング|スキャナ (適用時にスキップ)|

スキャナが該当する Record またはパラメーターをスキップします。作業中の Record を一時的に外したり、Record 内部の計算用パラメーターを除外するときに使用します。

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

|項目|内容|
|-|-|
|対象|Record パラメーター|
|引数|なし|
|複数許可|X (Record ごとに一つ)|
|検証タイミング|スキャナ (Map Value Record での必須性)、抽出 (重複検査)、ロード|

`[Key]` が意味を持つ箇所は二つあります。

- **Map (`FrozenDictionary`) の Value Record** — Dictionary のキーをどこから取り出すかを知らせるために必須。なければスキャナが `KeyAttributeRequiredInDictionaryValue` 診断で拒否。詳しい例は [5.1 Map (FrozenDictionary)](./01-schemata.md#map-frozendictionary) を参照。
- **ExcelColumnExtractor の重複検査** — `[Key]` が付いたカラムの値の重複を抽出ステージで検査します。なければ検査自体をスキップ。

付随ルール:

- Record 全体で `[Key]` は最大一つです (スキャナが `StaticDataRecordMustHaveAtMostOneKey` 例外を発生)。
- `[Key]` が付いたパラメーターは non-nullable でなければなりません (スキャナが `KeyAttributeMustBeNonNullable` 例外を発生)。
- enum パラメーターに `[Key]` を付けるとマッピング時の `Enum.IsDefined` 検査が省略されます — [5.3 型ブランディングパターン](./03-type-branding.md) を参照。

---

<a id="attr-length"></a>
</br></br></br>

## `[Length(length)]`

|項目|内容|
|-|-|
|対象|コレクションパラメーター (`ImmutableArray<T>`、`FrozenSet<T>`、`FrozenDictionary<K,V>`)|
|引数|`length` — 固定長|
|複数許可|X|
|検証タイミング|スキャナ|
|省略時|`[SingleColumnCollection]` もなければスキャナが `LengthAttributeRequired` 例外を発生|
|排他関係|`[SingleColumnCollection]`、`[CountRange]` と同時使用不可|

Excel ヘッダーが `Col[0]`、`Col[1]`、...、`Col[length-1]` に展開される複数カラム方式です。

```csharp
[StaticDataRecord("GameItems", "Items")]
public sealed record ItemRecord(
    int Id,
    string Name,
    [Length(3)] ImmutableArray<string> Tags);
```

詳しい例は [5.1 コレクション](./01-schemata.md#コレクション) を参照してください。

---

<a id="attr-nullstring"></a>
</br></br></br>

## `[NullString(nullString)]`

|項目|内容|
|-|-|
|対象|Nullable パラメーター (または Nullable 要素を持つコレクション)|
|引数|`nullString` — null を意味する文字列表現|
|複数許可|X|
|検証タイミング|スキャナ (存在有無)、ロード (置換)|
|省略時|スキャナが `NullStringAttributeRequiredForNullable` (または ...Array、...Set、...Map) 例外を発生|
|誤用|Non-nullable に付けるとスキャナが `NullStringAttributeNotAllowed` 例外を発生|

CSV セルの値がこの文字列と一致すれば `null` と解釈します。よく `"NULL"`、`""`、`"N/A"` などを使います。

```csharp
[StaticDataRecord("GameItems", "Items")]
public sealed record ItemRecord(
    int Id,
    string Name,
    [NullString("NULL")] string? Description);
```

`Description` セルが `NULL` であれば `null`、それ以外の文字列であればそのままマッピングされます。

---

<a id="attr-range"></a>
</br></br></br>

## `[Range(min, max)]`

|項目|内容|
|-|-|
|対象|数値型、`char`、`DateTime`、`TimeSpan`、`string`、`enum` パラメーター (各 nullable バリアントを含む)|
|引数|`(int, int)`、`(double, double)`、`(Type, string, string)` の三つのオーバーロード|
|複数許可|X|
|検証タイミング|抽出 (`SchemaInfoScanner` の `RangeAttributeChecker`)、ロード (`Sdp.Csv.RangeValidator`)|
|適用不可な型への付与|スキャナが `RangeAttributeNotApplicable` 例外を発生 (例: `bool` / `bool?` / コレクション / record)|
|省略時|範囲検査なしで進行|

`System.ComponentModel.DataAnnotations.RangeAttribute` を継承した型です。値が範囲を外れると、`ExcelColumnExtractor` のセル互換性検査とランタイムロードの両方で `ArgumentOutOfRangeException` として失敗します。両ステージは同じ境界解釈ルール — `string` は辞書順 (`CompareOrdinal`)、`DateTime` / `TimeSpan` は `[DateTimeFormat]` / `[TimeSpanFormat]` のフォーマット、`enum` は underlying の整数 — を使用するため、検査結果が一致します。

数値型は二引数オーバーロードをそのまま使います。

```csharp
[StaticDataRecord("GameItems", "Items")]
public sealed record ItemRecord(
    int Id,
    string Name,
    [Range(0, 1_000_000)] int Price);
```

非数値型は `(Type, string, string)` オーバーロードで境界を明示します。境界文字列は該当する型のパースルールに従って解釈されます。

```csharp
// DateTime — [DateTimeFormat] の形式で境界を記述します
[DateTimeFormat("yyyy-MM-dd")]
[Range(typeof(DateTime), "2024-01-01", "2024-12-31")]
DateTime EventDate;

// TimeSpan — [TimeSpanFormat] の形式で境界を記述します
[TimeSpanFormat("c")]
[Range(typeof(TimeSpan), "00:00:00", "01:00:00")]
TimeSpan Duration;

// string — 辞書順比較 (CompareOrdinal、文化圏に依存しない)
[Range(typeof(string), "apple", "zebra")]
string Tag;

// enum — メンバー名で境界を指定。underlying の整数で比較されます
[Range(typeof(Tier), "Low", "High")]
Tier Grade;

// Key enum — underlying の整数文字列で境界を指定
[Key]
[Range(typeof(ItemId), "100", "1000")]
ItemId Id;
```

Nullable バリアント (`int?`、`DateTime?`、`string?`、`Tier?` など) もそのままサポートされます。cell value が `[NullString]` でマッチされると Range 検査は省略され、non-null の値には inner 型の Range 検査がそのまま適用されます。

---

<a id="attr-regularexpression"></a>
</br></br></br>

## `[RegularExpression(pattern)]`

|項目|内容|
|-|-|
|対象|`string` または `string?` パラメーター|
|引数|`pattern` — [.NET 正規表現パターン](https://learn.microsoft.com/dotnet/standard/base-types/regular-expression-language-quick-reference)|
|複数許可|X|
|検証タイミング|スキャナ (型確認)、抽出 / ロード (`Regex.IsMatch`)|
|省略時|正規表現検査なしで進行|
|誤用|`string` / `string?` 以外の型に付けるとスキャナが `RegularExpressionAttributeOnlyForString` 例外を発生|

`System.ComponentModel.DataAnnotations.RegularExpressionAttribute` を継承。パターンと一致しない値があると、`ExcelColumnExtractor` のセル互換性検査とランタイムロードの両方で失敗します。`string?` に付けた場合、セル値が `[NullString]` でマッチされると `null` と解釈され、パターン検査をスキップします。

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

|項目|内容|
|-|-|
|対象|`ImmutableArray<T>` または `FrozenSet<T>` (Dictionary には不可)|
|引数|`separator` (デフォルト値 `","`)|
|複数許可|X|
|検証タイミング|スキャナ / ロード|
|省略時|`[Length]` もなければスキャナが `LengthAttributeRequired` 例外を発生|
|排他関係|`[Length]` と同時使用不可|
|備考|要素が Record の場合、スキャナが `SingleColumnArrayOnlyPrimitive` (Array)、`SingleColumnHashSetOnlyPrimitive` (Set) 例外を発生。Map (FrozenDictionary) に付けると `SingleColumnCollectionNotForMap` を発生|

一つのセルに `"a,b,c"` の形式で複数の値をまとめて入れる方式です。分割された要素数に制約が必要な場合は [`[CountRange]`](#attr-countrange) を一緒に付けます (任意)。

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

|項目|内容|
|-|-|
|対象|Record クラス|
|引数|`excelFileName` (拡張子を除く)、`sheetName`、`startCell` (任意、デフォルト `null`)|
|検証タイミング|スキャナ / 抽出 / ロードの各ステージで要求される|
|省略時|`ExcelColumnExtractor` は抽出対象の Record が一つもないとき `StaticDataRecordAttributeNotFound` で終了。CSV ロード時に対象テーブルの Record になければ `StaticDataRecordAttributeRequired` 例外を発生。|

この Attribute がない Record は「静的データテーブル対象ではない補助 Record」とみなされ、抽出/ロード対象から除外されます。

```csharp
[StaticDataRecord("GameItems", "Items")]
public sealed record ItemRecord(int Id, string Name);
```

三つ目の引数 `startCell` は、この Record が対応するシートのヘッダー開始セルを Record 単位で指定します。指定すると `ExcelColumnExtractor` の `--start-cell` オプションより優先されます。一つのプロジェクト内でほとんどのシートが同じ開始セルを使うが、一部だけ異なる位置で始まるときに便利です。

```csharp
// 他のシートと異なり、このシートだけ B3 からヘッダーが始まる
[StaticDataRecord("GameItems", "Quests", "B3")]
public sealed record QuestRecord(int Id, string Title);
```

---

<a id="attr-switchforeignkey"></a>
</br></br></br>

## `[SwitchForeignKey(conditionColumnName, conditionValue, tableSetName, recordColumnName)]`

|項目|内容|
|-|-|
|対象|Record パラメーター|
|引数|`conditionColumnName`、`conditionValue`、`tableSetName`、`recordColumnName`|
|複数許可|O (`AllowMultiple = true`)|
|検証タイミング|スキャナ (FK/SFK 同時付与のブロック、重複条件のブロック) + ロード (FK/SFK 同時付与の再確認、重複条件の再確認、ターゲット検証、参照検証)|
|`[ForeignKey]` と同時付与|スキャナとロードの両方が `FkSwitchFkConflict` 診断で拒否|
|同じ `(conditionColumnName, conditionValue)` が二回以上付与|スキャナは `SwitchForeignKeyDuplicateCondition`、ロードは `SwitchFkDuplicateConditionValue` 診断で拒否 (メッセージキーが互いに異なる)|
|ターゲットが TableSet に存在しない|ロード時に `FkTargetNotFound` 例外を発生|
|ターゲットが `[SingleColumnCollection]` カラム|ロード時に `FkTargetIsSingleColumnCollection` 例外を発生|
|`conditionColumnName` が同じ Record 内に存在しない|テーブルロード後のターゲット解決ステージで `SwitchFkConditionColumnNotFound` 例外を発生|
|ターゲットのカラム名が存在しない|テーブルロード後のターゲット解決ステージで `FkTargetColumnNotFound` 例外を発生|
|条件カラムの値がどの分岐にもマッチしない|値検証ステージで `SwitchFkConditionValueNotMatched` 例外を発生|
|値検証の失敗|`AggregateException(FkValidationFailed, ...)` の内部に `FkValueNotFound` (条件値を含む) 例外を発生|

同じパラメーター値が **別のカラムの値に応じて異なるテーブルを参照** しなければならないときに使います。詳細なフローと例は [3.6 外部キー](../03-usage/06-foreign-keys.md) を参照してください。

上記のロードステージ診断も単独でスローされません — `FkTargetNotFound`、`FkTargetIsSingleColumnCollection`、`SwitchFkConditionColumnNotFound`、`FkTargetColumnNotFound`、`SwitchFkConditionValueNotMatched`、`FkValueNotFound` はすべて `AggregateException(FkValidationFailed, ...)` の `InnerExceptions` に集約され一度に通知されます。

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

|項目|内容|
|-|-|
|対象|`TimeSpan` または `TimeSpan?` 型のパラメーター (コレクション要素を含む)|
|引数|`format` — .NET 標準の TimeSpan フォーマット文字列 ([標準](https://learn.microsoft.com/dotnet/standard/base-types/standard-timespan-format-strings)、[カスタム](https://learn.microsoft.com/dotnet/standard/base-types/custom-timespan-format-strings))|
|複数許可|X|
|検証タイミング|スキャナ (存在有無)、抽出 (`TimeSpan.TryParseExact`)、ロード (`TimeSpan.ParseExact`)|
|省略時|スキャナが `TimeSpanFormatAttributeRequired` 例外を発生|
|誤用|非 `TimeSpan` 型に付けるとスキャナが `TimeSpanFormatAttributeNotApplicable` 例外を発生|

`TimeSpan` も同様に、この Attribute なしでは使用できません。抽出時のセル値検証とランタイムマッピングの両方が同じ `format` で `ParseExact` を呼び出すため、format から外れた表記は両方で失敗します。

```csharp
[StaticDataRecord("GameItems", "Items")]
public sealed record ItemRecord(
    int Id,
    string Name,
    [TimeSpanFormat(@"hh\:mm\:ss")] TimeSpan Cooldown);
```

---

[← 前: 5.1 サポートされる型 (Schemata)](./01-schemata.md) | [目次](../README.md) | [次: 5.3 型ブランディングパターン →](./03-type-branding.md)
