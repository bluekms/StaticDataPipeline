using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using Sdp.Attributes;
using Sdp.Manager;
using Sdp.Table;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.ForeignKeyTests;

public class FkTargetSingleColumnCollectionValidationTests(ITestOutputHelper testOutputHelper)
{
    private enum TagKind
    {
        Alpha,
    }

    private const string TagBundleCsv =
        """
        Id,Tags
        1,"a, b, c"
        """;

    private const string FkConsumerCsv =
        """
        Id,Tag
        1,a
        """;

    private const string SwitchFkConsumerCsv =
        """
        Id,Kind,Tag
        1,Alpha,a
        """;

    [StaticDataRecord("TagBundle", "Sheet1")]
    private record TagBundleRecord(
        int Id,
        [SingleColumnCollection(", ")] ImmutableArray<string> Tags);

    [StaticDataRecord("FkConsumer", "Sheet1")]
    private record FkConsumerRecord(
        int Id,
        [ForeignKey("TagBundle", "Tags")] string Tag);

    [StaticDataRecord("SwitchFkConsumer", "Sheet1")]
    private record SwitchFkConsumerRecord(
        int Id,
        TagKind Kind,
        [SwitchForeignKey("Kind", "Alpha", "TagBundle", "Tags")] string Tag);

    private sealed class TagBundleTable(ImmutableList<TagBundleRecord> records)
        : StaticDataTable<TagBundleTable, TagBundleRecord>(records);

    private sealed class FkConsumerTable(ImmutableList<FkConsumerRecord> records)
        : StaticDataTable<FkConsumerTable, FkConsumerRecord>(records);

    private sealed class SwitchFkConsumerTable(ImmutableList<SwitchFkConsumerRecord> records)
        : StaticDataTable<SwitchFkConsumerTable, SwitchFkConsumerRecord>(records);

    private sealed class FkStaticData(ILogger logger)
        : StaticDataManager<FkStaticData.TableSet>(logger)
    {
        public sealed record TableSet(
            TagBundleTable? TagBundle,
            FkConsumerTable? FkConsumer);
    }

    private sealed class SwitchFkStaticData(ILogger logger)
        : StaticDataManager<SwitchFkStaticData.TableSet>(logger)
    {
        public sealed record TableSet(
            TagBundleTable? TagBundle,
            SwitchFkConsumerTable? SwitchFkConsumer);
    }

    [Fact]
    public async Task Load_FkTargetingSingleColumnCollection_ThrowsAggregateException()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<FkTargetSingleColumnCollectionValidationTests>() is not TestOutputLogger<FkTargetSingleColumnCollectionValidationTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        using var dir = new CsvTestDirectory();
        dir.Write("TagBundle.Sheet1.csv", TagBundleCsv);
        dir.Write("FkConsumer.Sheet1.csv", FkConsumerCsv);

        var staticData = new FkStaticData(logger);

        var ex = await Assert.ThrowsAsync<AggregateException>(() => staticData.LoadAsync(dir.Path));

        Assert.Contains(ex.InnerExceptions, e => e.Message.Contains("Tags") && e.Message.Contains("TagBundle"));
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public async Task Load_SwitchFkTargetingSingleColumnCollection_ThrowsAggregateException()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<FkTargetSingleColumnCollectionValidationTests>() is not TestOutputLogger<FkTargetSingleColumnCollectionValidationTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        using var dir = new CsvTestDirectory();
        dir.Write("TagBundle.Sheet1.csv", TagBundleCsv);
        dir.Write("SwitchFkConsumer.Sheet1.csv", SwitchFkConsumerCsv);

        var staticData = new SwitchFkStaticData(logger);

        var ex = await Assert.ThrowsAsync<AggregateException>(() => staticData.LoadAsync(dir.Path));

        Assert.Contains(ex.InnerExceptions, e => e.Message.Contains("Tags") && e.Message.Contains("TagBundle"));
        Assert.Empty(logger.Logs);
    }
}
