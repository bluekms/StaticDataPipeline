# 3.7 StaticDataView Pre-built Views

Up through [3.5](./05-static-data-manager.md), the StaticDataManager exposes tables as-is. In practice, **results that are worth building once on top of the tables** — such as "the result of joining several tables" or "a sum grouped by a specific key" — are often needed. Instead of rebuilding such results on every query, `StaticDataView` is a composition layer that **builds them once at load time**.

## When to use it

- You frequently query the result of joining two or more tables.
- You want to pre-group child rows linked by FK under their parent key.
- A transformation such as a sum or filter is repeated each time over the same input.

Views are read-only. The TableSet and the ViewSet are built together in a single `LoadAsync`, and once a view is built it is kept as-is until the next `LoadAsync` swaps in a new snapshot.

</br></br></br>

## Defining the ViewSet

A ViewSet has the same shape as a TableSet — **a record that takes each view as a parameter**.

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

There are three rules.

1. **Use the 2-generic base** — when handling only a TableSet, the base is `StaticDataManager<TTableSet>`, but to also compose views, inherit the `StaticDataManager<TTableSet, TViewSet>` base. The part where `ILogger` is passed straight through to the base is the same.
2. **Each view parameter is non-nullable** — a TableSet is nullable because some of it can be excluded with the `disabledTables` option, but the ViewSet's builder always fills every slot, so declaring it nullable is rejected with `ViewSetMemberMustBeNonNullable`.
3. **Expose only `Current` to the outside.** `Current` points at a `TableAndViewSet(Tables, Views)` bundle, which is swapped atomically as a single bundle. As seen in [3.5](./05-static-data-manager.md), do not unfold per-view convenience properties in the manager subclass — the call site receives it once with `var tables = staticData.Current;` and retrieves things from its `Tables` / `Views`.

</br></br></br>

## Implementing a StaticDataView

A view has **a constructor that takes only a TableSet**. The standard pattern is to pre-build the needed indexes and aggregations inside the constructor.

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

The three key points are as follows.

1. Declare the input TableSet as the type argument, as in `StaticDataView<GameStaticData.TableSet>`.
2. **A single constructor taking only a TableSet** — `ViewSetBuilder` invokes this constructor via reflection. If there is a different signature, it fails with `ViewConstructorNotFound`.
3. **Finish the build inside the constructor** — keep no mutable state on the outside, and store only lookup indexes (`UniqueIndex`, `MultiIndex`) and pre-built collections as readonly.

</br></br></br>

## A view's self-validation

In the same way a table has a `Validate`, a view can also override `Validate` to place custom validation logic. It is called right after the builder creates the view instance, and if it throws an exception, that exception is collected together with the other views' build results into `ViewsFailedToBuild`.

```csharp
public sealed class EventBundleView(GameStaticData.TableSet tables)
    : StaticDataView<GameStaticData.TableSet>(tables)
{
    protected override void Validate()
    {
        // Write your validation logic here
    }
}
```

Validation that can be finished in the constructor is simpler to keep in the constructor. Use `Validate` when you want to read the build and the validation as separate things, or when a post-check across the entire build result is needed.

</br></br></br>

## Relationship with disabledTables

