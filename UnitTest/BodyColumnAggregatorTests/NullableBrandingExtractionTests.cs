using CLICommonLibrary;
using ExcelColumnExtractor.Aggregator;
using ExcelColumnExtractor.Checkers;
using ExcelColumnExtractor.Exceptions;
using ExcelColumnExtractor.Scanners;
using Microsoft.Extensions.Logging;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.BodyColumnAggregatorTests;

public class NullableBrandingExtractionTests(ITestOutputHelper testOutputHelper)
{
    // language=C#
    private const string StructBrandingSource = """
        using System.Collections.Frozen;
        using System.Collections.Immutable;
        using Sdp.Attributes;

        namespace NullableBranding;

        public record struct QuestId([Range(1, 999)] int Value);

        [StaticDataRecord("Rpg", "Quests")]
        public sealed record QuestRecord(
            [Key] QuestId Id,
            [NullString("")] QuestId? PrevQuestId,
            [Length(2)] [NullString("-")] ImmutableArray<QuestId?> Related,
            [Length(2)] [NullString("x")] FrozenSet<QuestId?> Unlocks);
        """;

    // language=C#
    private const string ClassBrandingSource = """
        using System.Collections.Immutable;
        using Sdp.Attributes;

        namespace NullableBranding;

        public sealed record NpcCode(string Value);

        [StaticDataRecord("Rpg", "Quests")]
        public sealed record QuestRecord(
            int Id,
            [NullString("-")] NpcCode? Giver,
            [Length(2)] [NullString("-")] ImmutableArray<NpcCode?> Witnesses);
        """;

    private static readonly string[] StructHeaders =
        ["Id", "PrevQuestId", "Related[0]", "Related[1]", "Unlocks[0]", "Unlocks[1]"];

    [Fact]
    public void NullStringCells_AreExtractedAsIs()
    {
        string[][] rows = [["1", string.Empty, "-", "-", "x", "2"], ["2", "1", "1", "-", "1", "x"]];

        var table = Extract(StructBrandingSource, StructHeaders, rows);

        Assert.Equal(StructHeaders, table.Headers);
        Assert.Equal(2, table.Rows.Count);
        Assert.Equal(["1", string.Empty, "-", "-", "x", "2"], CellValues(table, 0));
        Assert.Equal(["2", "1", "1", "-", "1", "x"], CellValues(table, 1));
    }

    [Fact]
    public void ExpandedValueHeaders_AreExtracted()
    {
        string[] headers = ["Id.Value", "PrevQuestId.Value", "Related[0].Value", "Related[1].Value", "Unlocks[0].Value", "Unlocks[1].Value"];

        var table = Extract(StructBrandingSource, headers, [["1", string.Empty, "-", "2", "x", "3"]]);

        Assert.Equal(headers, table.Headers);
        Assert.Single(table.Rows);
        Assert.Equal(["1", string.Empty, "-", "2", "x", "3"], CellValues(table, 0));
    }

    [Fact]
    public void ClassBranding_NullStringCells_AreExtractedAsIs()
    {
        string[] headers = ["Id", "Giver", "Witnesses[0]", "Witnesses[1]"];

        var table = Extract(ClassBrandingSource, headers, [["1", "-", "N1", "-"]]);

        Assert.Single(table.Rows);
        Assert.Equal(["1", "-", "N1", "-"], CellValues(table, 0));
    }

    [Theory]
    [InlineData("abc", "-", "-", "x", "2")]
    [InlineData("0", "-", "-", "x", "2")]
    [InlineData("", "-", "not-a-number", "x", "2")]
    [InlineData("", "-", "-", "x", "1000")]
    public void ValueThatIsNotNullString_IsValidatedAsInnerValue(
        string prevQuestId,
        string related0,
        string related1,
        string unlocks0,
        string unlocks1)
    {
        string[][] rows = [["1", prevQuestId, related0, related1, unlocks0, unlocks1]];

        var ex = Record.Exception(() => Extract(StructBrandingSource, StructHeaders, rows));

        Assert.IsType<ExtractionException>(ex);
    }

    [Fact]
    public void DuplicateNullElementsInSet_FailExtraction()
    {
        string[][] rows = [["1", string.Empty, "-", "-", "x", "x"]];

        var ex = Record.Exception(() => Extract(StructBrandingSource, StructHeaders, rows));

        Assert.IsType<ExtractionException>(ex);
    }

    private static List<string> CellValues(BodyColumnAggregator.ExtractedTable table, int rowIndex)
    {
        return table.Rows[rowIndex].Data
            .Select(cell => cell.Value)
            .ToList();
    }

    private BodyColumnAggregator.ExtractedTable Extract(string recordSource, string[] sheetHeaders, string[][] sheetRows)
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        var createdLogger = factory.CreateLogger<NullableBrandingExtractionTests>();
        if (createdLogger is not TestOutputLogger<NullableBrandingExtractionTests> logger)
        {
            throw new InvalidOperationException("로거를 만들지 못했다.");
        }

        var createdProgramLogger = factory.CreateLogger<ExcelColumnExtractor.Program>();
        if (createdProgramLogger is not TestOutputLogger<ExcelColumnExtractor.Program> programLogger)
        {
            throw new InvalidOperationException("로거를 만들지 못했다.");
        }

        using var recordDirectory = new CsvTestDirectory();
        using var excelDirectory = new CsvTestDirectory();
        recordDirectory.Write("QuestRecord.cs", recordSource);

        var grid = new[] { sheetHeaders }.Concat(sheetRows).ToList();
        MinimalXlsxWriter.Write(Path.Combine(excelDirectory.Path, "Rpg.xlsx"), "Quests", grid);

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
