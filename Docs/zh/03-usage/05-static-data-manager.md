# 3.5 使用 StaticDataManager 管理多个表

要一次性加载多个表并以一致的快照对外公开，就需要 `StaticDataManager`。本章把分类表和物品表两个表捆绑在一起，通过 StaticDataManager 来运行。表之间的 FK 校验在下一章 [3.6 外键](./06-foreign-keys.md) 中讨论。

## 示例领域

我们把物品商店稍作扩展。分类表单独存在，物品引用这些分类（由于引用校验在下一章讨论，这里只看加载两个表的最简单形式）。

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

也为每个表定义一个 Table 类。

```csharp
using System.Collections.Immutable;
using Sdp.Table;

public sealed class CategoryTable(ImmutableArray<CategoryRecord> records)
    : StaticDataTable<CategoryRecord>(records);

public sealed class ItemTable(ImmutableArray<ItemRecord> records)
    : StaticDataTable<ItemRecord>(records);
```

</br></br></br>

## 定义 Manager 与 TableSet

`StaticDataManager` 通过 `TableSet` 类型来声明「它处理哪些表」。TableSet 是一个 **把每个表作为参数接收的 record**。

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

有三条规则。

1. **TableSet record 的参数名直接成为 TableSet 的属性名。** 一旦引入 FK，这些名称必须与 `[ForeignKey]` 的第一个参数一致（[3.6](./06-foreign-keys.md)）。
2. **每个表声明为 nullable。** 因为不能假定每次加载都包含所有表。如果用后面介绍的 `disabledTables` 选项跳过某些表，那个位置就会变成 `null`。
3. **`StaticDataManager<TTableSet>` 要求 `ILogger` 作为构造函数参数。** 子类把它原样传递给 base。加载进度（整体完成、各表完成）通过这个 logger 记录。

此外，**TableSet 必须恰好有一个构造函数。** 由于加载器通过反射调用该构造函数，存在两个以上会在 `LoadAsync` 开始时以 `TableSetMustHaveSingleConstructor` 消息被拒绝。声明为 record 自然只会有一个。ViewSet 遵循同样的规则（[3.7](./07-static-data-view.md)）。

StaticDataManager **只对外公开 `Current` 一处**。在 StaticDataManager 内部像 `CategoryTable => Current.CategoryTable!` 这样展开表，会让调用方更简短，但会引入一个陷阱：**在同一个操作中调用两次可能会观察到两个不同的快照**。下一节将详细说明。

</br></br></br>

## 加载与查询

```csharp
var staticData = new GameStaticData(logger);
await staticData.LoadAsync("./csv");

var tables = staticData.Current;
foreach (var item in tables.ItemTable!.Records)
{
    Console.WriteLine($"{item.Id} {item.Name}");
}
```