The `EventBundleView` above retrieves `t.EventTable`, `t.WeaponTable`, and `t.ArmorTable` all with `!` inside its constructor. If any one of these three is disabled by the `disabledTables` option from [3.5](./05-static-data-manager.md#skipping-specific-tables), a `NullReferenceException` occurs during the build, and **that entire attempt is collected into `ViewsFailedToBuild`.**

Therefore, settle on one of the following two approaches.

- **Always load the tables a view depends on together.** When designing a disable group, bundle the view's dependencies so they are excluded together.
- If you want the build to pass even when some table is disabled, **make the `null` branch explicit inside the view constructor**. For example, in an environment where `WeaponTable` may be excluded, you can substitute that slot with an empty sequence so the view exposes an empty result.

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
            // even if the weapon/armor tables are disabled, weaponsByEvent.Get(ev.Id) returns an empty sequence
            ...
        }
    }

    return new UniqueIndex<EventBundle, int>(bundles, b => b.Event.Id);
}
```

Such a branch is valid only when the intent that "this view must survive as an empty result in any environment" is clear. The default recommendation is the first — load the tables a view depends on together. It is natural for the very existence of a view to rest on the assumption that "all of these tables are present."

</br></br></br>

## Load flow and the build stage

The `LoadAsync` flow is almost the same as the single-generic StaticDataManager from [3.5](./05-static-data-manager.md), with **a ViewSet build stage added after all validation passes**.

1. **Schema-stage check** — verify the TableSet single constructor and validate FK targets.
2. **Parallel TableSet load** — per-table `Trace` log `LoadedTable`.
3. **FK value validation** — validate the actual FK values.
4. **StaticDataManager validation** — call `Validate(TTableSet)` (if overridden).
5. **ViewSet build** — `ViewSetBuilder.Build` walks the ViewSet constructor parameters one by one and builds each view in turn. Right after instantiation, that view's `Validate` is also called. Per-view build time is recorded at `Trace` level (message key `BuiltView`).
6. **Snapshot swap** — the newly built `(TableSet, ViewSet)` is swapped into a `volatile` field in one step, and the total completion time is recorded at `Information` level.

If any one view throws an exception during the build, all view attempts are collected to the end and then an `AggregateException(Messages.ViewsFailedToBuild, ...)` is thrown. In this case `Current` is not updated, so the previous snapshot (or, if this is the first load, the still-empty state) is kept.

The diagnostics for when the ViewSet itself has an invalid parameter are also defined.

- If a parameter type is not a `StaticDataView<,>` subtype, `InvalidViewParameter`.
- If a parameter is nullable, `ViewSetMemberMustBeNonNullable`.
- If there is no constructor that takes exactly one TableSet, `ViewConstructorNotFound`.
- If the ViewSet record has more than one constructor, `ViewSetMustHaveSingleConstructor`.

All of these enter as inner exceptions of `AggregateException(Messages.ViewsFailedToBuild, ...)` at the build stage.

</br></br></br>

## Concurrency

The concurrency guarantees are the same as the TableSet's in [3.5](./05-static-data-manager.md#concurrency-in-loadasync). The `LoadAsync` concurrent-entry guard and the atomic swap of `Current` apply as-is, with the addition that the TableSet and ViewSet are swapped together as a single bundle.

The `TableAndViewSet(Tables, Views)` that `Current` points at is swapped atomically as a single bundle. Even if another thread calls `LoadAsync` again while a thread is querying, the query side always sees **a single bundle in which the TableSet and ViewSet are consistent**. An intermediate state in which only one side has been changed to the new value is never exposed. The premise of this guarantee is that the ViewSet is a record with a single constructor, and `ViewSetMustHaveSingleConstructor` enforces that premise.

The call-site pattern is the same as [3.5](./05-static-data-manager.md#querying-with-a-consistent-snapshot). When handling multiple tables or views together in the same operation, **receive `Current` into a variable only once** and retrieve from within it.

```csharp
var snapshot = staticData.Current;
var bundle = snapshot.Views.EventBundleView.Get(1);
var events = snapshot.Tables.EventTable!.Records;
```

</br></br></br>

## Using it with DI in ASP.NET Core

In the same way as [the registration pattern in 3.5](./05-static-data-manager.md#using-it-with-di-in-aspnet-core), bind the Snapshot as Scoped and unfold each table and view from within it. The Manager is a Singleton.

```csharp
// Program.cs
services.AddSingleton<GameStaticData>();

// Share the same (Tables, Views) bundle within one request
services.AddScoped(sp =>
    sp.GetRequiredService<GameStaticData>().Current);

// Each table
services.AddScoped<EventTable>(sp =>
    sp.GetRequiredService<GameStaticData.TableAndViewSet>().Tables.EventTable!);

services.AddScoped<WeaponTable>(sp =>
    sp.GetRequiredService<GameStaticData.TableAndViewSet>().Tables.WeaponTable!);

services.AddScoped<ArmorTable>(sp =>
    sp.GetRequiredService<GameStaticData.TableAndViewSet>().Tables.ArmorTable!);

// Each View (ViewSet members are non-nullable, so there is no !)
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
        // bundles and totals are values taken from the same (TableSet, ViewSet) bundle bound to this request
        ...
    }
}
```

On the application side, it usually keeps the call site cleaner to pick and inject only the tables and Views actually used, rather than receiving the whole `GameStaticData` or the whole `TableSet`.

</br></br></br>

## When you only use a TableSet

If you do not need view composition, you can use `StaticDataManager<TTableSet>` from [3.5](./05-static-data-manager.md) as-is. The two bases exist separately, and you can expand incrementally by moving to the 2-generic base at the point when you need views.

</br></br></br>

## Summary

- `StaticDataManager<TTableSet, TViewSet>` is responsible for view composition.
- A view inherits from `StaticDataView<TTableSet>` and finishes the build in a constructor that takes one TableSet.
- If needed, override `Validate` to place the view's own post-check.
- Keep the ViewSet as a record with non-nullable views, and the call site accesses it through `Current.Views`.
- The TableSet and ViewSet are swapped atomically as a single bundle, so a query always sees a consistent bundle.

---

[← Previous: 3.6 Foreign Keys](./06-foreign-keys.md) | [Table of Contents](../README.md) | [Next: 4.1 StaticDataHeaderGenerator →](../04-cli-tools/01-header-generator.md)
