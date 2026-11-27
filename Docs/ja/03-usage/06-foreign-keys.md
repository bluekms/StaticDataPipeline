# 3.6 外部キー (ForeignKey, SwitchForeignKey)

[3.5](./05-static-data-manager.md) ではカテゴリテーブルとアイテムテーブルの 2 つを StaticDataManager でロードしましたが、2 つのテーブル間の参照は検証していませんでした。この章では、その参照を自動的に検証させる方法を扱います。

Sdp は 2 種類の外部キー Attribute を提供します。

- `[ForeignKey]` — 1 つのカラムが常に同じ対象テーブルの 1 つのカラムを指す場合。
- `[SwitchForeignKey]` — 同じカラムが、別のカラムの値に応じて指す対象が変わる場合。

どちらの Attribute も同じ検証フロー (`LoadAsync` の中) で処理されます。

## ForeignKey — 1 つのカラムが 1 つの対象を指す

[3.5](./05-static-data-manager.md) の例の `ItemRecord.CategoryId` が `CategoryTable` の `Id` を指すように宣言します。

```csharp
using Sdp.Attributes;

[StaticDataRecord("GameItems", "Categories")]
public sealed record CategoryRecord(
    int Id,
    string Name,
    bool IsConsumable);

[StaticDataRecord("GameItems", "Items")]
public sealed record ItemRecord(
    int Id,
    string Name,
    [ForeignKey("CategoryTable", "Id")] int CategoryId,
    int Price);
```

2 つの引数の意味は次のとおりです。

- 第 1 引数 `"CategoryTable"` — **TableSet のプロパティ名** (= TableSet record のパラメーター名)。
- 第 2 引数 `"Id"` — **その対象テーブル Record のパラメーター名**。

検証時に `CategoryId` が対象テーブルの中に存在するかどうかを確認します。

</br></br></br>

## 検証ステージ

`LoadAsync` の ForeignKey 検証は 3 つのステージに分かれて進行します。

1. **スキーマステージ — Record/TableSet 型の解析だけで確認 (テーブルのロード前)**
   - `tableSetName` が実際の TableSet パラメーターとして存在するか? (`FkTargetNotFound`)
   - 対象が `[SingleColumnCollection]` でまとめられたカラムではないか? (`FkTargetIsSingleColumnCollection`)
   - 同じパラメーターに `[ForeignKey]` と `[SwitchForeignKey]` が同時に付いていないか? (`FkSwitchFkConflict`)
   - `[SwitchForeignKey]` の同じ条件が 2 回以上登場していないか? (`SwitchFkDuplicateConditionValue`)
2. **ターゲット解決ステージ — テーブルのロード後、値検証の直前**
   - `recordColumnName` が対象 Record のパラメーターとして存在するか? (`FkTargetColumnNotFound`)
   - `[SwitchForeignKey]` の条件カラムが同じ Record の中に存在するか? (`SwitchFkConditionColumnNotFound`)
   - 対象テーブルが `disabledTables` で除外されていないか? (`FkTargetNotFound`)
3. **値ステージ — 実際の参照の存在有無**
   - 実際の CSV 値が対象テーブルの該当カラムの値の集合の中にあるか? (`FkValueNotFound`)
   - `[SwitchForeignKey]` の条件カラム値が分岐条件のいずれか 1 つにマッチするか? (`SwitchFkConditionValueNotMatched`)

各ステージは順次進行し、あるステージで失敗が見つかると、そのステージで集めた失敗を `AggregateException(Messages.FkValidationFailed, ...)` に格納して直ちに throw し、次のステージへは進みません。たとえばターゲット解決ステージ (2) が失敗すると、値ステージ (3) の参照検証は実行されません。

</br></br></br>

## ForeignKey が崩れているとき

`Items` シートに存在しない `CategoryId=99` の行があると、次のように失敗します。

```
AggregateException: FK の検証に失敗しました。
  - ItemRecord.CategoryId(99) は [CategoryTable.Id] のいずれにも存在しません。
```

複数の行が崩れている場合は、`InnerExceptions` に 1 つずつ格納されます。

</br></br></br>

## 対象が複数あるとき

`[ForeignKey]` は 1 つのパラメーターに 1 つだけ付けられます。2 つ以上付けるとコンパイルエラー (`CS0579`) になります。

値が指すテーブルが行ごとに異なる場合は、どのテーブルかを示す区分カラムを置き、下記の `[SwitchForeignKey]` を使います。「列挙したすべてのテーブルに存在すること」のようなそれ以外の条件は Table の `Validate` に書きます。

</br></br></br>

## SwitchForeignKey — 条件に応じて指す対象が変わる

同じカラム値が **別のカラムの値に応じて** 異なるテーブルを参照しなければならないときは `[SwitchForeignKey]` を使います。たとえば報酬の種類が `Item` のときは `ItemTable.Id` を指し、`Currency` のときは `CurrencyTable.Id` を指すようにします。

