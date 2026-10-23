# 3.7 StaticDataView 事前生成ビュー

[3.5](./05-static-data-manager.md) までの StaticDataManager はテーブルをそのまま公開します。実務では「複数のテーブルを join した結果」「特定のキーでグルーピングした合計」のように、**テーブルの上に一度作っておくとよい結果** がよく必要になります。こうした結果を参照のたびに作り直す代わりに、**ロード時点で一度生成しておく** 合成レイヤーが `StaticDataView` です。

## いつ使うか

- 2つ以上のテーブルを join した結果をよく参照する。
- FK で結ばれた子行を親キーであらかじめグルーピングしておきたい。
- 合計やフィルタリングのような加工が、毎回同じ入力に対して繰り返される。

ビューは read-only です。`LoadAsync` 一度で TableSet と ViewSet が一緒に作られ、一度生成されたビューは次の `LoadAsync` が新しいスナップショットへ差し替えるまでそのまま維持されます。

</br></br></br>

## ViewSet の定義

ViewSet は TableSet と同じ形 — **各ビューをパラメーターとして受け取る record** — です。

```csharp
using Microsoft.Extensions.Logging;
using Sdp.Manager;

public sealed class GameStaticData(ILogger<GameStaticData> logger)
    : StaticDataManager<GameStaticData.TableSet, GameStaticData.ViewSet>(logger)
{
    public sealed record TableSet(
        EventTable? EventTable,
        WeaponTable? WeaponTable,
        ArmorTable? ArmorTable);

    public sealed record ViewSet(
        EventBundleView EventBundleView,
        EventAttackTotalView EventAttackTotalView);
}
```

3つのルールです。

1. **2-ジェネリックのベースを使う** — TableSet だけを扱う場合のベースは `StaticDataManager<TTableSet>` だが、ビューまで合成するには `StaticDataManager<TTableSet, TViewSet>` ベースを継承する。`ILogger` をそのまま base へ渡す部分は同じ。
2. **各ビューのパラメーターは non-nullable** — TableSet は `disabledTables` オプションで一部が抜けることがあるため nullable だが、ViewSet はビルダーがすべてのスロットを常に埋めるので、nullable で宣言すると `ViewSetMemberMustBeNonNullable` で拒否される。
3. **`Current` 1か所だけを外部に公開する。** `Current` は `TableAndViewSet(Tables, Views)` の束を指し、1つの束として atomic に差し替えられる。[3.5](./05-static-data-manager.md) で見たように、マネージャーのサブクラスでビューごとの便利プロパティを展開しておかない — 呼び出し側で `var tables = staticData.Current;` として一度受け取り、その中の `Tables` / `Views` から取り出す。

</br></br></br>

## StaticDataView の実装

ビューは **TableSet を1つだけ受け取る生成子** を持ちます。生成子の中で必要なインデックスや集計をあらかじめ作っておくのが標準パターンです。

```csharp
using Sdp.Table;
using Sdp.View;

public sealed record EventBundle(
    EventRecord Event,
    IReadOnlyList<Equipment> Equipment);

public sealed class EventBundleView(GameStaticData.TableSet tables)
    : StaticDataView<GameStaticData.TableSet>(tables)
{
    private readonly UniqueIndex<EventBundle, int> byEventId = Build(tables);

    public EventBundle Get(int eventId)
        => byEventId.Get(eventId);

    private static UniqueIndex<EventBundle, int> Build(GameStaticData.TableSet t)
    {
        var weaponsByEvent = new MultiIndex<WeaponRecord, int>(t.WeaponTable!.Records, x => x.EventId);
        var armorsByEvent = new MultiIndex<ArmorRecord, int>(t.ArmorTable!.Records, x => x.EventId);

        var bundles = new List<EventBundle>();
        foreach (var ev in t.EventTable!.Records)
        {
            var equipment = new List<Equipment>();
            foreach (var weapon in weaponsByEvent.Get(ev.Id))
            {
                equipment.Add(new Equipment(weapon.Id, EquipmentType.Weapon, weapon.Name, weapon.AttackPower, 0));
            }

            foreach (var armor in armorsByEvent.Get(ev.Id))
            {
                equipment.Add(new Equipment(armor.Id, EquipmentType.Armor, armor.Name, 0, armor.DefensePower));
            }

            bundles.Add(new EventBundle(ev, equipment));
        }

        return new UniqueIndex<EventBundle, int>(bundles, b => b.Event.Id);
    }
}
```

核心は次の3つです。

1. `StaticDataView<GameStaticData.TableSet>` のように、入力 TableSet を型引数に宣言する。
2. **TableSet だけを受け取る単一の生成子** — `ViewSetBuilder` がリフレクションでこの生成子を呼び出す。別のシグネチャがあると `ViewConstructorNotFound` で失敗する。
3. **生成子の中でビルドを終わらせる** — 外部に可変状態を置かず、参照用のインデックス（`UniqueIndex`、`MultiIndex`）と事前生成されたコレクションだけを readonly で保持する。