重要的一点是把 `Current` 接收一次并存入变量。调用方随后只用那个变量里的表来工作。为什么必须只接收一次，将在下面的 [LoadAsync 的并发性](#loadasync-的并发性) 和 [以一致的快照查询](#以一致的快照查询) 中详细讨论。

`LoadAsync` 按以下顺序运作。

1. **架构阶段检查** — 确认 TableSet 恰好有一个构造函数（两个以上会以 `TableSetMustHaveSingleConstructor` 被拒绝），并确认 `[ForeignKey]` / `[SwitchForeignKey]` 的目标存在于 TableSet 中、所指向的列是标量。如果没有 FK，则跳过 FK 目标检查。
2. **TableSet 并行加载** — 逐个遍历 TableSet 的构造函数参数，并行加载每个表的 CSV。每个表实例化之后立即调用 `StaticDataTable.Validate()`，各表的加载时间以 `Trace` 级别记录。只要有一个失败，就把所有失败汇总并作为 `AggregateException(Messages.TablesFailedToLoad, ...)` 抛出。
3. **FK 值校验** — 全部成功后组装 TableSet，如果有 FK 则校验其实际值。如果有任何校验失败，则作为 `AggregateException(Messages.FkValidationFailed, ...)` 抛出。
4. **StaticDataManager 校验** — 调用 StaticDataManager 的 `Validate(TTableSet)`（如果被 override）。如果它抛出异常，则原样抛出该异常。
5. **快照替换** — 全部通过后，一次性替换 `StaticDataManager.Current`，并以 `Information` 级别记录整体加载时间。

如果加载失败，`Current` 不会更新，保持之前的状态（如果是首次加载则仍为 `null`）。

</br></br></br>

## 跳过特定的表

当你想在 CI 中只快速校验部分表，或在开发服务器上临时排除尚未就绪的表时，使用 `disabledTables`。

```csharp
await staticData.LoadAsync("./csv", disabledTables: ["ItemTable"]);
```

- 基于参数名进行匹配。`"ItemTable"` 对应 TableSet 的参数 `ItemTable`。
- 该表不会被加载，TableSet 属性中会放入 `null`。
- 如果另一个表有指向该表的 FK，校验会失败 — 因为无法引用一个不存在的目标。因此实际上，只对相互独立的分组进行 disable 才是安全的。

每个表在 TableSet 中被声明为 nullable 的最大理由就是这个选项的存在。`null` 的可能性必须体现在类型上，这样才能写出不读取被 disable 的位置、直接跳过的代码。

实务中它常被用作 **临时绕过破坏性变更的开发期选项**。当某个表的架构损坏，但你需要先构建并运行不使用该表的代码时，把该表 disable 掉就能在不修改代码的情况下继续推进。你不需要在调用方写额外代码来绕开被 disable 的位置，但如有必要也可以用 nullable 守卫加一个分支。

</br></br></br>

## 测量加载时间

`StaticDataManager` 通过注入的 `ILogger` 记录两个阶段。

- 各表加载完成 — `Trace` 级别，消息键 `LoadedTable`（`表 {Name} 加载完成 ({ElapsedMs}ms)`）。
- 整体 `LoadAsync` 完成 — `Information` 级别，消息键 `LoadAsyncCompleted`（`LoadAsync 完成 ({ElapsedMs}ms)`）。

如果想单独查看各表的时间，在宿主端把最低日志级别降到 `Trace`。为了保持正常运维日志的整洁，保持在 `Information` 就只会留下整体完成时间。

</br></br></br>

## StaticDataManager 级别的附加校验

如果你 override `StaticDataManager.Validate(TTableSet)`，它会在所有 FK 校验完成之后被调用一次。用它来检查跨表约束（例如「武器分类中至少要有一个物品」）—— 即那些难以在单个表内表达的规则。在前面定义的 `GameStaticData` 中 override `Validate`。

```csharp
public sealed class GameStaticData(ILogger<GameStaticData> logger)
    : StaticDataManager<GameStaticData.TableSet>(logger)
{
    public sealed record TableSet(
        CategoryTable? CategoryTable,
        ItemTable? ItemTable);

    protected override void Validate(TableSet tableSet)
    {
        // 在这里编写跨表校验逻辑
    }
}
```

</br></br></br>

## LoadAsync 的并发性

`LoadAsync` 会防护并发进入。当已经在加载中时，如果另一个线程调用它，会立即以 `InvalidOperationException` 被拒绝。加载结束后的 `Current` 替换是通过对 `volatile` 字段的一次写入完成的，因此查询端绝不会观察到部分更新的中间状态。

</br></br></br>

## 以一致的快照查询

`Current` 每次调用时都返回那一时刻 `current` 字段的值。特别是在 **`LoadAsync` 可能在后台再次运行的环境** 中，在两次连续读取 `Current` 之间，快照可能被替换为新的。首次加载之后，或者应用端出于某种原因再次调用 `LoadAsync` 的流程中，快照可能会因调用时机不同而不同。

```csharp
// 危险 - 如果 LoadAsync 在这两行之间设置了新值
// categoryTable 和 itemTable 就会观察到不同的版本
var categoryTable = staticData.Current.CategoryTable!;
var itemTable = staticData.Current.ItemTable!;
```

在同一个操作中一起处理多个表时，**把 `Current` 接收进变量只接收一次再使用是安全的。**

```csharp
// 安全 - 从单个快照中一起取出两个表
var tables = staticData.Current;
var categoryTable = tables.CategoryTable!;
var itemTable = tables.ItemTable!;
```

不在 StaticDataManager 中展开 `CategoryTable` / `ItemTable` 这类便利属性的理由正在于此。如果展开了，调用方每次访问其中之一两次时都会重新读取 `Current`，从而把同样的风险一并带过来。**始终在一处把 `Current` 接收一次再使用的模式** 最为简单。

</br></br></br>

## 在 ASP.NET Core 中以 DI 使用

在有按请求生命周期的环境中，推荐把 **Manager 注册为 Singleton，TableSet 快照和各个表注册为 Scoped** 的模式。控制器和处理器只直接注入它们需要的表，而不是整个 `GameStaticData`。

```csharp
// Program.cs
services.AddSingleton<GameStaticData>();

// 捆绑使得同一个 TableSet 快照在一个请求内被共享
services.AddScoped<GameStaticData.TableSet>(sp =>
    sp.GetRequiredService<GameStaticData>().Current);

services.AddScoped<CategoryTable>(sp =>
    sp.GetRequiredService<GameStaticData.TableSet>().CategoryTable!);

services.AddScoped<ItemTable>(sp =>
    sp.GetRequiredService<GameStaticData.TableSet>().ItemTable!);
```

如果把 `TableSet` 本身捆绑为 Scoped，那么一个请求内出现的所有表都会成为从同一个快照展开出来的实例。即使 `LoadAsync` 在请求处理过程中替换为新快照，已经在处理中的请求也会从开始到结束都看到其起始时刻的快照。前一节看到的「LoadAsync 插入两行之间」的风险，在 DI 配置阶段就被阻断了。

```csharp
public sealed class ItemController(
    CategoryTable categoryTable,
    ItemTable itemTable) : ControllerBase
{
    [HttpGet("{id:int}")]
    public IActionResult Get(int id)
    {
        // categoryTable 和 itemTable 是从绑定到本请求的同一个快照展开出来的实例
        ...
    }
}
```

`LoadAsync` 本身由于 Manager 是 Singleton，可以从任何地方用同一个实例调用。通常在宿主启动时调用一次。

</br></br></br>

## 到本章为止的小结

- Record 与 Excel 表单相对应（3.1、3.3）。
- 每个 Record 创建一个 `StaticDataTable`（3.4）。
- 多个表用 `StaticDataManager` 捆绑加载，并从单一处 `Current` 以一致的快照取出（3.5）。

下一章讨论表之间的外键校验。

---

[← 上一篇: 3.4 实现 StaticDataTable](./04-static-data-table.md) | [目录](../README.md) | [下一篇: 3.6 外键 →](./06-foreign-keys.md)
