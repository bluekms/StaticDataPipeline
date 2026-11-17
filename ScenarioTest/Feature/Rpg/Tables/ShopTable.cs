using System.Collections.Immutable;
using ScenarioTest.Records.Rpg;
using Sdp.Table;

namespace ScenarioTest.Tables;

public sealed partial class ShopTable : StaticDataTable<ShopRecord>
{
    private readonly UniqueIndex<ShopRecord, ItemRecord.ItemId> byItemId;

    public ShopTable(ImmutableArray<ShopRecord> records)
        : base(records)
    {
        byItemId = new UniqueIndex<ShopRecord, ItemRecord.ItemId>(records, x => x.ItemId);
    }

    public ShopRecord Get(ItemRecord.ItemId itemId)
    {
        return byItemId.Get(itemId);
    }
}
