# 3.6 外键 (ForeignKey, SwitchForeignKey)

在 [3.5](./05-static-data-manager.md) 中，我们用 StaticDataManager 加载了分类表和物品表两个表，但并未验证两个表之间的引用。本章介绍如何让该引用自动得到验证。

Sdp 提供两种外键 Attribute。

- `[ForeignKey]` — 当一列始终指向同一个目标表的某一列时使用。
- `[SwitchForeignKey]` — 当同一列根据另一列的值不同而指向不同目标时使用。

两种 Attribute 都由同一个验证流程 (在 `LoadAsync` 内) 处理。

## ForeignKey — 一列指向一个目标

我们声明 [3.5](./05-static-data-manager.md) 示例中的 `ItemRecord.CategoryId` 指向 `CategoryTable` 的 `Id`。

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

两个参数的含义如下。

- 第一个参数 `"CategoryTable"` — **TableSet 的属性名** (= TableSet record 的参数名)。
- 第二个参数 `"Id"` — **该目标表 Record 的参数名**。

验证时会检查 `CategoryId` 是否存在于目标表中。

</br></br></br>

## 验证阶段

`LoadAsync` 中的 ForeignKey 验证分三个阶段进行。

1. **架构阶段 — 仅通过分析 Record/TableSet 类型来确认 (表加载之前)**
   - `tableSetName` 是否作为实际的 TableSet 参数存在? (`FkTargetNotFound`)
   - 目标是否不是被 `[SingleColumnCollection]` 捆绑的列? (`FkTargetIsSingleColumnCollection`)
   - 同一参数上是否没有同时附加 `[ForeignKey]` 和 `[SwitchForeignKey]`? (`FkSwitchFkConflict`)
   - `[SwitchForeignKey]` 的相同条件是否没有出现两次以上? (`SwitchFkDuplicateConditionValue`)
2. **目标解析阶段 — 表加载之后、值验证之前**
   - `recordColumnName` 是否作为目标 Record 的参数存在? (`FkTargetColumnNotFound`)
   - `[SwitchForeignKey]` 的条件列是否存在于同一个 Record 中? (`SwitchFkConditionColumnNotFound`)
   - 目标表是否未被 `disabledTables` 排除? (`FkTargetNotFound`)
3. **值阶段 — 实际引用是否存在**
   - 实际的 CSV 值是否在目标表对应列的值集合之中? (`FkValueNotFound`)
   - `[SwitchForeignKey]` 的条件列值是否匹配某一个分支条件? (`SwitchFkConditionValueNotMatched`)

各阶段按顺序进行，当某一阶段发现失败时，会将该阶段收集到的失败装入 `AggregateException(Messages.FkValidationFailed, ...)` 并立即 throw，不再进入下一阶段。例如，如果目标解析阶段 (2) 失败，则值阶段 (3) 的引用验证不会执行。

</br></br></br>

## 当 ForeignKey 出错时

如果 `Items` 工作表中存在一行 `CategoryId=99` 而该值并不存在，则会如下失败。

```
AggregateException: FK 验证失败。
  - ItemRecord.CategoryId(99) 不存在于 [CategoryTable.Id] 中的任何位置。
```

如果有多行出错，则会逐个装入 `InnerExceptions`。

</br></br></br>

## 多个 ForeignKey — “只要在某一处存在即有效”

存在多个表分担同一套 ID 体系的情况。例如 `RewardRecord.TargetId` 只需存在于 `ItemTable` 或 `CurrencyTable` 任一方即可。`[ForeignKey]` 的 `AllowMultiple = true`，因此可以在同一参数上附加多次，并且 **只要其中一个匹配即通过**。

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

如果 `RewardRecord.TargetId` 为 `1`，则只需存在于 `ItemTable.Id == 1` 或 `CurrencyTable.Id == 1` 任一方即可。这是只有在保证两个表的 ID 体系完全分离、互不重叠时才能干净地运作的模式。

</br></br></br>

## SwitchForeignKey — 根据条件指向的目标会改变

当同一列值必须 **根据另一列的值** 引用不同的表时，使用 `[SwitchForeignKey]`。例如，当奖励种类为 `Item` 时指向 `ItemTable.Id`，为 `Currency` 时指向 `CurrencyTable.Id`。

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

四个参数的含义如下。

- `conditionColumnName` — 同一 Record 中哪一列作为分支条件。
- `conditionValue` — 该列为何值时应用此 SwitchForeignKey。
- `tableSetName` — 该条件下所指向的 TableSet 的属性名。
- `recordColumnName` — 该目标表 Record 的参数名。

上述示例按行的验证结果如下确定。

|Kind|TargetId|检查对象|
|-|-|-|
|Item|10|ItemTable.Id 中必须存在 10|
|Currency|5|CurrencyTable.Id 中必须存在 5|

如果没有任何 `SwitchForeignKey` 匹配条件 (即 `Kind` 为 `Item`、`Currency` 以外的值)，则该行的验证失败。

如果在一个 Record 中两次以上附加具有相同 `(conditionColumnName, conditionValue)` 组合的 `SwitchForeignKey`，则会在提取阶段或 `LoadAsync` 的架构阶段被拒绝。由于针对给定条件的目标表必须正好为一个，因此如果将分支表写成与其自身冲突的形式，会在此处当场被捕获。

</br></br></br>

## 跨两个表的 SwitchForeignKey 示例

我们以更贴近实际的一组来看。奖励工作表只持有种类和目标 ID，并根据种类指向不同的数据表。

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

假设 CSV 如下。

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

`Rewards` 的第 4 行 `Currency / TargetId=9` 不在 `CurrencyTable.Id` 中，因此验证失败。其余三行通过。

</br></br></br>

## 小结

- `[ForeignKey(tableSet, column)]` — 单一目标。附加多次后变为“在其中任一个里存在即有效”。
- `[SwitchForeignKey(conditionColumn, conditionValue, tableSet, column)]` — 分支目标。在同一参数上附加多次以构建分支表。
- 验证在 `LoadAsync` 内分三个阶段处理 — 架构 (目标是否存在)、目标解析 (列/条件列是否存在)、值 (实际引用是否存在) — 失败会作为 `AggregateException` 一次性通知。
- 如果用 `disabledTables` 跳过目标表，则引用该表的 ForeignKey 验证会失败 — 仅 disable 独立的分组才是安全的。

`[ForeignKey]` 和 `[SwitchForeignKey]` 本身的 Attribute 规范 (误用诊断、允许多次附加等) 整理在 [5.2 Attribute 目录](../05-advanced/02-attributes.md) 中。

---

[← 上一篇: 3.5 用 StaticDataManager 管理多个表](./05-static-data-manager.md) | [目录](../README.md) | [下一篇: 3.7 StaticDataView 预生成视图 →](./07-static-data-view.md)
