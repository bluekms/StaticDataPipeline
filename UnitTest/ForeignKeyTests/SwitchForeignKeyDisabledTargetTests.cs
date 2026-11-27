using System.Collections.Immutable;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Sdp.Attributes;
using Sdp.Manager;
using Sdp.Table;
using UnitTest.Utility;
using SdpMessages = Sdp.Resources.Messages;

namespace UnitTest.ForeignKeyTests;

public partial class SwitchForeignKeyDisabledTargetTests
{
    private enum RewardKind
    {
        Weapon,
        Armor,
        Gold,
    }

    private const string AllKindsRewardCsv =
        """
        Id,Kind,TargetId
        1,Weapon,101
        2,Armor,102
        3,Gold,1
        4,Weapon,102
        """;

    private const string GoldOnlyRewardCsv =
        """
        Id,Kind,TargetId
        1,Gold,1
        2,Gold,2
        """;

    private const string UnknownWeaponRewardCsv =
        """
        Id,Kind,TargetId
        1,Weapon,999
        2,Gold,1
        """;

    private const string ItemCsv =
        """
        Id,Name
        101,Sword
        102,Shield
        """;

    private const string CurrencyCsv =
        """
        Id,Name
        1,Gold
        2,Gem
        """;

    [StaticDataRecord("Reward", "Sheet1")]
    private partial record RewardRecord(
        int Id,
        RewardKind Kind,
        [SwitchForeignKey("Kind", "Weapon", "Item", "Id")]
        [SwitchForeignKey("Kind", "Armor", "Item", "Id")]
        [SwitchForeignKey("Kind", "Gold", "Currency", "Id")]
        int TargetId);

    [StaticDataRecord("Item", "Sheet1")]
    private partial record ItemRecord(int Id, string Name);

    [StaticDataRecord("Currency", "Sheet1")]
    private partial record CurrencyRecord(int Id, string Name);

    private sealed partial class RewardTable(ImmutableArray<RewardRecord> records)
        : StaticDataTable<RewardRecord>(records);

    private sealed partial class ItemTable(ImmutableArray<ItemRecord> records)
        : StaticDataTable<ItemRecord>(records);

    private sealed partial class CurrencyTable(ImmutableArray<CurrencyRecord> records)
        : StaticDataTable<CurrencyRecord>(records);

    private sealed partial class StaticData(ILogger logger)
        : StaticDataManager<StaticData.TableSet>(logger)
    {
        public sealed partial record TableSet(
            RewardTable? Reward,
            ItemTable? Item,
            CurrencyTable? Currency);
    }

    private static string TargetNotLoadedMessage(string target)
    {
        var format = SdpMessages.Composite.FkTargetNotFound;
        return string.Format(CultureInfo.CurrentCulture, format, "RewardRecord.TargetId", target);
    }

    private static CsvTestDirectory CreateDirectory(string rewardCsv)
    {
        var dir = new CsvTestDirectory();
        dir.Write("Reward.Sheet1.csv", rewardCsv);
        dir.Write("Item.Sheet1.csv", ItemCsv);
        dir.Write("Currency.Sheet1.csv", CurrencyCsv);

        return dir;
    }

    private static async Task<List<string>> LoadFailureMessages(string rewardCsv, List<string> disabledTables)
    {
        using var dir = CreateDirectory(rewardCsv);

        var staticData = new StaticData(NullLogger.Instance);
        var exception = await Assert.ThrowsAsync<AggregateException>(() => staticData.LoadAsync(dir.Path, disabledTables));

        return exception.InnerExceptions.Select(x => x.Message).ToList();
    }

    [Fact]
    public async Task OneOfTwoTargetsDisabled_ReportsOnlyTheDisabledTarget()
    {
        var messages = await LoadFailureMessages(AllKindsRewardCsv, ["Currency"]);

        var message = Assert.Single(messages);
        Assert.Equal(TargetNotLoadedMessage("Currency"), message);
    }

    [Fact]
    public async Task TargetSharedByTwoConditionsDisabled_IsReportedOnce()
    {
        var messages = await LoadFailureMessages(AllKindsRewardCsv, ["Item"]);

        var message = Assert.Single(messages);
        Assert.Equal(TargetNotLoadedMessage("Item"), message);
    }

    [Fact]
    public async Task BothTargetsDisabled_ReportsEachTargetOnce()
    {
        var messages = await LoadFailureMessages(AllKindsRewardCsv, ["Item", "Currency"]);

        Assert.Equal(2, messages.Count);
        Assert.Contains(TargetNotLoadedMessage("Item"), messages);
        Assert.Contains(TargetNotLoadedMessage("Currency"), messages);
    }

    [Fact]
    public async Task DisabledTargetAndUnknownValueInLoadedTarget_ReportsBoth()
    {
        var messages = await LoadFailureMessages(UnknownWeaponRewardCsv, ["Currency"]);

        Assert.Equal(2, messages.Count);
        Assert.Contains(TargetNotLoadedMessage("Currency"), messages);
        Assert.Contains(messages, x => x.Contains("TargetId(999)", StringComparison.Ordinal));
    }

    [Fact]
    public async Task DisabledTargetNoRowPointsTo_DoesNotFail()
    {
        using var dir = CreateDirectory(GoldOnlyRewardCsv);

        var staticData = new StaticData(NullLogger.Instance);
        await staticData.LoadAsync(dir.Path, ["Item"]);

        Assert.Equal(2, staticData.Current.Reward!.Records.Length);
    }
}
