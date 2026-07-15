# 3.5 StaticDataManager で複数のテーブルを管理する

複数のテーブルを一度にロードし、一貫したスナップショットとして公開するには `StaticDataManager` が必要です。この章では、カテゴリーテーブルとアイテムテーブルの2つをまとめて StaticDataManager で動かしてみます。テーブル間の FK 検証は次の章 [3.6 外部キー](./06-foreign-keys.md) で扱います。

## 例として使うドメイン

アイテムショップを少し拡張します。カテゴリーシートが別にあり、アイテムはそのカテゴリーを参照します（参照検証は次の章で扱うため、ここでは単純な2つのテーブルのロードまでだけを見ます）。

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
    int CategoryId,
    int Price);
```

それぞれの Table クラスも用意します。

```csharp
using System.Collections.Immutable;
using Sdp.Table;

public sealed class CategoryTable(ImmutableArray<CategoryRecord> records)
    : StaticDataTable<CategoryRecord>(records);

public sealed class ItemTable(ImmutableArray<ItemRecord> records)
    : StaticDataTable<ItemRecord>(records);
```

</br></br></br>

## Manager と TableSet の定義

`StaticDataManager` は「どのテーブルを扱うのか」を `TableSet` 型として宣言で受け取ります。TableSet は **各テーブルをパラメーターとして受け取る record** です。

```csharp
using Microsoft.Extensions.Logging;
using Sdp.Manager;

public sealed class GameStaticData(ILogger<GameStaticData> logger)
    : StaticDataManager<GameStaticData.TableSet>(logger)
{
    public sealed record TableSet(
        CategoryTable? CategoryTable,
        ItemTable? ItemTable);
}
```

3つのルールがあります。

1. **TableSet record のパラメーター名がそのまま TableSet のプロパティ名になる。** FK が導入されると、この名前が `[ForeignKey]` の第1引数と一致しなければならない（[3.6](./06-foreign-keys.md)）。
2. **各テーブルは nullable で宣言する。** ロードのたびにすべてのテーブルが含まれていると仮定しないためである。後述する `disabledTables` オプションで一部のテーブルをスキップすると、その位置は `null` になる。
3. **`StaticDataManager<TTableSet>` は `ILogger` を生成子引数として要求する。** サブクラスでは base にそのまま渡す。ロードの進行状況（全体の完了、テーブルごとの完了）がこのロガーで記録される。

さらに **TableSet は生成子をちょうど1つだけ持たなければならない。** ローダーがリフレクションでその生成子を呼び出すため、2つ以上あると `LoadAsync` の開始時点で `TableSetMustHaveSingleConstructor` メッセージで拒否される。record として宣言すれば自然に1つになる。ViewSet も同じルールに従う（[3.7](./07-static-data-view.md)）。

StaticDataManager は **`Current` 1か所だけを外部に公開** します。StaticDataManager の中で `CategoryTable => Current.CategoryTable!` のようにテーブルを展開しておくと呼び出し側は短くなりますが、**同じ作業の中で2回呼び出すと互いに異なるスナップショットを見る可能性がある** という落とし穴が生じます。次の節で詳しく見ます。

</br></br></br>

## ロードと参照

```csharp
var staticData = new GameStaticData(logger);
await staticData.LoadAsync("./csv");

