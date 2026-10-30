using CLICommonLibrary;
using ExcelColumnExtractor.Aggregator;
using ExcelColumnExtractor.Checkers;
using ExcelColumnExtractor.Scanners;
using Microsoft.Extensions.Logging;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.BodyColumnAggregatorTests;

public class IgnoredParameterTests(ITestOutputHelper testOutputHelper)
{
    private const string RecordSource = """
        using Sdp.Attributes;

        namespace IgnoredParameter;

        [StaticDataRecord("Book", "Items")]
        public sealed record ItemRecord(int Id, [Ignore] int Cache, string Name);
        """;

    [Fact]
    public void SheetWithoutIgnoredColumn_Extracts()
    {
        var table = Extract(
            sheetHeaders: ["Id", "Name"],
            sheetRows:
            [
                ["1", "Potion"],
                ["2", "Sword"],
            ]);

        Assert.Equal(["Id", "Name"], table.Headers);
        Assert.Equal(2, table.Rows.Count);
    }

    [Fact]
    public void SheetWithIgnoredColumn_ColumnIsNeitherValidatedNorExtracted()
    {
        var table = Extract(
            sheetHeaders: ["Id", "Cache", "Name"],
            sheetRows:
            [
                ["1", "not-a-number", "Potion"],
            ]);

        Assert.Equal(["Id", "Name"], table.Headers);
        var row = Assert.Single(table.Rows);
        Assert.Equal(["1", "Potion"], row.Data.Select(cell => cell.Value));
    }

    private BodyColumnAggregator.ExtractedTable Extract(string[] sheetHeaders, string[][] sheetRows)
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        var logger = factory.CreateLogger<IgnoredParameterTests>();
        var programLogger = factory.CreateLogger<ExcelColumnExtractor.Program>();

        using var recordDirectory = new CsvTestDirectory();
        using var excelDirectory = new CsvTestDirectory();
        recordDirectory.Write("ItemRecord.cs", RecordSource);

        var grid = new[] { sheetHeaders }.Concat(sheetRows).ToList();
        MinimalXlsxWriter.Write(Path.Combine(excelDirectory.Path, "Book.xlsx"), "Items", grid);

        var catalogs = RecordScanner.Scan(recordDirectory.Path, logger);
        var sheetNameMap = SheetNameScanner.Scan(excelDirectory.Path, logger);
        var requiredHeaderMap = RequiredHeadersChecker.Check(catalogs.RecordSchemaCatalog, sheetNameMap, "A1", logger);
        var extractedTableMap = BodyColumnAggregator.Aggregate(
            catalogs.RecordSchemaCatalog.StaticDataRecordSchemata,
            sheetNameMap,
            requiredHeaderMap,
            "A1",
            logger);
        DataBodyChecker.Check(catalogs, extractedTableMap, programLogger);
        PrimaryKeyDuplicateChecker.Check(extractedTableMap, programLogger);

        return Assert.Single(extractedTableMap.SortedTables).Value;
    }
}
