using Microsoft.Extensions.Logging;
using Sdp.Attributes;
using Sdp.Manager;
using Sdp.Table;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.ForeignKeyTests;

public partial class SwitchForeignKeyGuidConditionValidationTests(ITestOutputHelper testOutputHelper)
{
    private const string ItemKind = "{0F8FAD5B-D9CB-469F-A165-70867728950E}";
    private const string CharacterKind = "7C9E6679742540DE944BE07FC1F90AE7";

    private const string ValidRewardCsv =
        """
        Id,Kind,TargetId
        1,0f8fad5b-d9cb-469f-a165-70867728950e,101
        2,7c9e6679-7425-40de-944b-e07fc1f90ae7,5
        """;

    private const string ErrorRewardCsv =
        """
        Id,Kind,TargetId
        1,0f8fad5b-d9cb-469f-a165-70867728950e,999
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
        Guid Kind,
        [SwitchForeignKey("Kind", ItemKind, "Item", "Id")]
        [SwitchForeignKey("Kind", CharacterKind, "Character", "Id")]
        int TargetId);

    [StaticDataRecord("Item", "Sheet1")]
    private partial record ItemRecord(int Id, string Name);

    [StaticDataRecord("Character", "Sheet1")]
    private partial record CharacterRecord(int Id, string Name);

    private sealed partial class RewardTable : StaticDataTable<RewardRecord>;

    private sealed partial class ItemTable : StaticDataTable<ItemRecord>;

    private sealed partial class CharacterTable : StaticDataTable<CharacterRecord>;

    private sealed partial class StaticData(ILogger logger)
        : StaticDataManager<StaticData.TableSet>(logger)
    {
        public sealed partial record TableSet(
            RewardTable? Reward,
            ItemTable? Item,
            CharacterTable? Character);

        public RewardTable RewardTable => Current.Reward!;
    }

    [Fact]
    public async Task Load_GuidConditionWrittenInDifferentNotation_Matches()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<SwitchForeignKeyGuidConditionValidationTests>() is not TestOutputLogger<SwitchForeignKeyGuidConditionValidationTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        using var dir = new CsvTestDirectory();
        dir.Write("Reward.Sheet1.csv", ValidRewardCsv);
        dir.Write("Item.Sheet1.csv", ItemCsv);
        dir.Write("Character.Sheet1.csv", CharacterCsv);

        var staticData = new StaticData(logger);
        await staticData.LoadAsync(dir.Path);

        Assert.Equal(2, staticData.RewardTable.Records.Length);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public async Task Load_GuidConditionMatchesButValueMissing_Throws()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<SwitchForeignKeyGuidConditionValidationTests>() is not TestOutputLogger<SwitchForeignKeyGuidConditionValidationTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        using var dir = new CsvTestDirectory();
        dir.Write("Reward.Sheet1.csv", ErrorRewardCsv);
        dir.Write("Item.Sheet1.csv", ItemCsv);
        dir.Write("Character.Sheet1.csv", CharacterCsv);

        var staticData = new StaticData(logger);

        var ex = await Assert.ThrowsAsync<AggregateException>(() => staticData.LoadAsync(dir.Path));

        var inner = Assert.Single(ex.InnerExceptions);
        Assert.Contains("999", inner.Message);
        Assert.Contains("Item", inner.Message);
        Assert.Empty(logger.Logs);
    }
}
