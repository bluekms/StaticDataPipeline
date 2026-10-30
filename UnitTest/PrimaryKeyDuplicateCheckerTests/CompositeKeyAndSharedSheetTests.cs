using ExcelColumnExtractor.Aggregator;
using ExcelColumnExtractor.Checkers;
using ExcelColumnExtractor.Exceptions;
using ExcelColumnExtractor.Mappings;
using ExcelColumnExtractor.Writers;
using Microsoft.Extensions.Logging;
using SchemaInfoScanner.Schemata;
using SchemaInfoScanner.Schemata.TypedPropertySchemata;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.PrimaryKeyDuplicateCheckerTests;

public partial class CompositeKeyAndSharedSheetTests(ITestOutputHelper testOutputHelper)
{
    private const string CompositeKeySource = """
        public sealed partial record Position(int X, int Y);

        [StaticDataRecord("Map", "Tiles")]
        public sealed partial record TileRecord([Key] Position Pos, string Name);
        """;

    private const string CompositeStringKeySource = """
        public sealed partial record Position(string A, string B);

        [StaticDataRecord("Map", "Tiles")]
        public sealed partial record TileRecord([Key] Position Pos, string Name);
        """;

    private const string BrandingKeySource = """
        public sealed partial record TileId(int Value);

        [StaticDataRecord("Map", "Tiles")]
        public sealed partial record TileRecord([Key] TileId Id, string Name);
        """;

    private const string SharedSheetSource = """
        [StaticDataRecord("Unit", "Main")]
        public sealed partial record UnitRecord([Key] int Id, string Name);

        [StaticDataRecord("Unit", "Main")]
        public sealed partial record UnitNameRecord([Key] int Id, string Name);
        """;