</br></br></br>

## ビューの自己検証

テーブルに `Validate` があるのと同じやり方で、ビューにも `Validate` を override してユーザー検証ロジックを置けます。ビルダーがビューインスタンスを作った直後に呼び出され、例外を投げると他のビューのビルド結果と一緒に `ViewsFailedToBuild` へまとめられます。

```csharp
public sealed class EventBundleView(GameStaticData.TableSet tables)
    : StaticDataView<GameStaticData.TableSet>(tables)
{
    protected override void Validate()
    {
        // ここに検証ロジックを記述
    }
}
```

生成子で終わらせられる検証は生成子に置くほうが単純です。`Validate` は、ビルドと検証を分けて読みたいとき、あるいはビルド結果全体にわたる事後点検が必要なときに使います。

</br></br></br>

## disabledTables との関係

上の `EventBundleView` は生成子の中で `t.EventTable`、`t.WeaponTable`、`t.ArmorTable` をすべて `!` で取り出します。この3つのうち1つでも [3.5](./05-static-data-manager.md#特定のテーブルをスキップする) の `disabledTables` オプションで無効化されていると、ビルド途中で `NullReferenceException` が発生し、**その試み全体が `ViewsFailedToBuild` の中にまとめられます。**

したがって、次の2つのうち1つで整理します。

- **ビューが依存するテーブルは常に一緒にロードする。** disable グループを組むとき、ビューの依存関係も一緒に抜けるように束ねる。
- あるテーブルが disable されたときもビルドを通過させたいなら、**ビューの生成子の中で `null` 分岐を明示** する。例えば `WeaponTable` が抜けることがある環境なら、その位置を空シーケンスに置き換えてビューが空の結果を公開するようにしておけます。

```csharp
private static UniqueIndex<EventBundle, int> Build(GameStaticData.TableSet t)
{
    var weaponRecords = t.WeaponTable?.Records ?? ImmutableArray<WeaponRecord>.Empty;
    var armorRecords = t.ArmorTable?.Records ?? ImmutableArray<ArmorRecord>.Empty;

    var weaponsByEvent = new MultiIndex<WeaponRecord, int>(weaponRecords, x => x.EventId);
    var armorsByEvent = new MultiIndex<ArmorRecord, int>(armorRecords, x => x.EventId);

    var bundles = new List<EventBundle>();
    if (t.EventTable is not null)
    {
        foreach (var ev in t.EventTable.Records)
        {
            // 武器/防具テーブルが disable されていても weaponsByEvent.Get(ev.Id) は空シーケンスを返す
            ...
        }
    }

    return new UniqueIndex<EventBundle, int>(bundles, b => b.Event.Id);
}
```

このような分岐は「このビューはどんな環境でも空の結果として生き残らなければならない」という意図が明確なときにのみ有効です。基本の推奨は1つ目 — ビューが依存するテーブルは一緒にロードする — です。ビューの存在そのものが「これらのテーブルがすべて揃っている」という前提に立つのが自然です。

</br></br></br>

## ロードの流れとビルド段階

`LoadAsync` の流れは [3.5](./05-static-data-manager.md) の単一ジェネリック StaticDataManager とほぼ同じで、**検証をすべて通過した後に ViewSet ビルド段階が追加** されます。

1. **スキーマ段階のチェック** — TableSet の単一生成子の確認と FK ターゲットの検証。
2. **TableSet の並列ロード** — テーブルごとの `Trace` ログ `LoadedTable`。
3. **FK 値検証** — 実際の FK 値の検証。
4. **StaticDataManager 検証** — （override していれば）`Validate(TTableSet)` の呼び出し。
5. **ViewSet ビルド** — `ViewSetBuilder.Build` が ViewSet の生成子パラメーターを1つずつたどり、各ビューを順に構築。インスタンス化直後にそのビューの `Validate` も一緒に呼び出される。ビューごとのビルド時間は `Trace` レベルで記録（メッセージキー `BuiltView`）。
6. **スナップショット差し替え** — 新しく構築された `(TableSet, ViewSet)` を `volatile` フィールドに一度で差し替え、全体の完了時間を `Information` レベルで記録。

ビューのビルド途中でどれか1つでも例外を投げると、すべてのビューの試みを最後までまとめたうえで `AggregateException(Messages.ViewsFailedToBuild, ...)` を投げます。この場合 `Current` は更新されないため、直前のスナップショット（または初回ロードならまだ空の状態）が維持されます。

ViewSet 自体に不正なパラメーターがあるときの診断も定められています。

- パラメーターの型が `StaticDataView<,>` のサブタイプでなければ `InvalidViewParameter`。
- パラメーターが nullable なら `ViewSetMemberMustBeNonNullable`。
- TableSet を1つだけ受け取る生成子がなければ `ViewConstructorNotFound`。
- ViewSet record に生成子が2つ以上あれば `ViewSetMustHaveSingleConstructor`。

すべてビルド段階で `AggregateException(Messages.ViewsFailedToBuild, ...)` の inner として入ります。

</br></br></br>

## 並行性

並行性の保証は [3.5](./05-static-data-manager.md#loadasync-の並行性) の TableSet と同じです。`LoadAsync` の並行進入ガードと `Current` の atomic な差し替えがそのまま適用され、そこに TableSet と ViewSet が1つの束として一緒に差し替えられるという点が加わります。

`Current` が指す `TableAndViewSet(Tables, Views)` は、1つの束として atomic に差し替えられます。参照中のスレッドがあるときに別のスレッドが `LoadAsync` を再度呼び出しても、参照側は常に **TableSet と ViewSet が整合した1つの束** を見ます。片方だけが新しい値に変わっている中間状態は公開されません。この保証の前提は ViewSet が単一の生成子を持つ record であるという点であり、`ViewSetMustHaveSingleConstructor` がその前提を強制します。

呼び出し側のパターンは [3.5](./05-static-data-manager.md#一貫したスナップショットで参照する) と同じです。同じ作業の中で複数のテーブルやビューを一緒に扱うときは、**`Current` を変数に一度だけ受け取って** その中から取り出して使います。

```csharp
var snapshot = staticData.Current;
var bundle = snapshot.Views.EventBundleView.Get(1);
var events = snapshot.Tables.EventTable!.Records;
```

</br></br></br>

## ASP.NET Core で DI として使う

[3.5 の登録パターン](./05-static-data-manager.md#aspnet-core-で-di-として使う) と同じやり方で、Snapshot を Scoped として束ね、その中から各テーブルとビューを展開します。Manager は Singleton。

```csharp
// Program.cs
services.AddSingleton<GameStaticData>();

// 1リクエストの中で同じ (Tables, Views) の束を共有する
services.AddScoped(sp =>
    sp.GetRequiredService<GameStaticData>().Current);

// 各テーブル
services.AddScoped<EventTable>(sp =>
    sp.GetRequiredService<GameStaticData.TableAndViewSet>().Tables.EventTable!);

services.AddScoped<WeaponTable>(sp =>
    sp.GetRequiredService<GameStaticData.TableAndViewSet>().Tables.WeaponTable!);

services.AddScoped<ArmorTable>(sp =>
    sp.GetRequiredService<GameStaticData.TableAndViewSet>().Tables.ArmorTable!);

// 各 View（ViewSet のメンバーは non-nullable なので ! がない）
services.AddScoped<EventBundleView>(sp =>
    sp.GetRequiredService<GameStaticData.TableAndViewSet>().Views.EventBundleView);

services.AddScoped<EventAttackTotalView>(sp =>
    sp.GetRequiredService<GameStaticData.TableAndViewSet>().Views.EventAttackTotalView);
```

```csharp
public sealed class EventController(
    EventBundleView bundles,
    EventAttackTotalView totals) : ControllerBase
{
    [HttpGet("{id:int}")]
    public IActionResult Get(int id)
    {
        // bundles と totals は、このリクエストに束ねられた同じ (TableSet, ViewSet) の束から取り出した値
        ...
    }
}
```

アプリ側では通常、`GameStaticData` 全体や `TableSet` 全体を受け取らず、実際に使うテーブルや View だけを選んで注入で受け取るほうが、呼び出し側がすっきりします。

</br></br></br>

## TableSet だけを使う場合

ビューの合成が不要なら、[3.5](./05-static-data-manager.md) の `StaticDataManager<TTableSet>` をそのまま使えばよいです。2つのベースは別々に存在し、ビューが必要になった時点で 2-ジェネリックのベースへ移す形で段階的に拡張できます。

</br></br></br>

## まとめ

- `StaticDataManager<TTableSet, TViewSet>` がビューの合成を担当する。
- ビューは `StaticDataView<TTableSet>` を継承し、TableSet を1つ受け取る生成子でビルドを終わらせる。
- 必要なら `Validate` を override してビュー自身の事後点検を置く。
- ViewSet は non-nullable なビューを持つ record として置き、呼び出し側は `Current.Views` でアクセスする。
- TableSet と ViewSet は1つの束として atomic に差し替えられるため、参照は常に整合した束を見る。

---

[← 前: 3.6 外部キー](./06-foreign-keys.md) | [目次](../README.md) | [次: 4.1 StaticDataHeaderGenerator →](../04-cli-tools/01-header-generator.md)
