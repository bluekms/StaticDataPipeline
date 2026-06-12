using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using Sdp.Attributes;
using Sdp.Manager;
using Sdp.Table;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.ForeignKeyTests;

public partial class FkTargetSingleColumnCollectionValidationTests(ITestOutputHelper testOutputHelper)
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
    private partial record TagBundleRecord(
        int Id,
        [SingleColumnCollection(", ")] ImmutableArray<string> Tags);

#pragma warning disable SDP0207 // FK target is SingleColumnCollection — 의도된 invalid 구조. 런타임 거부 검증.
    [StaticDataRecord("FkConsumer", "Sheet1")]
    private partial record FkConsumerRecord(
        int Id,
        [ForeignKey("TagBundle", "Tags")] string Tag);

    [StaticDataRecord("SwitchFkConsumer", "Sheet1")]
    private partial record SwitchFkConsumerRecord(
        int Id,
        TagKind Kind,
        [SwitchForeignKey("Kind", "Alpha", "TagBundle", "Tags")] string Tag);
#pragma warning restore SDP0207

    private sealed partial class TagBundleTable(ImmutableArray<TagBundleRecord> records)
        : StaticDataTable<TagBundleTable, TagBundleRecord>(records);

    private sealed partial class FkConsumerTable(ImmutableArray<FkConsumerRecord> records)
        : StaticDataTable<FkConsumerTable, FkConsumerRecord>(records);

    private sealed partial class SwitchFkConsumerTable(ImmutableArray<SwitchFkConsumerRecord> records)
        : StaticDataTable<SwitchFkConsumerTable, SwitchFkConsumerRecord>(records);

    private sealed partial class FkStaticData(ILogger logger)
        : StaticDataManager<FkStaticData.TableSet>(logger)
    {
        public sealed partial record TableSet(
            TagBundleTable? TagBundle,
            FkConsumerTable? FkConsumer);

        public FkConsumerTable FkConsumerTable => Current.FkConsumer!;
    }

    private sealed partial class SwitchFkStaticData(ILogger logger)
        : StaticDataManager<SwitchFkStaticData.TableSet>(logger)
    {
        public sealed partial record TableSet(
            TagBundleTable? TagBundle,
            SwitchFkConsumerTable? SwitchFkConsumer);

        public SwitchFkConsumerTable SwitchFkConsumerTable => Current.SwitchFkConsumer!;
    }

    // FK target 이 [SingleColumnCollection] 인 구조의 거부는 SG 컴파일타임 진단(SDP0207)으로 검증된다.
    // 런타임에서는 SG-emit ValidateForeignKeys 가 collection 타겟 분기를 만들지 않으므로 FK 검사가 일어나지 않는다.
    // 아래 테스트가 그 동작을 검증한다.
    [Fact]
    public async Task Load_FkTargetingSingleColumnCollection_SkipsFkValidation()
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
        await staticData.LoadAsync(dir.Path);

        Assert.Single(staticData.FkConsumerTable.Records);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public async Task Load_SwitchFkTargetingSingleColumnCollection_SkipsFkValidation()
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
        await staticData.LoadAsync(dir.Path);

        Assert.Single(staticData.SwitchFkConsumerTable.Records);
        Assert.Empty(logger.Logs);
    }
}
