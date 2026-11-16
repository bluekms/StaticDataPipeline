using Microsoft.Extensions.Logging;
using ScenarioTest.Tables;
using Sdp.Manager;

namespace ScenarioTest.Managers;

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
