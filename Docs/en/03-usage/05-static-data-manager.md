# 3.5 Managing Multiple Tables with StaticDataManager

To load multiple tables at once and expose them as a consistent snapshot, you need a `StaticDataManager`. In this chapter we bundle two tables — a category table and an item table — and run them through a StaticDataManager. FK validation between tables is covered in the next chapter, [3.6 Foreign Keys](./06-foreign-keys.md).

## Example domain

We extend the item shop a little. There is a separate category sheet, and items reference those categories (since reference validation is covered in the next chapter, here we only look at loading the two tables in their simplest form).

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

We also define a Table class for each.

```csharp
using System.Collections.Immutable;
using Sdp.Table;

public sealed class CategoryTable(ImmutableArray<CategoryRecord> records)
    : StaticDataTable<CategoryRecord>(records);

public sealed class ItemTable(ImmutableArray<ItemRecord> records)
    : StaticDataTable<ItemRecord>(records);
```

</br></br></br>

## Defining the Manager and TableSet

A `StaticDataManager` is told "which tables it handles" through a `TableSet` type. A TableSet is a **record that takes each table as a parameter**.

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

There are three rules.

1. **The parameter names of the TableSet record become the TableSet property names.** Once FK is introduced, these names must match the first argument of `[ForeignKey]` ([3.6](./06-foreign-keys.md)).
2. **Declare each table as nullable.** This is because you cannot assume every load will contain every table. If you skip some tables with the `disabledTables` option described later, that slot becomes `null`.
3. **`StaticDataManager<TTableSet>` requires an `ILogger` as a constructor argument.** The subclass passes it straight through to the base. Load progress (overall completion, per-table completion) is recorded through this logger.

In addition, **the TableSet must have exactly one constructor.** Because the loader invokes that constructor via reflection, having more than one causes a rejection at the start of `LoadAsync` with a `TableSetMustHaveSingleConstructor` message. Declaring it as a record naturally yields a single one. The ViewSet follows the same rule ([3.7](./07-static-data-view.md)).

A StaticDataManager **exposes only `Current` to the outside**. Unfolding tables inside the StaticDataManager — like `CategoryTable => Current.CategoryTable!` — shortens the call site, but introduces the pitfall that **calling it twice within the same operation may observe two different snapshots**. We look at this in detail in the next section.

</br></br></br>

## Loading and lookups

```csharp
var staticData = new GameStaticData(logger);
await staticData.LoadAsync("./csv");

var tables = staticData.Current;
foreach (var item in tables.ItemTable!.Records)
{
    Console.WriteLine($"{item.Id} {item.Name}");
}
```

