using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using Sdp.Attributes;
using Sdp.Manager;
using Sdp.Table;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.ForeignKeyTests;

public partial class ForeignKeyNullableBrandingTests(ITestOutputHelper testOutputHelper)
{
    public record struct QuestId(int Value);

    public sealed record ItemCode(string Value);

    private enum HintTarget
    {
        Quest,
        None,
    }

    [StaticDataRecord("Item", "Sheet1")]
    private partial record ItemRecord([Key] ItemCode Code);

    [StaticDataRecord("Quest", "Sheet1")]
    private partial record QuestRecord(
        [Key] QuestId Id,
        [NullString("")] [ForeignKey("Quest", "Id")] QuestId? PrevQuestId,
        [NullString("-")] [ForeignKey("Item", "Code")] ItemCode? RewardItem,
        [Length(2)] [NullString("")] [ForeignKey("Quest", "Id")] ImmutableArray<QuestId?> Related);

    [StaticDataRecord("Hint", "Sheet1")]
    private partial record HintRecord(
        [Key] int Id,
        HintTarget Kind,
        [NullString("")] [SwitchForeignKey(nameof(Kind), "Quest", "Quest", "Id")] QuestId? Target);

    private sealed partial class HintTable(ImmutableArray<HintRecord> records)
        : StaticDataTable<HintRecord>(records);

    private sealed partial class ItemTable(ImmutableArray<ItemRecord> records)
        : StaticDataTable<ItemRecord>(records);

    private sealed partial class QuestTable(ImmutableArray<QuestRecord> records)
        : StaticDataTable<QuestRecord>(records);

    private sealed partial class QuestStaticData(ILogger logger)
        : StaticDataManager<QuestStaticData.TableSet>(logger)
    {
        public sealed partial record TableSet(ItemTable? Item, QuestTable? Quest, HintTable? Hint);
    }

    private const string ItemCsv =
        """
        Code
        sword
        """;

    private const string SingleQuestCsv =
        """
        Id,PrevQuestId,RewardItem,Related[0],Related[1]
        1,,-,,
        """;

    private const string MissingTargetHintCsv =
        """
        Id,Kind,Target
        1,Quest,42
        """;

    private const string ValidHintCsv =
        """
        Id,Kind,Target
        1,Quest,1
        2,None,
        """;

    [Fact]
    public async Task Load_NullAndExistingValues_Succeeds()
    {
        var logger = CreateLogger();

        using var dir = CreateDirectory(
            """
            Id,PrevQuestId,RewardItem,Related[0],Related[1]
            1,,-,,
            2,1,sword,1,
            3,2,-,1,2
            """);

        var staticData = new QuestStaticData(logger);
        await staticData.LoadAsync(dir.Path);

        var quests = staticData.Current.Quest!.Records;
        Assert.Null(quests[0].PrevQuestId);
        Assert.Null(quests[0].RewardItem);
        Assert.Equal(new QuestId(1), quests[1].PrevQuestId);
        Assert.Equal(new ItemCode("sword"), quests[1].RewardItem);
        Assert.Equal([new QuestId(1), new QuestId(2)], quests[2].Related.ToArray());
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public async Task Load_MissingStructBrandingValue_FailsWithInnerValue()
    {
        var logger = CreateLogger();

        using var dir = CreateDirectory(
            """
            Id,PrevQuestId,RewardItem,Related[0],Related[1]
            1,,-,,
            2,99,-,,
            """);

        var staticData = new QuestStaticData(logger);

        var ex = await Assert.ThrowsAsync<AggregateException>(() => staticData.LoadAsync(dir.Path));

        var inner = Assert.Single(ex.InnerExceptions);
        Assert.Contains("QuestRecord[Id=2].PrevQuestId(99)", inner.Message, StringComparison.Ordinal);
        Assert.Contains("Quest.Id", inner.Message, StringComparison.Ordinal);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public async Task Load_MissingClassBrandingValue_FailsWithInnerValue()
    {
        var logger = CreateLogger();

        using var dir = CreateDirectory(
            """
            Id,PrevQuestId,RewardItem,Related[0],Related[1]
            1,,shield,,
            """);

        var staticData = new QuestStaticData(logger);

        var ex = await Assert.ThrowsAsync<AggregateException>(() => staticData.LoadAsync(dir.Path));

        var inner = Assert.Single(ex.InnerExceptions);
        Assert.Contains("QuestRecord[Id=1].RewardItem(shield)", inner.Message, StringComparison.Ordinal);
        Assert.Contains("Item.Code", inner.Message, StringComparison.Ordinal);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public async Task Load_MissingArrayElementValue_FailsWithElementIndexAndInnerValue()
    {
        var logger = CreateLogger();

        using var dir = CreateDirectory(
            """
            Id,PrevQuestId,RewardItem,Related[0],Related[1]
            1,,-,,77
            """);

        var staticData = new QuestStaticData(logger);

        var ex = await Assert.ThrowsAsync<AggregateException>(() => staticData.LoadAsync(dir.Path));

        var inner = Assert.Single(ex.InnerExceptions);
        Assert.Contains("QuestRecord[Id=1].Related[1](77)", inner.Message, StringComparison.Ordinal);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public async Task Load_SwitchForeignKeyWithNullValue_SkipsConditionAndTargetCheck()
    {
        var logger = CreateLogger();

        using var dir = CreateDirectory(SingleQuestCsv, ValidHintCsv);

        var staticData = new QuestStaticData(logger);
        await staticData.LoadAsync(dir.Path);

        var hints = staticData.Current.Hint!.Records;
        Assert.Equal(new QuestId(1), hints[0].Target);
        Assert.Null(hints[1].Target);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public async Task Load_SwitchForeignKeyWithMissingValue_FailsWithInnerValueAndCondition()
    {
        var logger = CreateLogger();

        using var dir = CreateDirectory(SingleQuestCsv, MissingTargetHintCsv);

        var staticData = new QuestStaticData(logger);

        var ex = await Assert.ThrowsAsync<AggregateException>(() => staticData.LoadAsync(dir.Path));

        var inner = Assert.Single(ex.InnerExceptions);
        Assert.Contains("HintRecord[Id=1].Target(42)", inner.Message, StringComparison.Ordinal);
        Assert.Contains("Kind=Quest", inner.Message, StringComparison.Ordinal);
        Assert.Empty(logger.Logs);
    }

    private static CsvTestDirectory CreateDirectory(string questCsv, string hintCsv = ValidHintCsv)
    {
        var dir = new CsvTestDirectory();
        dir.Write("Item.Sheet1.csv", ItemCsv);
        dir.Write("Quest.Sheet1.csv", questCsv);
        dir.Write("Hint.Sheet1.csv", hintCsv);

        return dir;
    }

    private TestOutputLogger<ForeignKeyNullableBrandingTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        var createdLogger = factory.CreateLogger<ForeignKeyNullableBrandingTests>();
        if (createdLogger is not TestOutputLogger<ForeignKeyNullableBrandingTests> logger)
        {
            throw new InvalidOperationException("로거를 만들지 못했다.");
        }

        return logger;
    }
}
