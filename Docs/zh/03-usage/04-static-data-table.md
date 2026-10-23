# 3.4 实现 StaticDataTable

Record 是“一行的形状”，而 `StaticDataTable` 是“容纳那些 Record 的表”。本章中我们构建与 [3.3](./03-first-record.md) 的 `ItemRecord` 对应的 `ItemTable`，并用 `UniqueIndex` 加上键查询。

## 最简单的表定义

```csharp
using System.Collections.Immutable;
using Sdp.Table;

public sealed class ItemTable(ImmutableArray<ItemRecord> records)
    : StaticDataTable<ItemRecord>(records);
```

只需记住两点。

1. 类型参数是记录类型 `TRecord`: `StaticDataTable<ItemRecord>`。
2. 必须提供一个仅接收一个 `ImmutableArray<TRecord>` 的构造函数。Sdp 会通过反射调用此构造函数。

在此状态下，仅通过 `Records` 属性公开整体列表。

```csharp
foreach (var item in table.Records)
{
    // ...
}
```

`Records` 是 `ImmutableArray<ItemRecord>`，并原样维持 CSV 的行顺序。

</br></br></br>

## 用 UniqueIndex 添加键查询

如果需要通过主键直接取出一行的查询，则创建 `UniqueIndex` 并公开 Getter。

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

`UniqueIndex` 在创建时检查键是否重复。如果同一个 `Id` 出现两次以上，会抛出 `InvalidOperationException`。

```csharp
var potion = table.Get(1);

if (table.TryGet(2, out var sword))
{
    Console.WriteLine($"{sword.Name}: {sword.Price}");
}
```

`Get` 在键不存在时抛出 `KeyNotFoundException`，而 `TryGet` 返回 `false`。对于键存在性无法保证的查询，使用 `TryGet`。

如果在其他列上也需要同样方式的查询，只需像 `UniqueIndex<ItemRecord, string> byName` 那样追加索引即可。对于键不唯一的分组查询，使用 `MultiIndex`。

</br></br></br>

## 表内部的额外验证

大多数验证通过 `[Range]`、`[ForeignKey]`、`[RegularExpression]` 这类 Attribute 以声明式方式运作。如果有仅靠 Attribute 难以表达的特殊规则，可以 override `Validate` 来添加自定义验证逻辑。该方法在表创建之后立即被调用，并在 StaticDataManager 阶段的 FK 验证之前执行。

```csharp
public sealed class ItemTable : StaticDataTable<ItemRecord>
{
    public ItemTable(ImmutableArray<ItemRecord> records)
        : base(records)
    {
    }

    protected override void Validate()
    {
        // 示例: 确认每个分类都至少有一个物品
        foreach (var category in Enum.GetValues<ItemCategory>())
        {
            if (!Records.Any(x => x.Category == category))
            {
                throw new InvalidOperationException($"没有与分类 {category} 对应的物品。");
            }
        }
    }
}
```

如上例所示，**跨多行的条件** 无法用仅查看单个单元格值的 Attribute 来表达，因此 `Validate` 是适合的位置。反之，像“价格不为负”这样的单值检查，最好用 `[Range]` 来声明而非 `Validate` — 因为这样在提取阶段也会被一并过滤掉。

表之间引用的有效性通常用 `[ForeignKey]` / `[SwitchForeignKey]` Attribute 来表达更为干净 ([3.6](./06-foreign-keys.md))。`Validate` 留给那些难以用此方式表达的、自身表内部的规则。

</br></br></br>

## CSV 加载怎么办?

`ItemTable` 本身不含 CSV 加载器。无论是单个表，还是多个表的集合，都通过 `StaticDataManager` 加载 ([3.5](./05-static-data-manager.md))。本章仅涉及到“表类的形状”为止。

</br></br></br>

## 小结

- 继承 `StaticDataTable<TRecord>` 并提供 `ImmutableArray<TRecord>` 构造函数。
- `Records` 维持 CSV 的行顺序。
- 键查询或分组查询将 `UniqueIndex` / `MultiIndex` 作为成员持有，并通过 Getter 公开。
- 实际的 CSV 加载由 `StaticDataManager` 负责。

---

[← 上一篇: 3.3 定义你的第一个 Record](./03-first-record.md) | [目录](../README.md) | [下一篇: 3.5 用 StaticDataManager 管理多张表 →](./05-static-data-manager.md)