var tables = staticData.Current;
foreach (var item in tables.ItemTable!.Records)
{
    Console.WriteLine($"{item.Id} {item.Name}");
}
```

`Current` を一度受け取って変数に格納しておく点が重要です。呼び出し側はその変数の中のテーブルだけで作業します。なぜ一度だけ受け取らなければならないのかは、下記の [LoadAsync の並行性](#loadasync-の並行性) と [一貫したスナップショットで参照する](#一貫したスナップショットで参照する) で詳しく扱います。

`LoadAsync` は次の順序で動作します。

1. **スキーマ段階のチェック** — TableSet が生成子をちょうど1つ持つかを確認し（2つ以上なら `TableSetMustHaveSingleConstructor` で拒否）、`[ForeignKey]` / `[SwitchForeignKey]` のターゲットが TableSet に存在するか、指し示すカラムがスカラーかを確認。FK がなければ FK ターゲットの検査はスキップ。
2. **TableSet の並列ロード** — TableSet の生成子パラメーターを1つずつたどり、各テーブルの CSV を並列ロード。各テーブルのインスタンス化直後に `StaticDataTable.Validate()` が呼び出され、テーブルごとのロード時間は `Trace` レベルで記録。1つでも失敗するとすべての失敗をまとめて `AggregateException(Messages.TablesFailedToLoad, ...)` として throw。
3. **FK 値検証** — すべて成功すると TableSet を組み立て、FK があれば実際の値を検証。検証失敗があれば `AggregateException(Messages.FkValidationFailed, ...)` として throw。
4. **StaticDataManager 検証** — （override していれば）StaticDataManager の `Validate(TTableSet)` が呼び出される。例外を投げればそのまま throw。
5. **スナップショット差し替え** — すべて通過すると `StaticDataManager.Current` に一度で差し替え、全体のロード時間を `Information` レベルで記録。

ロードが失敗した場合、`Current` は更新されず直前の状態が維持されます（初回ロード中なら、まだ `null`）。

</br></br></br>

## 特定のテーブルをスキップする

CI で一部のテーブルだけを素早く検証したいとき、あるいは開発サーバーでまだ準備できていないテーブルを一時的に除外したいときに `disabledTables` を使います。

```csharp
await staticData.LoadAsync("./csv", disabledTables: ["ItemTable"]);
```

- パラメーター名を基準にマッチングします。`"ItemTable"` は TableSet のパラメーター `ItemTable` に対応します。
- 該当のテーブルはロードされず、TableSet のプロパティには `null` が入ります。
- 該当のテーブルを対象とした FK が他のテーブルにある場合、検証が失敗します — 対象がないのに参照できないためです。したがって実際には、互いに独立したグループ単位でのみ disable するのが安全です。

各テーブルが TableSet で nullable として宣言されている最大の理由が、このオプションの存在です。`null` の可能性が型に表れていることで、disable された位置を読まずに通り過ぎるコードを書けます。

実務では **ブレイキングチェンジを一時的に回避する開発用オプション** としてよく使われます。あるテーブルのスキーマが壊れているが、そのテーブルを使用しないコードを先にビルド・実行してみる必要があるとき、該当のテーブルを disable しておけばコードを修正せずに進められます。呼び出し側で disable された位置を回避する別途のコードを書く必要はありませんが、必要なら nullable ガードで分岐を置くこともできます。

</br></br></br>

## ロード時間の計測

`StaticDataManager` は注入された `ILogger` で2つの段階を記録します。

- テーブルごとのロード完了 — `Trace` レベル、メッセージキー `LoadedTable`（`テーブル {Name} ロード完了 ({ElapsedMs}ms)`）。
- 全体の `LoadAsync` 完了 — `Information` レベル、メッセージキー `LoadAsyncCompleted`（`LoadAsync 完了 ({ElapsedMs}ms)`）。

テーブルごとの時間を個別に見たい場合は、ホスト側で最小ログレベルを `Trace` まで下げます。通常運用のログをきれいに保つには、`Information` のままにしておけば全体の完了時間だけが残ります。

</br></br></br>

## StaticDataManager レベルの追加検証

`StaticDataManager.Validate(TTableSet)` を override すると、すべての FK 検証が終わった後に一度呼び出されます。テーブル間の交差制約（例：「武器カテゴリーのアイテムは最低1つはなければならない」）のように、個々のテーブルでは表現しにくいルールをここで点検します。先ほど定義した `GameStaticData` の中に `Validate` を override しておきます。

```csharp
public sealed class GameStaticData(ILogger<GameStaticData> logger)
    : StaticDataManager<GameStaticData.TableSet>(logger)
{
    public sealed record TableSet(
        CategoryTable? CategoryTable,
        ItemTable? ItemTable);

    protected override void Validate(TableSet tableSet)
    {
        // ここにテーブル間の交差検証ロジックを記述
    }
}
```

</br></br></br>

## LoadAsync の並行性

`LoadAsync` は並行進入をガードします。すでにロード中のときに別のスレッドから呼び出すと、即座に `InvalidOperationException` で拒否されます。ロードが終わった後の `Current` 差し替えは `volatile` フィールドへの1回の書き込みで行われるため、参照側が部分的に更新された中間状態を見ることはありません。

</br></br></br>

## 一貫したスナップショットで参照する

`Current` は呼び出すたびにその時点の `current` フィールドの値を返します。特に **`LoadAsync` がバックグラウンドで再実行され得る環境** であれば、`Current` を2回連続で読む間に新しいスナップショットへ差し替えられる可能性があります。初回ロード直後であったり、アプリ側が何らかの理由で `LoadAsync` を再度呼び出す流れであれば、呼び出すタイミングによってスナップショットが変わる可能性があります。

```csharp
// 危険 - 2行の間で LoadAsync が新しい値をセットすると
// categoryTable と itemTable が互いに異なるバージョンを見る
var categoryTable = staticData.Current.CategoryTable!;
var itemTable = staticData.Current.ItemTable!;
```

同じ作業で複数のテーブルを一緒に扱うときは、**`Current` を変数に一度だけ受け取って使うのが安全です。**

```csharp
// 安全 - 1つのスナップショットから2つのテーブルを一緒に取り出す
var tables = staticData.Current;
var categoryTable = tables.CategoryTable!;
var itemTable = tables.ItemTable!;
```

StaticDataManager で `CategoryTable` / `ItemTable` のような便利プロパティを展開しない理由がここにあります。展開しておくと、呼び出し側が2回アクセスするたびに毎回 `Current` を読み直すことになり、同じリスクがそのまま移ってきます。**常に `Current` を1か所で一度受け取って使うパターン** が最も単純です。

</br></br></br>

## ASP.NET Core で DI として使う

リクエスト単位の寿命がある環境では、**Manager は Singleton、TableSet スナップショットと各テーブルを Scoped** として登録するパターンを推奨します。コントローラーやハンドラーは `GameStaticData` 全体ではなく、必要なテーブルだけを直接注入で受け取ります。

```csharp
// Program.cs
services.AddSingleton<GameStaticData>();

