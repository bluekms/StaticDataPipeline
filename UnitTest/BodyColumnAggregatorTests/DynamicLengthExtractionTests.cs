using CLICommonLibrary;
using ExcelColumnExtractor.Aggregator;
using ExcelColumnExtractor.Checkers;
using ExcelColumnExtractor.Exceptions;
using ExcelColumnExtractor.Scanners;
using Microsoft.Extensions.Logging;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.BodyColumnAggregatorTests;

public class DynamicLengthExtractionTests(ITestOutputHelper testOutputHelper)
{
    private const string PrimitiveArraySource = """
        using System.Collections.Immutable;
        using Sdp.Attributes;

        namespace DynamicLength;

        [StaticDataRecord("Book", "Items")]
        public sealed record ItemRecord(int Id, ImmutableArray<int> Scores, string Name);
        """;

    private const string RecordArraySource = """
        using System.Collections.Immutable;
        using Sdp.Attributes;

        namespace DynamicLength;

        public sealed record Point(int X, int Y);

        [StaticDataRecord("Book", "Items")]
        public sealed record ItemRecord(int Id, ImmutableArray<Point> Points);
        """;

    [Fact]
    public void TwoIndexedColumns_AreExtracted()
    {
        var table = Extract(PrimitiveArraySource, ["Id", "Scores[0]", "Scores[1]", "Name"], [["1", "10", "20", "Potion"]]);

        Assert.Equal(["Id", "Scores[0]", "Scores[1]", "Name"], table.Headers);
        Assert.Equal(["1", "10", "20", "Potion"], Assert.Single(table.Rows).Data.Select(cell => cell.Value));
    }

    [Fact]
    public void NoIndexedColumn_IsEmptyCollection()
    {
        var table = Extract(PrimitiveArraySource, ["Id", "Name"], [["1", "Potion"]]);

        Assert.Equal(["Id", "Name"], table.Headers);
        Assert.Single(table.Rows);
    }

    [Fact]
    public void IndexedColumn_ValueIsValidated()
    {
        var ex = Record.Exception(() => Extract(PrimitiveArraySource, ["Id", "Scores[0]", "Name"], [["1", "not-a-number", "Potion"]]));

        Assert.IsType<DataBodyCheckerException>(ex);
    }

    [Fact]
    public void RecordArrayGroups_AreExtracted()
    {
        var table = Extract(RecordArraySource, ["Id", "Points[0].X", "Points[0].Y", "Points[1].X", "Points[1].Y"], [["1", "1", "2", "3", "4"]]);

        Assert.Equal(["Id", "Points[0].X", "Points[0].Y", "Points[1].X", "Points[1].Y"], table.Headers);
        Assert.Single(table.Rows);
    }

    private const string SameNameNestedSource = """
        using System.Collections.Immutable;
        using Sdp.Attributes;

        namespace DynamicLength;

        public sealed record Inner(ImmutableArray<int> Items);

        [StaticDataRecord("Book", "Items")]
        public sealed record ItemRecord(int Id, ImmutableArray<Inner> Items);
        """;

    [Fact]
    public void SameNamedNestedCollection_AllCellsAreValidated()
    {
        string[] headers = ["Id", "Items[0].Items[0]", "Items[0].Items[1]", "Items[1].Items[0]", "Items[1].Items[1]", "Items[2].Items[0]", "Items[2].Items[1]"];

        var table = Extract(SameNameNestedSource, headers, [["1", "1", "2", "3", "4", "5", "6"]]);
        Assert.Equal(headers, table.Headers);

        var ex = Record.Exception(() => Extract(SameNameNestedSource, headers, [["1", "1", "2", "3", "4", "5", "oops"]]));
        Assert.IsType<DataBodyCheckerException>(ex);
    }

    [Fact]
    public void NonContiguousIndexedColumns_FailExtraction()
    {
        var ex = Record.Exception(() => Extract(PrimitiveArraySource, ["Id", "Scores[0]", "Scores[2]", "Name"], [["1", "10", "30", "Potion"]]));

        Assert.IsType<AggregateException>(ex);
        Assert.Contains("Scores[2]", ex!.Message, StringComparison.Ordinal);
    }

    private BodyColumnAggregator.ExtractedTable Extract(string recordSource, string[] sheetHeaders, string[][] sheetRows)
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        var logger = factory.CreateLogger<DynamicLengthExtractionTests>();
        var programLogger = factory.CreateLogger<ExcelColumnExtractor.Program>();

        using var recordDirectory = new CsvTestDirectory();
        using var excelDirectory = new CsvTestDirectory();
        recordDirectory.Write("ItemRecord.cs", recordSource);

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

        return Assert.Single(extractedTableMap.SortedTables).Value;
    }
}
