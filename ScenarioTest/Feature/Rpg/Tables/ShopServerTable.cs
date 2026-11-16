using System.Collections.Immutable;
using ScenarioTest.Records.Rpg;
using Sdp.Table;

namespace ScenarioTest.Tables;

public sealed partial class ShopServerTable : StaticDataTable<ShopServerRecord>
{
    private readonly UniqueIndex<ShopServerRecord, ItemRecord.ItemId> byItemId;

    public ShopServerTable(ImmutableArray<ShopServerRecord> records)
        : base(records)
    {
        byItemId = new UniqueIndex<ShopServerRecord, ItemRecord.ItemId>(records, x => x.ItemId);
    }

    public ShopServerRecord Get(ItemRecord.ItemId itemId)
    {
        return byItemId.Get(itemId);
    }
}
