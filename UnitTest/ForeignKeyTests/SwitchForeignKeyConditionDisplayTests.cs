using Microsoft.Extensions.Logging;
using Sdp.Attributes;
using Sdp.Manager;
using Sdp.Table;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.ForeignKeyTests;

public partial class SwitchForeignKeyConditionDisplayTests(ITestOutputHelper testOutputHelper)
{
    private const string ValidSeasonCsv =
        """
        Id,Season,ItemId
        1,2026-06-01,20001
        """;

    private const string ConditionNotMatchedSeasonCsv =
        """
        Id,Season,ItemId
        1,2026-07-01,20001
        """;

    private const string TargetValueNotFoundSeasonCsv =
        """
        Id,Season,ItemId
        1,2026-06-01,99999
        """;

    private const string ValidShiftCsv =
        """
        Id,At,ItemId
        1,09:00,20001
        """;

    private const string ConditionNotMatchedShiftCsv =
        """
        Id,At,ItemId
        1,12:30,20001
        """;

    private const string Season1ItemCsv =
        """
        Id,Name
        20001,Season1Sword
        """;

    private const string ItemCsv =
        """
        Id,Name
        1,Sword
        """;

    [StaticDataRecord("Season", "Sheet1")]
    private partial record SeasonRewardRecord(
        [Key] int Id,
        [DateTimeFormat("yyyy-MM-dd")] DateTime Season,
        [SwitchForeignKey("Season", "2026-06-01", "Season1Item", "Id")]
        [SwitchForeignKey("Season", "2026-11-01", "Item", "Id")]
        int ItemId);

    [StaticDataRecord("Shift", "Sheet1")]
    private partial record ShiftRewardRecord(
        [Key] int Id,
        [TimeSpanFormat(@"hh\:mm")] TimeSpan At,
        [SwitchForeignKey("At", "09:00", "Season1Item", "Id")]
        [SwitchForeignKey("At", "18:00", "Item", "Id")]
        int ItemId);

    [StaticDataRecord("Season1Item", "Sheet1")]
    private partial record Season1ItemRecord(int Id, string Name);

    [StaticDataRecord("Item", "Sheet1")]
    private partial record ItemRecord(int Id, string Name);

    private sealed partial class SeasonRewardTable : StaticDataTable<SeasonRewardRecord>;

    private sealed partial class ShiftRewardTable : StaticDataTable<ShiftRewardRecord>;

    private sealed partial class Season1ItemTable : StaticDataTable<Season1ItemRecord>;

    private sealed partial class ItemTable : StaticDataTable<ItemRecord>;

    private sealed partial class StaticData(ILogger logger)
        : StaticDataManager<StaticData.TableSet>(logger)
    {
        public sealed partial record TableSet(
            SeasonRewardTable? SeasonReward,
            ShiftRewardTable? ShiftReward,
            Season1ItemTable? Season1Item,
            ItemTable? Item);
    }

    [Fact]
    public async Task Load_ConditionNotMatched_ShowsConditionValueInRecordFormat()
    {
        var logger = CreateLogger();

        using var dir = new CsvTestDirectory();
        dir.Write("Season.Sheet1.csv", ConditionNotMatchedSeasonCsv);
        dir.Write("Shift.Sheet1.csv", ValidShiftCsv);
        dir.Write("Season1Item.Sheet1.csv", Season1ItemCsv);
        dir.Write("Item.Sheet1.csv", ItemCsv);

        var staticData = new StaticData(logger);

        var ex = await Assert.ThrowsAsync<AggregateException>(() => staticData.LoadAsync(dir.Path));

        var inner = Assert.Single(ex.InnerExceptions);
        Assert.Contains("Season=2026-07-01", inner.Message);
        Assert.DoesNotContain("07/01/2026", inner.Message);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public async Task Load_TargetValueNotFound_ShowsConditionValueInRecordFormat()
    {
        var logger = CreateLogger();

        using var dir = new CsvTestDirectory();
        dir.Write("Season.Sheet1.csv", TargetValueNotFoundSeasonCsv);
        dir.Write("Shift.Sheet1.csv", ValidShiftCsv);
        dir.Write("Season1Item.Sheet1.csv", Season1ItemCsv);
        dir.Write("Item.Sheet1.csv", ItemCsv);

        var staticData = new StaticData(logger);

        var ex = await Assert.ThrowsAsync<AggregateException>(() => staticData.LoadAsync(dir.Path));

        var inner = Assert.Single(ex.InnerExceptions);
        Assert.Contains("Season=2026-06-01", inner.Message);
        Assert.DoesNotContain("06/01/2026", inner.Message);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public async Task Load_TimeSpanCondition_ShowsConditionValueInRecordFormat()
    {
        var logger = CreateLogger();

        using var dir = new CsvTestDirectory();
        dir.Write("Season.Sheet1.csv", ValidSeasonCsv);
        dir.Write("Shift.Sheet1.csv", ConditionNotMatchedShiftCsv);
        dir.Write("Season1Item.Sheet1.csv", Season1ItemCsv);
        dir.Write("Item.Sheet1.csv", ItemCsv);

        var staticData = new StaticData(logger);

        var ex = await Assert.ThrowsAsync<AggregateException>(() => staticData.LoadAsync(dir.Path));

        var inner = Assert.Single(ex.InnerExceptions);
        Assert.Contains("At=12:30", inner.Message);
        Assert.DoesNotContain("At=12:30:00", inner.Message);
        Assert.Empty(logger.Logs);
    }

    private TestOutputLogger<SwitchForeignKeyConditionDisplayTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<SwitchForeignKeyConditionDisplayTests>() is not TestOutputLogger<SwitchForeignKeyConditionDisplayTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
