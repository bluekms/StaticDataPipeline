# 3.4 Implementing StaticDataTable

A Record is "the shape of a single row," and a `StaticDataTable` is "the table that holds those Records." In this chapter we build an `ItemTable` corresponding to the `ItemRecord` from [3.3](./03-first-record.md), and add key lookups with `UniqueIndex`.

## The simplest table definition

```csharp
using System.Collections.Immutable;
using Sdp.Table;

public sealed class ItemTable(ImmutableArray<ItemRecord> records)
    : StaticDataTable<ItemRecord>(records);
```

There are only two things to remember.

1. The type argument is the record type `TRecord`: `StaticDataTable<ItemRecord>`.
2. You must provide a constructor that takes exactly one `ImmutableArray<TRecord>`. Sdp invokes this constructor via reflection.

In this state, only the full list is exposed through the `Records` property.

```csharp
foreach (var item in table.Records)
{
    // ...
}
```

`Records` is an `ImmutableArray<ItemRecord>` and preserves the row order of the CSV as-is.

</br></br></br>

## Adding key lookups with UniqueIndex

If you need a lookup that retrieves a single row directly by primary key, create a `UniqueIndex` and expose a getter.

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

`UniqueIndex` checks for duplicate keys at construction time. If the same `Id` appears more than once, it throws an `InvalidOperationException`.

```csharp
var potion = table.Get(1);

if (table.TryGet(2, out var sword))
{
    Console.WriteLine($"{sword.Name}: {sword.Price}");
}
```

`Get` throws a `KeyNotFoundException` when the key is missing, while `TryGet` returns `false`. Use `TryGet` for lookups where key existence is not guaranteed.

If you need the same kind of lookup on another column, just add an index such as `UniqueIndex<ItemRecord, string> byName`. For group lookups where the key is not unique, use `MultiIndex`.

</br></br></br>

## Additional in-table validation

Most validation works declaratively through Attributes such as `[Range]`, `[ForeignKey]`, and `[RegularExpression]`. If you have a special rule that is hard to express with Attributes alone, you can override `Validate` to add custom validation logic. This method is invoked right after the table is created, and runs before the FK validation at the StaticDataManager stage.

```csharp
public sealed class ItemTable : StaticDataTable<ItemRecord>
{
    public ItemTable(ImmutableArray<ItemRecord> records)
        : base(records)
    {
    }

    protected override void Validate()
    {
        // Example: verify that every category has at least one item
        foreach (var category in Enum.GetValues<ItemCategory>())
        {
            if (!Records.Any(x => x.Category == category))
            {
                throw new InvalidOperationException($"No item matches category {category}.");
            }
        }
    }
}
```

As in the example above, a **condition that spans multiple rows** cannot be expressed by an Attribute that only sees a single cell value, so `Validate` is the right place for it. Conversely, a single-value check such as "the price is not negative" is better declared with `[Range]` rather than `Validate` — because it is then also filtered out at the extraction stage.

The validity of references between tables is usually cleaner to express with the `[ForeignKey]` / `[SwitchForeignKey]` Attributes ([3.6](./06-foreign-keys.md)). Leave `Validate` for rules internal to your own table that are hard to express that way.

</br></br></br>

## What about loading CSVs?

`ItemTable` itself does not contain a CSV loader. Both a single table and a bundle of multiple tables are loaded through `StaticDataManager` ([3.5](./05-static-data-manager.md)). This chapter only covers "the shape of the table class."

</br></br></br>

## Summary

- Inherit from `StaticDataTable<TRecord>` and provide an `ImmutableArray<TRecord>` constructor.
- `Records` preserves the row order of the CSV.
- For key lookups or group lookups, keep a `UniqueIndex` / `MultiIndex` as a member and expose it through a getter.
- The actual CSV loading is handled by `StaticDataManager`.

---

[← Previous: 3.3 Defining Your First Record](./03-first-record.md) | [Table of Contents](../README.md) | [Next: 3.5 Managing Multiple Tables with StaticDataManager →](./05-static-data-manager.md)
