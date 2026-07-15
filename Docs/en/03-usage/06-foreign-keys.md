# 3.6 Foreign Keys (ForeignKey, SwitchForeignKey)

In [3.5](./05-static-data-manager.md) we loaded two tables — a category table and an item table — with a StaticDataManager, but we did not validate the reference between the two tables. This chapter covers how to make that reference validate automatically.

Sdp provides two kinds of foreign key Attributes.

- `[ForeignKey]` — when one column always points at one column of the same target table.
- `[SwitchForeignKey]` — when the same column points at a different target depending on the value of another column.

Both Attributes are processed by the same validation flow (inside `LoadAsync`).

## ForeignKey — one column points at one target

We declare that `ItemRecord.CategoryId` from the [3.5](./05-static-data-manager.md) example points at the `Id` of `CategoryTable`.

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

The meaning of the two arguments is as follows.

- The first argument `"CategoryTable"` — the **property name of the TableSet** (= the parameter name of the TableSet record).
- The second argument `"Id"` — the **parameter name of that table's Record**.

During validation, it checks whether `CategoryId` exists in the target table.

</br></br></br>

## Validation stages

ForeignKey validation in `LoadAsync` proceeds in three stages.

1. **Schema stage — verified by analyzing the Record/TableSet types alone (before tables are loaded)**
   - Does `tableSetName` exist as an actual TableSet parameter? (`FkTargetNotFound`)
   - Is the target not a column bundled with `[SingleColumnCollection]`? (`FkTargetIsSingleColumnCollection`)
   - Are `[ForeignKey]` and `[SwitchForeignKey]` not both attached to the same parameter? (`FkSwitchFkConflict`)
   - Does the same condition of a `[SwitchForeignKey]` not appear more than once? (`SwitchFkDuplicateConditionValue`)
2. **Target resolution stage — after tables are loaded, right before value validation**
   - Does `recordColumnName` exist as a parameter of the target Record? (`FkTargetColumnNotFound`)
   - Does the condition column of a `[SwitchForeignKey]` exist within the same Record? (`SwitchFkConditionColumnNotFound`)
   - Was the target table not excluded by `disabledTables`? (`FkTargetNotFound`)
3. **Value stage — whether the actual reference exists**
   - Is the actual CSV value within the set of values of the corresponding column of the target table? (`FkValueNotFound`)
   - Does the condition column value of a `[SwitchForeignKey]` match one of the branch conditions? (`SwitchFkConditionValueNotMatched`)

Each stage proceeds sequentially, and when a failure is found at one stage, the failures collected at that stage are placed in an `AggregateException(Messages.FkValidationFailed, ...)`, thrown immediately, and the next stage is not reached. For example, if the target resolution stage (2) fails, the reference validation of the value stage (3) is not run.

</br></br></br>

## When a ForeignKey is broken

If the `Items` sheet has a row with `CategoryId=99` that does not exist, it fails as follows.

```
AggregateException: FK validation failed.
  - ItemRecord.CategoryId(99) not found in any of: [CategoryTable.Id]
```

If multiple rows are broken, each is placed in `InnerExceptions` one by one.

</br></br></br>

## Multiple ForeignKeys — "valid if present anywhere"

There are cases where the same ID scheme is split across multiple tables. For example, a situation where `RewardRecord.TargetId` only needs to exist in either `ItemTable` or `CurrencyTable`. `[ForeignKey]` has `AllowMultiple = true`, so it can be attached multiple times to the same parameter, and **passes if either one matches**.

```csharp
[StaticDataRecord("GameItems", "Currencies")]
public sealed record CurrencyRecord(
    int Id,
    string Name);

[StaticDataRecord("GameItems", "Rewards")]
public sealed record RewardRecord(
    int Id,
    [ForeignKey("ItemTable", "Id")]
    [ForeignKey("CurrencyTable", "Id")]
    int TargetId);
```

If `RewardRecord.TargetId` is `1`, it only needs to exist in either `ItemTable.Id == 1` or `CurrencyTable.Id == 1`. This is a pattern that works cleanly only when there is a guarantee that the two tables' ID schemes are completely separate and do not overlap.

</br></br></br>

## SwitchForeignKey — the target it points at changes by condition

When the same column value must reference a different table **depending on the value of another column**, use `[SwitchForeignKey]`. For example, when the reward kind is `Item`, make it point at `ItemTable.Id`, and when it is `Currency`, make it point at `CurrencyTable.Id`.

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

The meaning of the four arguments is as follows.

- `conditionColumnName` — which column within the same Record is the branch condition.
- `conditionValue` — for what value of that column this SwitchForeignKey applies.
- `tableSetName` — the property name of the TableSet to point at for that condition.
- `recordColumnName` — the parameter name of that table's Record.

The per-row validation results for the example above are determined as follows.

|Kind|TargetId|What is checked|
|-|-|-|
|Item|10|10 must exist in ItemTable.Id|
|Currency|5|5 must exist in CurrencyTable.Id|

If no `SwitchForeignKey` matches the condition (if `Kind` is a value other than `Item` or `Currency`), validation fails for that row.

If you attach a `SwitchForeignKey` with the same `(conditionColumnName, conditionValue)` combination more than once within one Record, it is rejected at the extraction stage or at the schema stage of `LoadAsync`. Since the target table for a given condition must be exactly one, a case where the branch table conflicts with itself is caught right there.

</br></br></br>

## A SwitchForeignKey example spanning two tables

Let us look at a more realistic bundle. The reward sheet has only the kind and the target ID, and points at a different data table depending on the kind.

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

Suppose the CSVs are as follows.

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

Row 4 of `Rewards`, `Currency / TargetId=9`, is not in `CurrencyTable.Id`, so validation fails. The other three rows pass.

</br></br></br>

## Summary

- `[ForeignKey(tableSet, column)]` — a single target. Attaching it multiple times makes it "valid if present in any one of them."
- `[SwitchForeignKey(conditionColumn, conditionValue, tableSet, column)]` — a branching target. Attach it multiple times to the same parameter to build a branch table.
- Validation is processed inside `LoadAsync` in three stages — schema (whether the target exists), target resolution (whether the column / condition column exists), and value (whether the actual reference exists) — and failures are reported all at once as an `AggregateException`.
- If you skip a target table with `disabledTables`, ForeignKey validation that references that table fails — it is safe to disable only independent groups.

The Attribute specifications of `[ForeignKey]` and `[SwitchForeignKey]` themselves (misuse diagnostics, multiple-attachment allowance, etc.) are organized in [5.2 Attribute Catalog](../05-advanced/02-attributes.md).

---

[← Previous: 3.5 Managing Multiple Tables with StaticDataManager](./05-static-data-manager.md) | [Table of Contents](../README.md) | [Next: 3.7 StaticDataView Pre-built Views →](./07-static-data-view.md)
