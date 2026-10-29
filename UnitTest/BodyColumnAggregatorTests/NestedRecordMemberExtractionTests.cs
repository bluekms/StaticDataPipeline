using CLICommonLibrary;
using ExcelColumnExtractor.Aggregator;
using ExcelColumnExtractor.Checkers;
using ExcelColumnExtractor.Exceptions;
using ExcelColumnExtractor.Scanners;
using Microsoft.Extensions.Logging;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.BodyColumnAggregatorTests;

public class NestedRecordMemberExtractionTests(ITestOutputHelper testOutputHelper)
{
    private const string NestedSource = """
        using System.Collections.Immutable;
        using Sdp.Attributes;

        namespace Nested;

        public sealed record Stat(int Power, ImmutableArray<int> Mods, [Ignore] string Note);

        [StaticDataRecord("Book", "Heroes")]
        public sealed record HeroRecord([Key] int Id, Stat Stat, string Name);
        """;

    private static readonly string[] Headers = ["Id", "Stat.Power", "Stat.Mods[0]", "Stat.Mods[1]", "Name"];

    [Fact]
    public void DynamicLengthInsideNestedRecord_UsesMemberName()
    {
        var table = Extract(NestedSource, Headers, [["1", "10", "3", "4", "Warrior"]]);

        Assert.Equal(Headers, table.Headers);
        Assert.Equal(["1", "10", "3", "4", "Warrior"], Assert.Single(table.Rows).Data.Select(cell => cell.Value));
    }

    [Fact]
    public void InvalidNestedCollectionCell_FailsWithRecordPrefix()
    {
        var ex = Record.Exception(() => Extract(NestedSource, Headers, [["1", "10", "3", "oops", "Warrior"]]));

        var extraction = Assert.IsType<ExtractionException>(ex);
        var failure = Assert.Single(extraction.Failures);
        Assert.Equal("Nested.HeroRecord", failure.RecordSchema.RecordName.FullName);
        Assert.StartsWith("Nested.HeroRecord: ", extraction.Message, StringComparison.Ordinal);
        Assert.Same(failure.Cause, extraction.InnerException);
    }

    private BodyColumnAggregator.ExtractedTable Extract(string recordSource, string[] sheetHeaders, string[][] sheetRows)
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        var logger = factory.CreateLogger<NestedRecordMemberExtractionTests>();
        var programLogger = factory.CreateLogger<ExcelColumnExtractor.Program>();

        using var recordDirectory = new CsvTestDirectory();
        using var excelDirectory = new CsvTestDirectory();
        recordDirectory.Write("HeroRecord.cs", recordSource);

        var grid = new[] { sheetHeaders }.Concat(sheetRows).ToList();
        MinimalXlsxWriter.Write(Path.Combine(excelDirectory.Path, "Book.xlsx"), "Heroes", grid);

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
