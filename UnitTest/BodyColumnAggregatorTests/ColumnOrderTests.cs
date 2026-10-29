using System.Globalization;
using CLICommonLibrary;
using ExcelColumnExtractor.Aggregator;
using ExcelColumnExtractor.Checkers;
using ExcelColumnExtractor.Scanners;
using Microsoft.Extensions.Logging;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.BodyColumnAggregatorTests;

public class ColumnOrderTests(ITestOutputHelper testOutputHelper)
{
    private const string RecordSource = """
        using Sdp.Attributes;

        namespace ColumnOrder;

        [StaticDataRecord("Order", "Items")]
        public sealed record ItemRecord(int Id, string Name, int Price);
        """;

    private const string BrandedIdRecordSource = """
        using Sdp.Attributes;

        namespace ColumnOrder;

        public sealed record ItemId(int Value);

        [StaticDataRecord("Order", "Items")]
        public sealed record ItemRecord(ItemId Id, string Name, int Price);
        """;

    private const string DuplicateColumnNameRecordSource = """
        using Sdp.Attributes;

        namespace ColumnOrder;

        [StaticDataRecord("Order", "Items")]
        public sealed record ItemRecord(
            int Id,
            [ColumnName("Price")] int Price,
            [ColumnName("Price")] int Cost);
        """;

    [Fact]
    public void SheetColumnOrderSameAsRecord_HeadersMatchValues()
    {
        var table = Extract(
            RecordSource,
            sheetHeaders: ["Id", "Name", "Price"],
            sheetRows:
            [
                ["1", "Potion", "100"],
                ["2", "Sword", "5000"],
            ]);

        AssertTable(table, RecordOrderHeaders, RecordOrderRows);
    }

    [Fact]
    public void SheetColumnOrderDiffersFromRecord_HeadersMatchValues()
    {
        var table = Extract(
            RecordSource,
            sheetHeaders: ["Price", "Name", "Id"],
            sheetRows:
            [
                ["100", "Potion", "1"],
                ["5000", "Sword", "2"],
            ]);

        AssertTable(table, RecordOrderHeaders, RecordOrderRows);
    }

    [Fact]
    public void SheetHasExtraColumnBetween_HeadersMatchValues()
    {
        var table = Extract(
            RecordSource,
            sheetHeaders: ["Name", "Memo", "Id", "Price"],
            sheetRows:
            [
                ["Potion", "회복용", "1", "100"],
                ["Sword", "기본 검", "2", "5000"],
            ]);

        AssertTable(table, RecordOrderHeaders, RecordOrderRows);
    }

    [Fact]
    public void ValueFallbackHeader_HeadersMatchValues()
    {
        var table = Extract(
            BrandedIdRecordSource,
            sheetHeaders: ["Price", "Id.Value", "Name"],
            sheetRows:
            [
                ["100", "1", "Potion"],
                ["5000", "2", "Sword"],
            ]);

        AssertTable(table, ["Id.Value", "Name", "Price"], RecordOrderRows);
    }

    [Fact]
    public void StartCellNotA1_HeadersMatchValues()
    {
        var table = Extract(
            RecordSource,
            sheetHeaders: ["Price", "Name", "Id"],
            sheetRows:
            [
                ["100", "Potion", "1"],
                ["5000", "Sword", "2"],
            ],
            startCell: "B2");

        AssertTable(table, RecordOrderHeaders, RecordOrderRows);
    }

    [Fact]
    public void TwoHeadersReferToSameSheetColumn_Throws()
    {
        var exception = Assert.Throws<AggregateException>(() => Extract(
            DuplicateColumnNameRecordSource,
            sheetHeaders: ["Id", "Price"],
            sheetRows:
            [
                ["1", "100"],
            ]));

        testOutputHelper.WriteLine(exception.Message);
        Assert.Contains("Price", exception.Message, StringComparison.Ordinal);
    }

    private static void AssertTable(
        BodyColumnAggregator.ExtractedTable table,
        string[] expectedHeaders,
        string[][] expectedRows)
    {
        Assert.Equal(expectedHeaders, table.Headers);
        Assert.Equal(expectedRows.Length, table.Rows.Count);
        for (var i = 0; i < expectedRows.Length; i++)
        {
            var values = table.Rows[i].Data.Select(cell => cell.Value);
            Assert.Equal(expectedRows[i], values);
        }
    }

    private static readonly string[] RecordOrderHeaders = ["Id", "Name", "Price"];

    private static readonly string[][] RecordOrderRows =
    [
        ["1", "Potion", "100"],
        ["2", "Sword", "5000"],
    ];

    private BodyColumnAggregator.ExtractedTable Extract(
        string recordSource,
        string[] sheetHeaders,
        string[][] sheetRows,
        string startCell = "A1")
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        var logger = factory.CreateLogger<ColumnOrderTests>();

        using var recordDirectory = new CsvTestDirectory();
        using var excelDirectory = new CsvTestDirectory();
        recordDirectory.Write("ItemRecord.cs", recordSource);

        var columnOffset = startCell[0] - 'A';
        var rowOffset = int.Parse(startCell[1..], CultureInfo.InvariantCulture) - 1;
        var padding = Enumerable.Repeat("설명", columnOffset).ToArray();
        var paddedRows = new[] { sheetHeaders }
            .Concat(sheetRows)
            .Select(row => padding.Concat(row).ToArray());
        var grid = Enumerable.Repeat<string[]>(["시트 설명"], rowOffset)
            .Concat(paddedRows)
            .ToList();
        MinimalXlsxWriter.Write(Path.Combine(excelDirectory.Path, "Order.xlsx"), "Items", grid);

        var catalogs = RecordScanner.Scan(recordDirectory.Path, logger);
        var sheetNameMap = SheetNameScanner.Scan(excelDirectory.Path, logger);
        var requiredHeaderMap = RequiredHeadersChecker.Check(catalogs.RecordSchemaCatalog, sheetNameMap, startCell, logger);
        var extractedTableMap = BodyColumnAggregator.Aggregate(
            catalogs.RecordSchemaCatalog.StaticDataRecordSchemata,
            sheetNameMap,
            requiredHeaderMap,
            startCell,
            logger);

        var table = Assert.Single(extractedTableMap.SortedTables).Value;
        testOutputHelper.WriteLine(string.Join(",", table.Headers));
        foreach (var row in table.Rows)
        {
            testOutputHelper.WriteLine(string.Join(",", row.Data.Select(cell => cell.Value)));
        }

        return table;
    }
}
