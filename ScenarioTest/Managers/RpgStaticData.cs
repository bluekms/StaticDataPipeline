using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.Extensions.Logging;
using ScenarioTest.Records.Rpg;
using Sdp.Manager;
using Sdp.Table;

namespace ScenarioTest.Managers;

public sealed partial class ItemTable : StaticDataTable<ItemRecord>
{
    private readonly UniqueIndex<ItemRecord, ItemRecord.ItemId> byId;

    public ItemTable(ImmutableArray<ItemRecord> records)
        : base(records)
    {
        byId = new UniqueIndex<ItemRecord, ItemRecord.ItemId>(records, x => x.Id);
    }

    public ItemRecord Get(ItemRecord.ItemId id)
        => byId.Get(id);

    public bool TryGet(ItemRecord.ItemId id, [NotNullWhen(true)] out ItemRecord? item)
        => byId.TryGet(id, out item);
}

public sealed partial class CurrencyTable(ImmutableArray<CurrencyRecord> records)
    : StaticDataTable<CurrencyRecord>(records);

public sealed partial class SkillTable(ImmutableArray<SkillRecord> records)
    : StaticDataTable<SkillRecord>(records);

public sealed partial class MonsterTable : StaticDataTable<MonsterRecord>
{
    private readonly UniqueIndex<MonsterRecord, MonsterId> byId;

    public MonsterTable(ImmutableArray<MonsterRecord> records)
        : base(records)
    {
        byId = new UniqueIndex<MonsterRecord, MonsterId>(records, x => x.Id);
    }

    public MonsterRecord Get(MonsterId id)
        => byId.Get(id);
}

public sealed partial class RewardTable(ImmutableArray<RewardRecord> records)
    : StaticDataTable<RewardRecord>(records);

public sealed partial class FreeRewardTable(ImmutableArray<FreeRewardRecord> records)
    : StaticDataTable<FreeRewardRecord>(records);

public sealed partial class PremiumRewardTable(ImmutableArray<PremiumRewardRecord> records)
    : StaticDataTable<PremiumRewardRecord>(records);

public sealed partial class SeasonPassTable(ImmutableArray<SeasonPassRecord> records)
    : StaticDataTable<SeasonPassRecord>(records);

public sealed partial class SeasonRewardTable(ImmutableArray<SeasonRewardRecord> records)
    : StaticDataTable<SeasonRewardRecord>(records);

public sealed partial class Season1ItemTable(ImmutableArray<Season1ItemRecord> records)
    : StaticDataTable<Season1ItemRecord>(records);

public sealed partial class QuestTable(ImmutableArray<QuestRecord> records)
    : StaticDataTable<QuestRecord>(records);

public sealed partial class AttendanceTable(ImmutableArray<AttendanceRecord> records)
    : StaticDataTable<AttendanceRecord>(records);

public sealed partial class ShopServerTable : StaticDataTable<ShopServerRecord>
{
    private readonly UniqueIndex<ShopServerRecord, ItemRecord.ItemId> byItemId;

    public ShopServerTable(ImmutableArray<ShopServerRecord> records)
        : base(records)
    {
        byItemId = new UniqueIndex<ShopServerRecord, ItemRecord.ItemId>(records, x => x.ItemId);
    }

    public ShopServerRecord Get(ItemRecord.ItemId itemId)
        => byItemId.Get(itemId);
}

public sealed partial class ShopClientTable(ImmutableArray<ShopClientRecord> records)
    : StaticDataTable<ShopClientRecord>(records);

public sealed partial class BuffTable(ImmutableArray<BuffRecord> records)
    : StaticDataTable<BuffRecord>(records.Where(x => !x.IsDebuff).ToImmutableArray());

public sealed partial class DebuffTable(ImmutableArray<BuffRecord> records)
    : StaticDataTable<BuffRecord>(records.Where(x => x.IsDebuff).ToImmutableArray());

public sealed partial class GachaTable(ImmutableArray<GachaRecord> records)
    : StaticDataTable<GachaRecord>(records)
{
    protected override void Validate()
    {
        var total = Records.Sum(x => x.Rate);
        if (total != 100m)
        {
            throw new InvalidOperationException(string.Format(
                CultureInfo.InvariantCulture,
                "Gacha rates must add up to 100 but add up to {0}.",
                total));
        }
    }
}

public sealed partial class RpgStaticData(ILogger logger)
    : StaticDataManager<RpgStaticData.TableSet>(logger)
{
    public sealed partial record TableSet(
        ItemTable ItemTable,
        CurrencyTable? CurrencyTable,
        SkillTable? SkillTable,
        MonsterTable? MonsterTable,
        RewardTable? RewardTable,
        FreeRewardTable? FreeRewardTable,
        PremiumRewardTable? PremiumRewardTable,
        SeasonPassTable? SeasonPassTable,
        SeasonRewardTable? SeasonRewardTable,
        Season1ItemTable? Season1ItemTable,
        QuestTable? QuestTable,
        AttendanceTable? AttendanceTable,
        ShopServerTable? ShopServerTable,
        ShopClientTable? ShopClientTable,
        BuffTable? BuffTable,
        DebuffTable? DebuffTable,
        GachaTable? GachaTable);
}
