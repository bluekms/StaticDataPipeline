using Microsoft.Extensions.Logging;
using Sdp.Attributes;
using Sdp.Manager;
using Sdp.Table;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.ForeignKeyTests;

public partial class SwitchForeignKeyTypedConditionValidationTests(ITestOutputHelper testOutputHelper)
{
    private const string ValidRewardCsv =
        """
        Id,Tier,TargetId
        1,1.50,101
        2,2.0,5
        """;

    private const string ErrorRewardCsv =
        """
        Id,Tier,TargetId
        1,1.5,999
        """;

    private const string ValidSeasonCsv =
        """
        Id,StartDay,TargetId
        1,2024-01-01,101
        2,2024-07-01,5
        """;

    private const string ItemCsv =
        """
        Id,Name
        101,전설의 검
        """;

    private const string CharacterCsv =
        """
        Id,Name
        5,기사
        """;

    [StaticDataRecord("Reward", "Sheet1")]
    private partial record RewardRecord(
        int Id,
        decimal Tier,
        [SwitchForeignKey("Tier", "1.5", "Item", "Id")]
        [SwitchForeignKey("Tier", "2", "Character", "Id")]
        int TargetId);

    [StaticDataRecord("Season", "Sheet1")]
    private partial record SeasonRecord(
        int Id,
        [DateTimeFormat("yyyy-MM-dd")] DateTime StartDay,
        [SwitchForeignKey("StartDay", "2024-01-01", "Item", "Id")]
        [SwitchForeignKey("StartDay", "2024-07-01", "Character", "Id")]
        int TargetId);

    [StaticDataRecord("Item", "Sheet1")]
    private partial record ItemRecord(int Id, string Name);

    [StaticDataRecord("Character", "Sheet1")]
    private partial record CharacterRecord(int Id, string Name);

    private sealed partial class RewardTable : StaticDataTable<RewardRecord>;

    private sealed partial class SeasonTable : StaticDataTable<SeasonRecord>;

    private sealed partial class ItemTable : StaticDataTable<ItemRecord>;

    private sealed partial class CharacterTable : StaticDataTable<CharacterRecord>;

    private sealed partial class StaticData(ILogger logger)
        : StaticDataManager<StaticData.TableSet>(logger)
    {
        public sealed partial record TableSet(
            RewardTable? Reward,
            SeasonTable? Season,
            ItemTable? Item,
            CharacterTable? Character);
    }

    [Fact]
    public async Task Load_DecimalAndDateTimeConditions_MatchByValue()
    {
        var logger = CreateLogger();

        using var dir = new CsvTestDirectory();
        dir.Write("Reward.Sheet1.csv", ValidRewardCsv);
        dir.Write("Season.Sheet1.csv", ValidSeasonCsv);
        dir.Write("Item.Sheet1.csv", ItemCsv);
        dir.Write("Character.Sheet1.csv", CharacterCsv);

        var staticData = new StaticData(logger);
        await staticData.LoadAsync(dir.Path);

        Assert.Equal(2, staticData.Current.Reward!.Records.Length);
        Assert.Equal(2, staticData.Current.Season!.Records.Length);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public async Task Load_DecimalConditionMatchesButValueMissing_Throws()
    {
        var logger = CreateLogger();

        using var dir = new CsvTestDirectory();
        dir.Write("Reward.Sheet1.csv", ErrorRewardCsv);
        dir.Write("Season.Sheet1.csv", ValidSeasonCsv);
        dir.Write("Item.Sheet1.csv", ItemCsv);
        dir.Write("Character.Sheet1.csv", CharacterCsv);

        var staticData = new StaticData(logger);

        var ex = await Assert.ThrowsAsync<AggregateException>(() => staticData.LoadAsync(dir.Path));

        var inner = Assert.Single(ex.InnerExceptions);
        Assert.Contains("999", inner.Message);
        Assert.Contains("Item", inner.Message);
        Assert.Empty(logger.Logs);
    }

    private TestOutputLogger<SwitchForeignKeyTypedConditionValidationTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<SwitchForeignKeyTypedConditionValidationTests>() is not TestOutputLogger<SwitchForeignKeyTypedConditionValidationTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
