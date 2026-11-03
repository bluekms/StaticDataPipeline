using System.Globalization;
using CLICommonLibrary;
using ExcelColumnExtractor.Aggregator;
using ExcelColumnExtractor.Checkers;
using ExcelColumnExtractor.Exceptions;
using ExcelColumnExtractor.Scanners;
using Microsoft.Extensions.Logging;
using UnitTest.Utility;
using Xunit.Abstractions;
using ExtractorMessages = ExcelColumnExtractor.Resources.Messages;

namespace UnitTest.BodyColumnAggregatorTests;

public class BrandedElementCollectionExtractionTests(ITestOutputHelper testOutputHelper)
{
    // language=C#
    private const string FixedLengthSource = """
        using System.Collections.Frozen;
        using Sdp.Attributes;

        namespace BrandedElement;

        public sealed record ItemId(int Value);

        [StaticDataRecord("Gacha", "Items")]
        public sealed record GachaRecord(int Id, [Length(2)] FrozenSet<ItemId> PickupItemIds);
        """;

    // language=C#
    private const string DynamicLengthSource = """
        using System.Collections.Immutable;
        using Sdp.Attributes;

        namespace BrandedElement;

        public sealed record ItemId(int Value);

        [StaticDataRecord("Gacha", "Items")]
        public sealed record GachaRecord(int Id, ImmutableArray<ItemId> PickupItemIds);
        """;

    [Fact]
    public void FoldedHeaders_AreExtracted()
    {
        var table = Extract(FixedLengthSource, ["Id", "PickupItemIds[0]", "PickupItemIds[1]"], [["1", "10001", "10002"]]);

        Assert.Equal(["Id", "PickupItemIds[0]", "PickupItemIds[1]"], table.Headers);
        Assert.Equal(["1", "10001", "10002"], Assert.Single(table.Rows).Data.Select(cell => cell.Value));
    }

    [Fact]
    public void ExpandedHeadersWithOtherParameterName_AreExtracted()
    {
        // language=C#
        const string source = """
            using System.Collections.Immutable;
            using Sdp.Attributes;

            namespace BrandedElement;

            public sealed record Code(string Text);

            [StaticDataRecord("Gacha", "Items")]
            public sealed record GachaRecord(int Id, ImmutableArray<Code> Codes);
            """;

        var table = Extract(source, ["Id", "Codes[0].Text", "Codes[1].Text"], [["1", "a", "b"]]);

        Assert.Equal(["Id", "Codes[0].Text", "Codes[1].Text"], table.Headers);
        Assert.Equal(["1", "a", "b"], Assert.Single(table.Rows).Data.Select(cell => cell.Value));
    }

    [Fact]
    public void ValueFallbackHeaders_AreExtracted()
    {
        var table = Extract(FixedLengthSource, ["Id", "PickupItemIds[0].Value", "PickupItemIds[1].Value"], [["1", "10001", "10002"]]);

        Assert.Equal(["Id", "PickupItemIds[0].Value", "PickupItemIds[1].Value"], table.Headers);
        Assert.Equal(["1", "10001", "10002"], Assert.Single(table.Rows).Data.Select(cell => cell.Value));
    }

    [Fact]
    public void HeaderInBothForms_FailsExtraction()
    {
        string[] headers = ["Id", "PickupItemIds[0]", "PickupItemIds[0].Value", "PickupItemIds[1]"];

        var ex = Record.Exception(() => Extract(FixedLengthSource, headers, [["1", "10001", "10001", "10002"]]));

        var extractionException = Assert.IsType<ExtractionException>(ex);
        var expected = string.Format(CultureInfo.CurrentCulture, ExtractorMessages.Composite.BrandedHeaderBothForms, "PickupItemIds[0]", "PickupItemIds[0].Value");
        Assert.Contains(expected, extractionException.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void DynamicLength_MixedHeaderForms_AreExtracted()
    {
        var table = Extract(DynamicLengthSource, ["Id", "PickupItemIds[0]", "PickupItemIds[1].Value"], [["1", "10001", "10002"]]);

        Assert.Equal(["Id", "PickupItemIds[0]", "PickupItemIds[1].Value"], table.Headers);
        Assert.Equal(["1", "10001", "10002"], Assert.Single(table.Rows).Data.Select(cell => cell.Value));
    }

    [Fact]
    public void MapSingleValueRecordValue_FoldedAndExpandedHeaders_AreExtracted()
    {
        // language=C#
        const string source = """
            using System.Collections.Frozen;
            using Sdp.Attributes;

            namespace BrandedElement;

            public sealed record Tag([Key] int Id);

            [StaticDataRecord("Gacha", "Items")]
            public sealed record GachaRecord(int Id, [Length(2)] FrozenDictionary<int, Tag> Tags);
            """;

        var folded = Extract(source, ["Id", "Tags[0]", "Tags[1]"], [["1", "7", "8"]]);
        Assert.Equal(["Id", "Tags[0]", "Tags[1]"], folded.Headers);
        Assert.Equal(["1", "7", "8"], Assert.Single(folded.Rows).Data.Select(cell => cell.Value));

        var expanded = Extract(source, ["Id", "Tags[0].Id", "Tags[1].Id"], [["1", "7", "8"]]);
        Assert.Equal(["Id", "Tags[0].Id", "Tags[1].Id"], expanded.Headers);
        Assert.Equal(["1", "7", "8"], Assert.Single(expanded.Rows).Data.Select(cell => cell.Value));
    }

    [Fact]
    public void FoldedHeader_ValueIsValidated()
    {
        var ex = Record.Exception(() => Extract(FixedLengthSource, ["Id", "PickupItemIds[0]", "PickupItemIds[1]"], [["1", "10001", "not-a-number"]]));

        Assert.IsType<ExtractionException>(ex);
    }

    private BodyColumnAggregator.ExtractedTable Extract(string recordSource, string[] sheetHeaders, string[][] sheetRows)
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<BrandedElementCollectionExtractionTests>() is not TestOutputLogger<BrandedElementCollectionExtractionTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        if (factory.CreateLogger<ExcelColumnExtractor.Program>() is not TestOutputLogger<ExcelColumnExtractor.Program> programLogger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        using var recordDirectory = new CsvTestDirectory();
        using var excelDirectory = new CsvTestDirectory();
        recordDirectory.Write("GachaRecord.cs", recordSource);

        var grid = new[] { sheetHeaders }.Concat(sheetRows).ToList();
        MinimalXlsxWriter.Write(Path.Combine(excelDirectory.Path, "Gacha.xlsx"), "Items", grid);

        var catalogs = RecordScanner.Scan(recordDirectory.Path, logger);
        var sheetNameMap = SheetNameScanner.Scan(excelDirectory.Path, logger);
        var requiredHeaderMap = RequiredHeadersChecker.Check(catalogs.RecordSchemaCatalog, sheetNameMap, "A1", logger);
        var extractedTableMap = BodyColumnAggregator.Aggregate(
            catalogs.RecordSchemaCatalog.StaticDataRecordSchemata,
            sheetNameMap,
            requiredHeaderMap,
            logger);
        DataBodyChecker.Check(catalogs, extractedTableMap, programLogger);
        Assert.Empty(logger.Logs);
        Assert.Empty(programLogger.Logs);

        return Assert.Single(extractedTableMap.SortedTables).Value;
    }
}