    [Fact]
    public void RecordKey_IsComparedAsTuple()
    {
        var logger = CreateLogger();
        var programLogger = CreateProgramLogger();
        var recordSchema = SimpleCordParser.Parse(CompositeKeySource, logger).RawRecordSchemata[0];

        var distinct = BuildMap(recordSchema, ["Pos.X", "Pos.Y", "Name"], [["1", "2", "A"], ["1", "3", "B"]]);
        PrimaryKeyDuplicateChecker.Check(distinct, programLogger);

        var duplicate = BuildMap(recordSchema, ["Pos.X", "Pos.Y", "Name"], [["1", "2", "A"], ["1", "2", "B"]]);
        var ex = Assert.Throws<ExtractionException>(() => PrimaryKeyDuplicateChecker.Check(duplicate, programLogger));

        Assert.Contains("(1, 2)", ex.Message, StringComparison.Ordinal);
        Assert.Single(ex.Failures);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void RecordKey_MemberContainingSeparator_IsNotDuplicate()
    {
        var logger = CreateLogger();
        var programLogger = CreateProgramLogger();
        var recordSchema = SimpleCordParser.Parse(CompositeStringKeySource, logger).RawRecordSchemata[0];

        var map = BuildMap(recordSchema, ["Pos.A", "Pos.B", "Name"], [["a, b", "c", "X"], ["a", "b, c", "Y"]]);
        PrimaryKeyDuplicateChecker.Check(map, programLogger);

        Assert.Empty(logger.Logs);
    }

    [Theory]
    [InlineData("Id")]
    [InlineData("Id.Value")]
    public void BrandingKey_MatchesBothHeaderForms(string header)
    {
        var logger = CreateLogger();
        var programLogger = CreateProgramLogger();
        var recordSchema = SimpleCordParser.Parse(BrandingKeySource, logger).RawRecordSchemata[0];

        var distinct = BuildMap(recordSchema, [header, "Name"], [["1", "A"], ["2", "B"]]);
        PrimaryKeyDuplicateChecker.Check(distinct, programLogger);

        var duplicate = BuildMap(recordSchema, [header, "Name"], [["1", "A"], ["1", "B"]]);
        var ex = Assert.Throws<ExtractionException>(() => PrimaryKeyDuplicateChecker.Check(duplicate, programLogger));

        Assert.Single(ex.Failures);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void SharedSheet_IdenticalTables_WriteOneFile()
    {
        var logger = CreateLogger();
        var schemata = SimpleCordParser.Parse(SharedSheetSource, logger).RawRecordSchemata;
        using var outDir = new CsvTestDirectory();

        var map = new ExtractedTableMap(new Dictionary<RecordSchema, BodyColumnAggregator.ExtractedTable>
        {
            [schemata[0]] = BuildTable(["Id", "Name"], [["1", "Alice"]]),
            [schemata[1]] = BuildTable(["Id", "Name"], [["1", "Alice"]]),
        });

        CsvWriter.Write(outDir.Path, map);

        var file = Assert.Single(Directory.GetFiles(outDir.Path));
        Assert.Equal("Unit.Main.csv", Path.GetFileName(file));
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void SharedSheet_DifferentColumns_WritesUnionOfColumnsAndRows()
    {
        var logger = CreateLogger();
        var schemata = SimpleCordParser.Parse(SharedSheetSource, logger).RawRecordSchemata;
        using var outDir = new CsvTestDirectory();

        var map = new ExtractedTableMap(new Dictionary<RecordSchema, BodyColumnAggregator.ExtractedTable>
        {
            [schemata[0]] = BuildTable(["Id", "Name"], [["1", "Alice"], ["2", "Bob"]]),
            [schemata[1]] = BuildTable(["Id", "Level"], [["1", "10"]]),
        });

        CsvWriter.Write(outDir.Path, map);

        var file = Assert.Single(Directory.GetFiles(outDir.Path));
        Assert.Equal(["Id,Name,Level", "1,Alice,10", "2,Bob,"], File.ReadAllLines(file));
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void SharedSheet_RowOnlyInSecondRecord_IsKeptWithFirstRecordColumnsEmpty()
    {
        var logger = CreateLogger();
        var schemata = SimpleCordParser.Parse(SharedSheetSource, logger).RawRecordSchemata;
        using var outDir = new CsvTestDirectory();

        var map = new ExtractedTableMap(new Dictionary<RecordSchema, BodyColumnAggregator.ExtractedTable>
        {
            [schemata[0]] = BuildTable(["Id", "Name"], [["1", "Alice"]]),
            [schemata[1]] = BuildTable(["Code", "Level"], [["A", "10"], ["B", "20"]]),
        });

        CsvWriter.Write(outDir.Path, map);

        var file = Assert.Single(Directory.GetFiles(outDir.Path));
        Assert.Equal(["Id,Name,Code,Level", "1,Alice,A,10", ",,B,20"], File.ReadAllLines(file));
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void HelpOption_ExitsWithZero()
    {
        var logger = CreateLogger();

        Assert.Equal(0, ExcelColumnExtractor.Program.Main(["--help"]));
        Assert.Empty(logger.Logs);
    }

    private static ExtractedTableMap BuildMap(RecordSchema recordSchema, string[] headers, string[][] rows)
        => new(new Dictionary<RecordSchema, BodyColumnAggregator.ExtractedTable> { [recordSchema] = BuildTable(headers, rows) });

    private static BodyColumnAggregator.ExtractedTable BuildTable(string[] headers, string[][] rows)
    {
        var extractedRows = rows
            .Select((row, rowIndex) => new BodyColumnAggregator.ExtractedRow(
                row.Select((value, columnIndex) => new CellData($"{(char)('A' + columnIndex)}{rowIndex + 2}", value, headers[columnIndex])).ToList()))
            .ToList();

        return new BodyColumnAggregator.ExtractedTable(headers, extractedRows);
    }

    private ILogger<ExcelColumnExtractor.Program> CreateProgramLogger()
        => new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning).CreateLogger<ExcelColumnExtractor.Program>();

    private TestOutputLogger<CompositeKeyAndSharedSheetTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<CompositeKeyAndSharedSheetTests>() is not TestOutputLogger<CompositeKeyAndSharedSheetTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