The important point is that `Current` is received once and stored in a variable. The call site then works only with the tables inside that variable. Why it must be received only once is covered in detail in [Concurrency in LoadAsync](#concurrency-in-loadasync) and [Querying with a consistent snapshot](#querying-with-a-consistent-snapshot) below.

`LoadAsync` operates in the following order.

1. **Schema-stage check** — Verifies that the TableSet has exactly one constructor (more than one is rejected with `TableSetMustHaveSingleConstructor`), and that the targets of `[ForeignKey]` / `[SwitchForeignKey]` exist in the TableSet and that the columns they point at are scalar. If there are no FKs, the FK target check is skipped.
2. **Parallel TableSet load** — Walks the TableSet constructor parameters one by one and loads each table's CSV in parallel. Right after each table is instantiated, `StaticDataTable.Validate()` is called, and per-table load time is recorded at `Trace` level. If any one fails, all failures are collected and thrown as `AggregateException(Messages.TablesFailedToLoad, ...)`.
3. **FK value validation** — If all succeed, the TableSet is assembled, and if there are FKs, their actual values are validated. If there are any validation failures, they are thrown as `AggregateException(Messages.FkValidationFailed, ...)`.
4. **StaticDataManager validation** — The StaticDataManager's `Validate(TTableSet)` is called (if overridden). If it throws an exception, that exception is thrown as-is.
5. **Snapshot swap** — If everything passes, `StaticDataManager.Current` is swapped in one step, and the total load time is recorded at `Information` level.

If the load fails, `Current` is not updated and the previous state is kept (or is still `null` if this is the first load).

</br></br></br>

## Skipping specific tables

When you want to quickly validate only some tables in CI, or temporarily exclude a not-yet-ready table on a development server, use `disabledTables`.

```csharp
await staticData.LoadAsync("./csv", disabledTables: ["ItemTable"]);
```

- Matching is based on parameter name. `"ItemTable"` corresponds to the TableSet parameter `ItemTable`.
- That table is not loaded, and `null` is placed in the TableSet property.
- If another table has an FK targeting that table, validation fails — because you cannot reference a target that does not exist. Therefore, in practice it is safe to disable only mutually independent groups.

The biggest reason each table is declared nullable in the TableSet is the existence of this option. Nullability must be visible in the type so that you can write code that skips over a disabled slot without reading it.

In practice it is often used as a **development-time option for temporarily working around a breaking change**. When some table's schema is broken but you need to first build and run code that does not use that table, disabling that table lets you proceed without modifying the code. You do not need to write extra code at the call site to route around the disabled slot, but if necessary you can branch on it with a nullable guard.

</br></br></br>

## Measuring load time

`StaticDataManager` records two stages through the injected `ILogger`.

- Per-table load completion — `Trace` level, message key `LoadedTable` (`Loaded table {Name} in {ElapsedMs} ms`).
- Overall `LoadAsync` completion — `Information` level, message key `LoadAsyncCompleted` (`LoadAsync completed in {ElapsedMs} ms`).

If you want to see per-table times separately, lower the minimum log level to `Trace` on the host side. To keep your normal operational logs clean, leaving it at `Information` keeps only the overall completion time.

</br></br></br>

## Additional validation at the StaticDataManager level

If you override `StaticDataManager.Validate(TTableSet)`, it is called once after all FK validation is finished. Use it to check cross-table constraints (for example, "there must be at least one item in the weapon category") — rules that are hard to express within an individual table. Override `Validate` inside the `GameStaticData` defined earlier.

```csharp
public sealed class GameStaticData(ILogger<GameStaticData> logger)
    : StaticDataManager<GameStaticData.TableSet>(logger)
{
    public sealed record TableSet(
        CategoryTable? CategoryTable,
        ItemTable? ItemTable);

    protected override void Validate(TableSet tableSet)
    {
        // Write your cross-table validation logic here
    }
}
```

</br></br></br>

## Concurrency in LoadAsync

`LoadAsync` guards against concurrent entry. If another thread calls it while a load is already in progress, it is immediately rejected with an `InvalidOperationException`. The `Current` swap after loading finishes is done with a single write to a `volatile` field, so the query side never observes a partially-updated intermediate state.

</br></br></br>

## Querying with a consistent snapshot

`Current` returns the value of the `current` field at the moment of the call. In particular, **in an environment where `LoadAsync` may run again in the background**, the snapshot may be swapped for a new one between two consecutive reads of `Current`. Right after the first load, or in a flow where the application calls `LoadAsync` again for some reason, the snapshot can differ depending on when you call it.

```csharp
// Dangerous - if LoadAsync sets a new value between these two lines,
// categoryTable and itemTable observe different versions
var categoryTable = staticData.Current.CategoryTable!;
var itemTable = staticData.Current.ItemTable!;
```

When handling multiple tables together in the same operation, **it is safe to receive `Current` into a variable only once and use that.**

```csharp
// Safe - both tables are taken from a single snapshot
var tables = staticData.Current;
var categoryTable = tables.CategoryTable!;
var itemTable = tables.ItemTable!;
```

This is exactly why convenience properties such as `CategoryTable` / `ItemTable` are not unfolded in the StaticDataManager. If they were, the call site would re-read `Current` every time it accesses one twice, carrying the same risk along with it. The simplest pattern is to **always receive `Current` once, in one place, and use it.**

</br></br></br>

## Using it with DI in ASP.NET Core

In an environment with per-request lifetimes, the recommended pattern is to register the **Manager as Singleton, and the TableSet snapshot and each table as Scoped**. Controllers and handlers inject only the tables they need directly, rather than the whole `GameStaticData`.

```csharp
// Program.cs
services.AddSingleton<GameStaticData>();

// Bind it so that the same TableSet snapshot is shared within one request
services.AddScoped<GameStaticData.TableSet>(sp =>
    sp.GetRequiredService<GameStaticData>().Current);

services.AddScoped<CategoryTable>(sp =>
    sp.GetRequiredService<GameStaticData.TableSet>().CategoryTable!);

services.AddScoped<ItemTable>(sp =>
    sp.GetRequiredService<GameStaticData.TableSet>().ItemTable!);
```

If you bind `TableSet` itself as Scoped, every table that appears within one request becomes an instance unfolded from the same snapshot. Even if `LoadAsync` swaps in a new snapshot during request processing, an already in-flight request sees the snapshot from its start to its end. The "LoadAsync slipping in between two lines" risk seen in the previous section is blocked at the DI configuration stage.

```csharp
public sealed class ItemController(
    CategoryTable categoryTable,
    ItemTable itemTable) : ControllerBase
{
    [HttpGet("{id:int}")]
    public IActionResult Get(int id)
    {
        // categoryTable and itemTable are instances unfolded from the same snapshot bound to this request
        ...
    }
}
```

`LoadAsync` itself, since the Manager is a Singleton, can be called from the same instance anywhere. Typically it is called once at host startup.

</br></br></br>

## Recap through this chapter

- A Record corresponds to an Excel sheet (3.1, 3.3).
- For each Record, you create one `StaticDataTable` (3.4).
- Multiple tables are bundled and loaded with a `StaticDataManager`, and retrieved as a consistent snapshot from a single place — `Current` (3.5).

The next chapter covers foreign key validation between tables.

---

[← Previous: 3.4 Implementing StaticDataTable](./04-static-data-table.md) | [Table of Contents](../README.md) | [Next: 3.6 Foreign Keys →](./06-foreign-keys.md)