// 1リクエストの中で同じ TableSet スナップショットを共有するように束ねる
services.AddScoped<GameStaticData.TableSet>(sp =>
    sp.GetRequiredService<GameStaticData>().Current);

services.AddScoped<CategoryTable>(sp =>
    sp.GetRequiredService<GameStaticData.TableSet>().CategoryTable!);

services.AddScoped<ItemTable>(sp =>
    sp.GetRequiredService<GameStaticData.TableSet>().ItemTable!);
```

`TableSet` 自体を Scoped として束ねておくと、1リクエストの中で登場するすべてのテーブルが同じスナップショットから展開されたインスタンスになります。リクエスト処理中に `LoadAsync` が新しいスナップショットへ差し替えても、すでに進行中のリクエストは開始時点のスナップショットを最後まで見ます。前の節で見た「2行の間に LoadAsync が割り込む」リスクが、DI 構成の段階で遮断されます。

```csharp
public sealed class ItemController(
    CategoryTable categoryTable,
    ItemTable itemTable) : ControllerBase
{
    [HttpGet("{id:int}")]
    public IActionResult Get(int id)
    {
        // categoryTable と itemTable は、このリクエストに束ねられた同じスナップショットから展開されたインスタンス
        ...
    }
}
```

`LoadAsync` 自体は Manager が Singleton なので、どこからでも同じインスタンスで呼べばよいです。一般的にはホスティング開始時に一度呼び出します。

</br></br></br>

## この章までのまとめ

- Record と Excel が対応する（3.1、3.3）。
- Record 1つにつき `StaticDataTable` を1つ作る（3.4）。
- 複数のテーブルは `StaticDataManager` でまとめてロードし、`Current` 1か所から一貫したスナップショットとして取り出す（3.5）。

次の章では、テーブル間の外部キー検証を扱います。

---

[← 前: 3.4 StaticDataTable の実装](./04-static-data-table.md) | [目次](../README.md) | [次: 3.6 外部キー →](./06-foreign-keys.md)
