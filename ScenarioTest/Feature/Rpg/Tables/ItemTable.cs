using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using ScenarioTest.Records.Rpg;
using Sdp.Table;

namespace ScenarioTest.Tables;

public sealed partial class ItemTable : StaticDataTable<ItemRecord>
{
    private readonly UniqueIndex<ItemRecord, ItemRecord.ItemId> byId;

    public ItemTable(ImmutableArray<ItemRecord> records)
        : base(records)
    {
        byId = new UniqueIndex<ItemRecord, ItemRecord.ItemId>(records, x => x.Id);
    }

    public ItemRecord Get(ItemRecord.ItemId id)
    {
        return byId.Get(id);
    }

    public bool TryGet(ItemRecord.ItemId id, [NotNullWhen(true)] out ItemRecord? item)
    {
        return byId.TryGet(id, out item);
    }
}
