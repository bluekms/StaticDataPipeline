using ExcelColumnExtractor.Aggregator;
using ExcelColumnExtractor.Checkers;
using ExcelColumnExtractor.Exceptions;
using ExcelColumnExtractor.Mappings;
using Microsoft.Extensions.Logging;
using SchemaInfoScanner;
using SchemaInfoScanner.Catalogs;
using SchemaInfoScanner.Collectors;
using SchemaInfoScanner.Schemata;
using SchemaInfoScanner.Schemata.TypedPropertySchemata;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.PrimaryKeyDuplicateCheckerTests;

public class PrimaryKeyDuplicateCheckerTest(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void UniqueKeys_Passes()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        var logger = factory.CreateLogger<ExcelColumnExtractor.Program>();

        var recordSchema = BuildRecordSchema();
        var table = BuildTable(
            headers: ["Id", "Name"],
            rows:
            [
                ["1", "Alice"],
                ["2", "Bob"],
                ["3", "Carol"],
            ]);

        var map = new ExtractedTableMap(new Dictionary<RecordSchema, BodyColumnAggregator.ExtractedTable>
        {
            [recordSchema] = table,
        });

        PrimaryKeyDuplicateChecker.Check(map, logger);
    }

    [Fact]
    public void DuplicateKey_ThrowsDataBodyCheckerException()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        var logger = factory.CreateLogger<ExcelColumnExtractor.Program>();

        var recordSchema = BuildRecordSchema();
        var table = BuildTable(
            headers: ["Id", "Name"],
            rows:
            [
                ["1", "Alice"],
                ["2", "Bob"],
                ["1", "Dave"],
            ]);

        var map = new ExtractedTableMap(new Dictionary<RecordSchema, BodyColumnAggregator.ExtractedTable>
        {
            [recordSchema] = table,
        });

        Assert.Throws<DataBodyCheckerException>(() => PrimaryKeyDuplicateChecker.Check(map, logger));
    }

    [Fact]
    public void KeyHeaderMissing_ThrowsDataBodyCheckerException()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        var logger = factory.CreateLogger<ExcelColumnExtractor.Program>();

        var recordSchema = BuildRecordSchema();
        var table = BuildTable(
            headers: ["Name"],
            rows:
            [
                ["Alice"],
            ]);

        var map = new ExtractedTableMap(new Dictionary<RecordSchema, BodyColumnAggregator.ExtractedTable>
        {
            [recordSchema] = table,
        });

        Assert.Throws<DataBodyCheckerException>(() => PrimaryKeyDuplicateChecker.Check(map, logger));
    }

    [Fact]
    public void NoKeyAttribute_Skipped()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        var logger = factory.CreateLogger<ExcelColumnExtractor.Program>();

        // [Key] 없는 record
        var recordSchema = BuildRecordSchemaWithoutKey();
        var table = BuildTable(
            headers: ["Id", "Name"],
            rows:
            [
                ["1", "Alice"],
                ["1", "Alice"],
            ]);

        var map = new ExtractedTableMap(new Dictionary<RecordSchema, BodyColumnAggregator.ExtractedTable>
        {
            [recordSchema] = table,
        });

        PrimaryKeyDuplicateChecker.Check(map, logger);
    }

    private static RecordSchema BuildRecordSchema()
    {
        // language=C#
        var code = """
                   [StaticDataRecord("Item", "ItemSheet")]
                   public sealed record ItemRecord(
                       [Key] int Id,
                       string Name,
                   );
                   """;

        return LoadStaticDataRecord(code);
    }

    private static RecordSchema BuildRecordSchemaWithoutKey()
    {
        // language=C#
        var code = """
                   [StaticDataRecord("Item", "ItemSheet")]
                   public sealed record ItemRecord(
                       int Id,
                       string Name,
                   );
                   """;

        return LoadStaticDataRecord(code);
    }

    private static RecordSchema LoadStaticDataRecord(string code)
    {
        var factory = LoggerFactory.Create(b => { });
        var logger = factory.CreateLogger<PrimaryKeyDuplicateCheckerTest>();

        var loadResult = RecordSchemaLoader.OnLoad(code, logger);
        var recordSchemaSet = new RecordSchemaSet(loadResult, logger);
        var catalog = new RecordSchemaCatalog(recordSchemaSet);
        return catalog.StaticDataRecordSchemata[0];
    }

    private static BodyColumnAggregator.ExtractedTable BuildTable(
        IReadOnlyList<string> headers,
        IReadOnlyList<IReadOnlyList<string>> rows)
    {
        var extractedRows = rows
            .Select((row, rowIndex) => new BodyColumnAggregator.ExtractedRow(
                row.Select((value, colIndex) => new CellData($"{(char)('A' + colIndex)}{rowIndex + 1}", value))
                    .ToList()))
            .ToList();

        return new BodyColumnAggregator.ExtractedTable(headers, extractedRows);
    }
}
