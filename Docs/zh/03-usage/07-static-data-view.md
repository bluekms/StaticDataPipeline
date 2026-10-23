# 3.7 StaticDataView 预构建视图

到 [3.5](./05-static-data-manager.md) 为止的 StaticDataManager 都是把表原样对外公开。在实务中，常常需要 **值得在表之上构建一次的结果**，例如「join 多个表的结果」「按特定键分组的合计」。与其在每次查询时重新构建这类结果，不如用 `StaticDataView` 这个 **在加载时点构建一次** 的合成层。

## 何时使用

- 频繁查询 join 两个或以上表的结果。
- 想把通过 FK 关联的子行预先按父键分组。
- 合计、过滤之类的加工每次都对相同的输入重复进行。

视图是 read-only 的。TableSet 和 ViewSet 在一次 `LoadAsync` 中一起构建，一旦构建出来的视图会保持原样，直到下一次 `LoadAsync` 替换为新快照。

</br></br></br>

## 定义 ViewSet

ViewSet 与 TableSet 形状相同 —— **一个把每个视图作为参数接收的 record**。

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

有三条规则。

1. **使用 2 泛型基类** — 只处理 TableSet 时的基类是 `StaticDataManager<TTableSet>`，但若要同时合成视图，则继承 `StaticDataManager<TTableSet, TViewSet>` 基类。把 `ILogger` 原样传递给 base 的部分相同。
2. **每个视图参数都是 non-nullable** — TableSet 之所以是 nullable，是因为其中一部分可能被 `disabledTables` 选项排除，但 ViewSet 的构建器总是填满所有槽位，因此声明为 nullable 会以 `ViewSetMemberMustBeNonNullable` 被拒绝。
3. **只对外公开 `Current` 一处。** `Current` 指向一个 `TableAndViewSet(Tables, Views)` 束，它作为单个束被原子地替换。如 [3.5](./05-static-data-manager.md) 所见，不要在 manager 子类中展开各视图的便利属性 —— 调用方用 `var tables = staticData.Current;` 接收一次，再从其 `Tables` / `Views` 中取出。

</br></br></br>

## 实现 StaticDataView

视图拥有一个 **只接收一个 TableSet 的构造函数**。在构造函数中预先构建所需的索引和聚合是标准模式。

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

核心是以下三点。

1. 像 `StaticDataView<GameStaticData.TableSet>` 这样，把输入 TableSet 声明为类型参数。
2. **只接收一个 TableSet 的单一构造函数** — `ViewSetBuilder` 通过反射调用这个构造函数。如果存在不同的签名，会以 `ViewConstructorNotFound` 失败。
3. **在构造函数中完成构建** — 不在外部保留可变状态，只把查询用的索引（`UniqueIndex`、`MultiIndex`）和预构建的集合作为 readonly 保留。

</br></br></br>

## 视图的自我校验

与表拥有 `Validate` 的方式相同，视图也可以 override `Validate` 来放置自定义校验逻辑。它在构建器创建视图实例之后立即被调用，如果抛出异常，该异常会与其他视图的构建结果一起汇集到 `ViewsFailedToBuild`。

```csharp
public sealed class EventBundleView(GameStaticData.TableSet tables)
    : StaticDataView<GameStaticData.TableSet>(tables)
{
    protected override void Validate()
    {
        // 在这里编写校验逻辑
    }
}
```

能在构造函数中完成的校验放在构造函数里更简单。当你想把构建和校验作为两件事分开阅读时，或者需要对整个构建结果进行事后检查时，使用 `Validate`。

</br></br></br>

## 与 disabledTables 的关系

