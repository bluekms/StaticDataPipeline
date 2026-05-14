using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using Sdp.Attributes;
using Sdp.Manager;
using Sdp.Table;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.ForeignKeyTests;

public class SwitchForeignKeySameConditionMultipleAttrTests(ITestOutputHelper testOutputHelper)
{
    private enum Kind
    {
        Item,
        None,
    }

    private const string QuestCsv =
        """
        QuestId,Kind,RewardId
        1,Item,101
        """;

    private const string ItemCsv =
        """
        Id,Name
        101,sword
        """;

    private const string FooCsv =
        """
        Id,Name
        201,foo-a
        """;

    [StaticDataRecord("Quest", "Sheet1")]
    private record QuestRecord(
        int QuestId,
        Kind Kind,
        [SwitchForeignKey("Kind", "Item", "Item", "Id")]
        [SwitchForeignKey("Kind", "Item", "Foo",  "Id")]
        int RewardId);

    [StaticDataRecord("Item", "Sheet1")]
    private record ItemRecord(int Id, string Name);

    [StaticDataRecord("Foo", "Sheet1")]
    private record FooRecord(int Id, string Name);

    private sealed class QuestTable(ImmutableList<QuestRecord> records)
        : StaticDataTable<QuestTable, QuestRecord>(records);

    private sealed class ItemTable(ImmutableList<ItemRecord> records)
        : StaticDataTable<ItemTable, ItemRecord>(records);

    private sealed class FooTable(ImmutableList<FooRecord> records)
        : StaticDataTable<FooTable, FooRecord>(records);

    private sealed class StaticData(ILogger logger)
        : StaticDataManager<StaticData.TableSet>(logger)
    {
        public sealed record TableSet(
            QuestTable? Quest,
            ItemTable? Item,
            FooTable? Foo);
    }

    [Fact]
    public async Task Load_DuplicateConditionValueOnSwitchFk_RejectedByStaticValidator()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<SwitchForeignKeySameConditionMultipleAttrTests>()
            is not TestOutputLogger<SwitchForeignKeySameConditionMultipleAttrTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        using var dir = new CsvTestDirectory();
        dir.Write("Quest.Sheet1.csv", QuestCsv);
        dir.Write("Item.Sheet1.csv", ItemCsv);
        dir.Write("Foo.Sheet1.csv", FooCsv);

        var staticData = new StaticData(logger);

        var ex = await Assert.ThrowsAsync<AggregateException>(() => staticData.LoadAsync(dir.Path));

        var duplicate = Assert.Single(ex.InnerExceptions);
        Assert.Contains("QuestRecord", duplicate.Message);
        Assert.Contains("RewardId", duplicate.Message);
        Assert.Contains("Kind", duplicate.Message);
        Assert.Contains("Item", duplicate.Message);
        Assert.Empty(logger.Logs);
    }

    private const string QuestTripleCsv =
        """
        QuestId,Kind,RewardId
        1,Item,101
        """;

    private const string BarCsv =
        """
        Id,Name
        301,bar-a
        """;

    [StaticDataRecord("QuestTriple", "Sheet1")]
    private record QuestTripleRecord(
        int QuestId,
        Kind Kind,
        [SwitchForeignKey("Kind", "Item", "Item", "Id")]
        [SwitchForeignKey("Kind", "Item", "Foo",  "Id")]
        [SwitchForeignKey("Kind", "Item", "Bar",  "Id")]
        int RewardId);

    [StaticDataRecord("Bar", "Sheet1")]
    private record BarRecord(int Id, string Name);

    private sealed class QuestTripleTable(ImmutableList<QuestTripleRecord> records)
        : StaticDataTable<QuestTripleTable, QuestTripleRecord>(records);

    private sealed class BarTable(ImmutableList<BarRecord> records)
        : StaticDataTable<BarTable, BarRecord>(records);

    private sealed class StaticDataTriple(ILogger logger)
        : StaticDataManager<StaticDataTriple.TableSet>(logger)
    {
        public sealed record TableSet(
            QuestTripleTable? QuestTriple,
            ItemTable? Item,
            FooTable? Foo,
            BarTable? Bar);
    }

    [Fact]
    public async Task Load_ThreeAttributesSameCondition_ReportsSingleError()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<SwitchForeignKeySameConditionMultipleAttrTests>()
            is not TestOutputLogger<SwitchForeignKeySameConditionMultipleAttrTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        using var dir = new CsvTestDirectory();
        dir.Write("QuestTriple.Sheet1.csv", QuestTripleCsv);
        dir.Write("Item.Sheet1.csv", ItemCsv);
        dir.Write("Foo.Sheet1.csv", FooCsv);
        dir.Write("Bar.Sheet1.csv", BarCsv);

        var staticData = new StaticDataTriple(logger);

        var ex = await Assert.ThrowsAsync<AggregateException>(() => staticData.LoadAsync(dir.Path));

        var duplicate = Assert.Single(ex.InnerExceptions);
        Assert.Contains("QuestTripleRecord", duplicate.Message);
        Assert.Contains("RewardId", duplicate.Message);
        Assert.Empty(logger.Logs);
    }
}
