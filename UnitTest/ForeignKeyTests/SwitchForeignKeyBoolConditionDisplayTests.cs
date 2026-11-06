using Microsoft.Extensions.Logging;
using Sdp.Attributes;
using Sdp.Manager;
using Sdp.Table;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.ForeignKeyTests;

public partial class SwitchForeignKeyBoolConditionDisplayTests(ITestOutputHelper testOutputHelper)
{
    private const string ItemCsv =
        """
        Id
        1
        """;

    [StaticDataRecord("Item", "Sheet1")]
    private partial record ItemRecord([Key] int Id);

    [StaticDataRecord("Pass", "Sheet1")]
    private partial record PassRecord(
        [Key] int Id,
        bool IsPremium,
        [SwitchForeignKey(nameof(IsPremium), "true", "Item", "Id")] int RewardId,
        [NullString("")] bool? IsBonus,
        [SwitchForeignKey(nameof(IsBonus), "true", "Item", "Id")] int BonusId);

    private sealed partial class ItemTable : StaticDataTable<ItemRecord>;

    private sealed partial class PassTable : StaticDataTable<PassRecord>;

    private sealed partial class StaticData(ILogger logger)
        : StaticDataManager<StaticData.TableSet>(logger)
    {
        public sealed partial record TableSet(ItemTable? Item, PassTable? Pass);
    }

    [Theory]
    [InlineData("1,true,999,true,1", "PassRecord[Id=1].RewardId(999)", "IsPremium=true")]
    [InlineData("1,false,1,true,1", "PassRecord[Id=1].RewardId", "IsPremium=false")]
    [InlineData("1,true,1,true,999", "PassRecord[Id=1].BonusId(999)", "IsBonus=true")]
    [InlineData("1,true,1,false,1", "PassRecord[Id=1].BonusId", "IsBonus=false")]
    public async Task Load_BoolConditionMismatch_ShowsConditionValueAsDeclared(
        string passRow,
        string expectedSource,
        string expectedCondition)
    {
        var logger = CreateLogger();

        using var dir = new CsvTestDirectory();
        dir.Write("Item.Sheet1.csv", ItemCsv);
        dir.Write("Pass.Sheet1.csv", "Id,IsPremium,RewardId,IsBonus,BonusId\n" + passRow);

        var staticData = new StaticData(logger);

        var ex = await Assert.ThrowsAsync<AggregateException>(() => staticData.LoadAsync(dir.Path));

        var inner = Assert.Single(ex.InnerExceptions);
        Assert.Contains(expectedSource, inner.Message, StringComparison.Ordinal);
        Assert.Contains(expectedCondition, inner.Message, StringComparison.Ordinal);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public async Task Load_NullBoolCondition_ShowsEmptyConditionValue()
    {
        var logger = CreateLogger();

        using var dir = new CsvTestDirectory();
        dir.Write("Item.Sheet1.csv", ItemCsv);
        dir.Write("Pass.Sheet1.csv", "Id,IsPremium,RewardId,IsBonus,BonusId\n1,true,1,,1");

        var staticData = new StaticData(logger);

        var ex = await Assert.ThrowsAsync<AggregateException>(() => staticData.LoadAsync(dir.Path));

        var inner = Assert.Single(ex.InnerExceptions);
        Assert.Contains("PassRecord[Id=1].BonusId", inner.Message, StringComparison.Ordinal);
        Assert.Contains("IsBonus=", inner.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("IsBonus=true", inner.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("IsBonus=false", inner.Message, StringComparison.Ordinal);
        Assert.Empty(logger.Logs);
    }

    private TestOutputLogger<SwitchForeignKeyBoolConditionDisplayTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        var created = factory.CreateLogger<SwitchForeignKeyBoolConditionDisplayTests>();
        if (created is not TestOutputLogger<SwitchForeignKeyBoolConditionDisplayTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