上面的 `EventBundleView` 在构造函数中用 `!` 取出了 `t.EventTable`、`t.WeaponTable`、`t.ArmorTable` 全部三个。如果这三个中有任何一个被 [3.5](./05-static-data-manager.md#跳过特定的表) 的 `disabledTables` 选项禁用，构建过程中就会发生 `NullReferenceException`，并且 **整个那次尝试都会汇集到 `ViewsFailedToBuild`。**

因此，请采用以下两种方式之一来整理。

- **始终把视图依赖的表一起加载。** 设计 disable 分组时，把视图的依赖项也捆绑进去使其一起被排除。
- 如果你希望即使某个表被禁用时构建也能通过，**就在视图构造函数中显式写出 `null` 分支**。例如在 `WeaponTable` 可能被排除的环境中，可以把那个位置替换为空序列，使视图对外公开一个空结果。

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
            // 即使武器/防具表被禁用，weaponsByEvent.Get(ev.Id) 也会返回空序列
            ...
        }
    }

    return new UniqueIndex<EventBundle, int>(bundles, b => b.Event.Id);
}
```

这样的分支只有在「这个视图必须在任何环境下都以空结果存活」的意图明确时才有效。默认推荐是第一种 —— 把视图依赖的表一起加载。视图的存在本身建立在「这些表全部齐备」的假设之上，这是很自然的。

</br></br></br>

## 加载流程与构建阶段

`LoadAsync` 的流程与 [3.5](./05-static-data-manager.md) 的单泛型 StaticDataManager 几乎相同，**在所有校验通过之后追加了一个 ViewSet 构建阶段**。

1. **架构阶段检查** — 确认 TableSet 的单一构造函数并校验 FK 目标。
2. **TableSet 并行加载** — 各表的 `Trace` 日志 `LoadedTable`。
3. **FK 值校验** — 校验实际的 FK 值。
4. **StaticDataManager 校验** — 调用 `Validate(TTableSet)`（如果被 override）。
5. **ViewSet 构建** — `ViewSetBuilder.Build` 逐个遍历 ViewSet 的构造函数参数，依次构建每个视图。实例化之后立即也调用该视图的 `Validate`。各视图的构建时间以 `Trace` 级别记录（消息键 `BuiltView`）。
6. **快照替换** — 把新构建的 `(TableSet, ViewSet)` 一次性替换进 `volatile` 字段，并以 `Information` 级别记录整体完成时间。

如果在视图构建过程中有任何一个抛出异常，会把所有视图的尝试汇集到底，然后抛出 `AggregateException(Messages.ViewsFailedToBuild, ...)`。这种情况下 `Current` 不会更新，因此保持之前的快照（如果是首次加载则保持仍为空的状态）。

ViewSet 本身存在无效参数时的诊断也已定义。

- 如果参数类型不是 `StaticDataView<,>` 的子类型，则为 `InvalidViewParameter`。
- 如果参数是 nullable，则为 `ViewSetMemberMustBeNonNullable`。
- 如果没有恰好接收一个 TableSet 的构造函数，则为 `ViewConstructorNotFound`。
- 如果 ViewSet record 有两个以上的构造函数，则为 `ViewSetMustHaveSingleConstructor`。

这些全部在构建阶段作为 `AggregateException(Messages.ViewsFailedToBuild, ...)` 的 inner 进入。

</br></br></br>

## 并发性

并发性保证与 [3.5](./05-static-data-manager.md#loadasync-的并发性) 的 TableSet 相同。`LoadAsync` 的并发进入守卫和 `Current` 的原子替换原样适用，再加上 TableSet 和 ViewSet 作为单个束一起被替换这一点。

`Current` 所指向的 `TableAndViewSet(Tables, Views)` 作为单个束被原子地替换。即使有线程正在查询时另一个线程再次调用 `LoadAsync`，查询端也总是看到 **TableSet 和 ViewSet 相互一致的单个束**。绝不会公开只有一侧被改成新值的中间状态。这个保证的前提是 ViewSet 是一个拥有单一构造函数的 record，而 `ViewSetMustHaveSingleConstructor` 强制了这个前提。

调用方的模式与 [3.5](./05-static-data-manager.md#以一致的快照查询) 相同。在同一个操作中一起处理多个表或视图时，**把 `Current` 接收进变量只接收一次**，再从其中取出使用。

```csharp
var snapshot = staticData.Current;
var bundle = snapshot.Views.EventBundleView.Get(1);
var events = snapshot.Tables.EventTable!.Records;
```

</br></br></br>

## 在 ASP.NET Core 中以 DI 使用

与 [3.5 的注册模式](./05-static-data-manager.md#在-aspnet-core-中以-di-使用) 相同的方式，把 Snapshot 捆绑为 Scoped，并从其中展开每个表和视图。Manager 是 Singleton。

```csharp
// Program.cs
services.AddSingleton<GameStaticData>();

// 在一个请求内共享同一个 (Tables, Views) 束
services.AddScoped(sp =>
    sp.GetRequiredService<GameStaticData>().Current);

// 每个表
services.AddScoped<EventTable>(sp =>
    sp.GetRequiredService<GameStaticData.TableAndViewSet>().Tables.EventTable!);

services.AddScoped<WeaponTable>(sp =>
    sp.GetRequiredService<GameStaticData.TableAndViewSet>().Tables.WeaponTable!);

services.AddScoped<ArmorTable>(sp =>
    sp.GetRequiredService<GameStaticData.TableAndViewSet>().Tables.ArmorTable!);

// 每个 View（ViewSet 的成员是 non-nullable，所以没有 !）
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
        // bundles 和 totals 是从绑定到本请求的同一个 (TableSet, ViewSet) 束中取出的值
        ...
    }
}
```

在应用端，通常不接收整个 `GameStaticData` 或整个 `TableSet`，而是只挑选并注入实际使用的表和 View，这样调用方更整洁。

</br></br></br>

## 只使用 TableSet 的情况

如果不需要视图合成，可以原样使用 [3.5](./05-static-data-manager.md) 的 `StaticDataManager<TTableSet>`。两个基类各自独立存在，你可以在需要视图的时点转向 2 泛型基类，从而渐进式地扩展。

</br></br></br>

## 小结

- `StaticDataManager<TTableSet, TViewSet>` 负责视图合成。
- 视图继承 `StaticDataView<TTableSet>`，并在接收一个 TableSet 的构造函数中完成构建。
- 如有需要，override `Validate` 来放置视图自身的事后检查。
- 把 ViewSet 作为拥有 non-nullable 视图的 record，调用方通过 `Current.Views` 访问。
- TableSet 和 ViewSet 作为单个束被原子地替换，因此查询总是看到一致的束。

---

[← 上一篇: 3.6 外键](./06-foreign-keys.md) | [目录](../README.md) | [下一篇: 4.1 StaticDataHeaderGenerator →](../04-cli-tools/01-header-generator.md)
