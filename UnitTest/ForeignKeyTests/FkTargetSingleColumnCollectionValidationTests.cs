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

    private const string SfkConsumerCsv =
        """
        Id,Kind,Tag
        1,Alpha,a
        """;

    [StaticDataRecord("TagBundle", "Sheet1")]
    private record TagBundle(
        int Id,
        [SingleColumnCollection(", ")] ImmutableArray<string> Tags);

    [StaticDataRecord("FkConsumer", "Sheet1")]
    private record FkConsumer(
        int Id,
        [ForeignKey("TagBundle", "Tags")] string Tag);

    [StaticDataRecord("SfkConsumer", "Sheet1")]
    private record SfkConsumer(
        int Id,
        TagKind Kind,
        [SwitchForeignKey("Kind", "Alpha", "TagBundle", "Tags")] string Tag);

    private sealed class TagBundleTable(ImmutableList<TagBundle> records)
        : StaticDataTable<TagBundleTable, TagBundle>(records);

    private sealed class FkConsumerTable(ImmutableList<FkConsumer> records)
        : StaticDataTable<FkConsumerTable, FkConsumer>(records);

    private sealed class SfkConsumerTable(ImmutableList<SfkConsumer> records)
        : StaticDataTable<SfkConsumerTable, SfkConsumer>(records);

    private sealed class FkStaticData(ILogger logger)
        : StaticDataManager<FkStaticData.TableSet>(logger)
    {
        public sealed record TableSet(
            TagBundleTable? TagBundle,
            FkConsumerTable? FkConsumer);
    }

    private sealed class SfkStaticData(ILogger logger)
        : StaticDataManager<SfkStaticData.TableSet>(logger)
    {
        public sealed record TableSet(
            TagBundleTable? TagBundle,
            SfkConsumerTable? SfkConsumer);
    }

    [Fact]
    public async Task Load_FkTargetingSingleColumnCollection_ThrowsAggregateException()
    {
        using var dir = new CsvTestDirectory();
        dir.Write("TagBundle.Sheet1.csv", TagBundleCsv);
        dir.Write("FkConsumer.Sheet1.csv", FkConsumerCsv);

        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<FkTargetSingleColumnCollectionValidationTests>() is not TestOutputLogger<FkTargetSingleColumnCollectionValidationTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        var staticData = new FkStaticData(logger);

        var ex = await Assert.ThrowsAsync<AggregateException>(() => staticData.LoadAsync(dir.Path));

        Assert.Contains(ex.InnerExceptions, e => e.Message.Contains("Tags") && e.Message.Contains("TagBundle"));
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public async Task Load_SfkTargetingSingleColumnCollection_ThrowsAggregateException()
    {
        using var dir = new CsvTestDirectory();
        dir.Write("TagBundle.Sheet1.csv", TagBundleCsv);
        dir.Write("SfkConsumer.Sheet1.csv", SfkConsumerCsv);

        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<FkTargetSingleColumnCollectionValidationTests>() is not TestOutputLogger<FkTargetSingleColumnCollectionValidationTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        var staticData = new SfkStaticData(logger);

        var ex = await Assert.ThrowsAsync<AggregateException>(() => staticData.LoadAsync(dir.Path));

        Assert.Contains(ex.InnerExceptions, e => e.Message.Contains("Tags") && e.Message.Contains("TagBundle"));
        Assert.Empty(logger.Logs);
    }
}