```csharp
public enum RewardKind
{
    Item,
    Currency,
}

[StaticDataRecord("GameItems", "Rewards")]
public sealed record RewardRecord(
    int Id,
    RewardKind Kind,

    [SwitchForeignKey(nameof(Kind), "Item",     "ItemTable",     "Id")]
    [SwitchForeignKey(nameof(Kind), "Currency", "CurrencyTable", "Id")]
    int TargetId);
```

4 つの引数の意味は次のとおりです。

- `conditionColumnName` — 同じ Record の中のどのカラムが分岐条件か。
- `conditionValue` — そのカラムがどの値のときにこの SwitchForeignKey を適用するか。
- `tableSetName` — その条件のときに指す TableSet のプロパティ名。
- `recordColumnName` — その対象テーブル Record のパラメーター名。

上記の例の行ごとの検証結果は次のように決まります。

|Kind|TargetId|検査対象|
|-|-|-|
|Item|10|ItemTable.Id に 10 が存在しなければならない|
|Currency|5|CurrencyTable.Id に 5 が存在しなければならない|

条件にマッチする `SwitchForeignKey` が 1 つもなければ (`Kind` が `Item`、`Currency` 以外の値であれば)、その行の検証は失敗します。

同じ `(conditionColumnName, conditionValue)` の組み合わせを持つ `SwitchForeignKey` を 1 つの Record の中に 2 回以上付けると、抽出ステージまたは `LoadAsync` のスキーマステージで拒否されます。同じ条件に対する対象テーブルは正確に 1 つでなければならないため、分岐表を自分自身と衝突するように書いた場合はその場で検出されます。

</br></br></br>

## 2 つのテーブルにまたがる SwitchForeignKey の例

もう少し現実的なまとまりで見てみましょう。報酬シートは種類と対象 ID だけを持ち、種類に応じて異なるデータテーブルを指します。

```csharp
[StaticDataRecord("GameItems", "Items")]
public sealed record ItemRecord(
    int Id,
    string Name,
    int Price);

[StaticDataRecord("GameItems", "Currencies")]
public sealed record CurrencyRecord(
    int Id,
    string Name);

[StaticDataRecord("GameItems", "Rewards")]
public sealed record RewardRecord(
    int Id,
    RewardKind Kind,

    [SwitchForeignKey(nameof(Kind), "Item",     "ItemTable",     "Id")]
    [SwitchForeignKey(nameof(Kind), "Currency", "CurrencyTable", "Id")]
    int TargetId,

    int Amount);

public sealed class ItemTable(ImmutableArray<ItemRecord> records)
    : StaticDataTable<ItemRecord>(records);

public sealed class CurrencyTable(ImmutableArray<CurrencyRecord> records)
    : StaticDataTable<CurrencyRecord>(records);

public sealed class RewardTable(ImmutableArray<RewardRecord> records)
    : StaticDataTable<RewardRecord>(records);

public sealed class GameStaticData(ILogger<GameStaticData> logger)
    : StaticDataManager<GameStaticData.TableSet>(logger)
{
    public sealed record TableSet(
        ItemTable? ItemTable,
        CurrencyTable? CurrencyTable,
        RewardTable? RewardTable);
}
```

CSV が次のようだとします。

`GameItems.Items.csv`
```
Id,Name,Price
1,Potion,100
2,Sword,5000
```

`GameItems.Currencies.csv`
```
Id,Name
1,Gold
2,Gem
```

`GameItems.Rewards.csv`
```
Id,Kind,TargetId,Amount
1,Item,1,10
2,Currency,1,500
3,Item,2,1
4,Currency,9,100
```

`Rewards` の 4 番目の行 `Currency / TargetId=9` は `CurrencyTable.Id` に存在しないため、検証は失敗します。残りの 3 行は通過します。

</br></br></br>

## まとめ

- `[ForeignKey(tableSet, column)]` — 単一の対象。1 つのパラメーターに 1 つだけ付ける。
- `[SwitchForeignKey(conditionColumn, conditionValue, tableSet, column)]` — 分岐する対象。同じパラメーターに複数回付けて分岐表を作る。
- 検証は `LoadAsync` の中で 3 つのステージ — スキーマ (対象の存在有無)、ターゲット解決 (カラム/条件カラムの存在有無)、値 (実際の参照の存在) — で処理され、失敗は `AggregateException` として一度にまとめて通知される。
- `disabledTables` で対象テーブルをスキップすると、そのテーブルを参照する ForeignKey 検証は失敗する — 独立したグループだけを disable するのが安全である。

`[ForeignKey]` と `[SwitchForeignKey]` 自体の Attribute 仕様 (誤用診断、複数付与の許可など) は [5.2 Attribute カタログ](../05-advanced/02-attributes.md) にまとめられています。

---

[← 前: 3.5 StaticDataManager で複数テーブルを管理する](./05-static-data-manager.md) | [目次](../README.md) | [次: 3.7 StaticDataView 事前生成ビュー →](./07-static-data-view.md)
