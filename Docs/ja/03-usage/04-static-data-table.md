# 3.4 StaticDataTable の実装

Record は「1 行の形」であり、`StaticDataTable` は「その Record たちを格納するテーブル」です。この章では [3.3](./03-first-record.md) の `ItemRecord` に対応する `ItemTable` を作り、`UniqueIndex` でキー照会まで付けてみます。

## もっとも単純なテーブル定義

```csharp
using System.Collections.Immutable;
using Sdp.Table;

public sealed class ItemTable(ImmutableArray<ItemRecord> records)
    : StaticDataTable<ItemRecord>(records);
```

2 つだけ覚えればよいです。

1. 型引数はレコード型 `TRecord` の 1 つ: `StaticDataTable<ItemRecord>`。
2. 必ず `ImmutableArray<TRecord>` 1 つだけを受け取るコンストラクターを提供する。Sdp がリフレクションでこのコンストラクターを呼び出す。

この状態では `Records` プロパティで全体のリストだけが公開されます。

```csharp
foreach (var item in table.Records)
{
    // ...
}
```

`Records` は `ImmutableArray<ItemRecord>` であり、CSV の行順をそのまま維持します。

</br></br></br>

## UniqueIndex でキー照会を追加する

主キーで 1 行を直接取り出す照会が必要なら、`UniqueIndex` を作り Getter を公開します。

```csharp
using System.Collections.Immutable;
using Sdp.Table;

public sealed class ItemTable : StaticDataTable<ItemRecord>
{
    private readonly UniqueIndex<ItemRecord, int> byId;

    public ItemTable(ImmutableArray<ItemRecord> records)
        : base(records)
    {
        byId = new UniqueIndex<ItemRecord, int>(records, x => x.Id);
    }

    public ItemRecord Get(int id)
        => byId.Get(id);

    public bool TryGet(int id, out ItemRecord? record)
        => byId.TryGet(id, out record);
}
```

`UniqueIndex` は生成時点でキーの重複を検査します。同じ `Id` が 2 つ以上あれば `InvalidOperationException` を投げます。

```csharp
var potion = table.Get(1);

if (table.TryGet(2, out var sword))
{
    Console.WriteLine($"{sword.Name}: {sword.Price}");
}
```

`Get` はキーがなければ `KeyNotFoundException` を投げ、`TryGet` は `false` を返します。キーの存在が保証されない照会には `TryGet` を使います。

別のカラムでも同じ方式の照会が必要なら、`UniqueIndex<ItemRecord, string> byName` のようにインデックスを追加すればよいです。キーが一意でないグループ照会には `MultiIndex` を使います。

</br></br></br>

## テーブル内部の追加検証

ほとんどの検証は `[Range]`、`[ForeignKey]`、`[RegularExpression]` のような Attribute で宣言的に動作します。Attribute だけでは表現しにくい特殊なルールがあれば、`Validate` を override してユーザー検証ロジックを追加できます。このメソッドはテーブルが作られた直後に呼び出され、StaticDataManager ステージの FK 検証より先に実行されます。

```csharp
public sealed class ItemTable : StaticDataTable<ItemRecord>
{
    public ItemTable(ImmutableArray<ItemRecord> records)
        : base(records)
    {
    }

    protected override void Validate()
    {
        // 例: すべてのカテゴリに最低 1 つはアイテムがあるか確認する
        foreach (var category in Enum.GetValues<ItemCategory>())
        {
            if (!Records.Any(x => x.Category == category))
            {
                throw new InvalidOperationException($"カテゴリ {category} に該当するアイテムがありません。");
            }
        }
    }
}
```

上記の例のように **複数の行にまたがる条件** は、1 つのセル値だけを見る Attribute では表現できないため、`Validate` が適した場所です。逆に「価格が負ではないか」のような単一値の検査は `Validate` ではなく `[Range]` で宣言するほうがよいです — 抽出ステージでも一緒にふるい落とされるからです。

テーブル間の参照の有効性は、通常 `[ForeignKey]` / `[SwitchForeignKey]` Attribute で表現するほうがきれいです ([3.6](./06-foreign-keys.md))。`Validate` は、それでは表現しにくい自テーブル内部のルール用に残しておきます。

</br></br></br>

## CSV ロードはどうする?

`ItemTable` 自体には CSV ローダーが入っていません。単一のテーブルも、複数テーブルのまとまりも、すべて `StaticDataManager` を通してロードします ([3.5](./05-static-data-manager.md))。この章では「テーブルクラスの形」までだけを扱います。

</br></br></br>

## まとめ

- `StaticDataTable<TRecord>` を継承し、`ImmutableArray<TRecord>` コンストラクターを提供する。
- `Records` は CSV の行順を維持する。
- キー照会やグループ照会は `UniqueIndex` / `MultiIndex` をメンバーとして持ち、Getter で公開する。
- 実際の CSV ロードは `StaticDataManager` が担当する。

---

[← 前: 3.3 最初の Record を定義する](./03-first-record.md) | [目次](../README.md) | [次: 3.5 StaticDataManager で複数テーブルを管理する →](./05-static-data-manager.md)
