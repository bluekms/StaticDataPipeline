using System.Collections.Frozen;
using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using Sdp.Attributes;
using Sdp.Csv;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.CsvMappingBehaviorTests;

public partial class NullableBrandingMappingTests(ITestOutputHelper testOutputHelper)
{
    public record struct QuestId(int Value);

    public sealed record NpcCode([RegularExpression("^N[0-9]+$")] string Value);

    [StaticDataRecord("NullableBranding", "Direct")]
    public sealed partial record DirectRecord(
        [Key] QuestId Id,
        [NullString("")] QuestId? PrevQuestId,
        [NullString("-")] NpcCode? Giver);

    [StaticDataRecord("NullableBranding", "Elements")]
    public sealed partial record ElementsRecord(
        int Id,
        [Length(3)] [NullString("")] ImmutableArray<QuestId?> Related,
        [NullString("x")] FrozenSet<NpcCode?> Witnesses);

    public sealed record Reward([NullString("")] QuestId? NextQuest, int Count);

    [StaticDataRecord("NullableBranding", "Nested")]
    public sealed partial record NestedRecord(
        int Id,
        Reward Reward,
        [Length(2)] FrozenDictionary<int, Stage> Stages);

    public sealed record Stage([Key] int Order, [NullString("-")] NpcCode? Boss);

    [Fact]
    public void MapsNullableBrandingInsideNestedRecordAndMapValue()
    {
        var logger = CreateLogger();

        var csv = "Id,Reward.NextQuest,Reward.Count,Stages[0].Order,Stages[0].Boss,Stages[1].Order,Stages[1].Boss\n"
            + "1,,3,1,-,2,N9\n"
            + "2,7,1,1,N1,2,-";
        var records = CsvLoader.Parse(csv, NestedRecord.MapFromCsvRow).ToList();

        Assert.Equal(2, records.Count);
        Assert.Null(records[0].Reward.NextQuest);
        Assert.Null(records[0].Stages[1].Boss);
        Assert.Equal(new NpcCode("N9"), records[0].Stages[2].Boss);
        Assert.Equal(new QuestId(7), records[1].Reward.NextQuest);
        Assert.Equal(new NpcCode("N1"), records[1].Stages[1].Boss);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void MapsNullStringCellToNullAndValueCellToBranding()
    {
        var logger = CreateLogger();

        var csv = "Id,PrevQuestId,Giver\n1,,-\n2,1,N7";
        var records = CsvLoader.Parse(csv, DirectRecord.MapFromCsvRow).ToList();

        Assert.Equal(2, records.Count);
        Assert.Null(records[0].PrevQuestId);
        Assert.Null(records[0].Giver);
        Assert.Equal(new QuestId(1), records[1].PrevQuestId);
        Assert.Equal(new NpcCode("N7"), records[1].Giver);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void ReadsExpandedValueHeaderAsFallback()
    {
        var logger = CreateLogger();

        var csv = "Id.Value,PrevQuestId.Value,Giver.Value\n2,1,-";
        var record = Assert.Single(CsvLoader.Parse(csv, DirectRecord.MapFromCsvRow));

        Assert.Equal(new QuestId(2), record.Id);
        Assert.Equal(new QuestId(1), record.PrevQuestId);
        Assert.Null(record.Giver);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void AppliesInnerValidationWhenCellIsNotNullString()
    {
        var logger = CreateLogger();

        var csv = "Id,PrevQuestId,Giver\n1,,bad";

        Assert.ThrowsAny<Exception>(() => CsvLoader.Parse(csv, DirectRecord.MapFromCsvRow));
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void MapsNullableElementsOfArrayAndSet()
    {
        var logger = CreateLogger();

        var csv = "Id,Related[0],Related[1],Related[2],Witnesses[0],Witnesses[1]\n1,10,,30,N1,x";
        var record = Assert.Single(CsvLoader.Parse(csv, ElementsRecord.MapFromCsvRow));

        Assert.Equal([new QuestId(10), null, new QuestId(30)], record.Related.ToArray());
        var witnesses = record.Witnesses.ToList();
        Assert.Equal(2, witnesses.Count);
        Assert.Contains(new NpcCode("N1"), witnesses);
        Assert.Contains(null, witnesses);
        Assert.Empty(logger.Logs);
    }

    private TestOutputLogger<NullableBrandingMappingTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        var createdLogger = factory.CreateLogger<NullableBrandingMappingTests>();
        if (createdLogger is not TestOutputLogger<NullableBrandingMappingTests> logger)
        {
            throw new InvalidOperationException("로거를 만들지 못했다.");
        }

        return logger;
    }
}
