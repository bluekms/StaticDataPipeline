using Microsoft.Extensions.Logging;
using SchemaInfoScanner;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.RecordFlattenerTests;

public partial class DynamicLengthTests(ITestOutputHelper testOutputHelper)
{
    // language=C#
    private const string PrimitiveArrayCode = """
        [StaticDataRecord("Test", "TestSheet")]
        public sealed partial record MyRecord(
            int Id,
            ImmutableArray<int> Scores,
            string Name
        );
        """;

    // language=C#
    private const string RecordArrayCode = """
        public sealed partial record Point(int X, int Y);

        [StaticDataRecord("Test", "TestSheet")]
        public sealed partial record MyRecord(
            int Id,
            ImmutableArray<Point> Points
        );
        """;

    [Fact]
    public void WithoutSheet_EmitsFirstGroupOnly()
    {
        var logger = CreateLogger();
        var parseResult = SimpleCordParser.Parse(PrimitiveArrayCode, logger);

        var results = RecordFlattener.Flatten(parseResult.RawRecordSchemata[0], parseResult.RecordSchemaCatalog, logger);

        Assert.Equal(["Id", "Scores[0]", "Name"], results);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void WithSheet_CountsContiguousIndexedHeaders()
    {
        var logger = CreateLogger();
        var parseResult = SimpleCordParser.Parse(PrimitiveArrayCode, logger);
        string[] sheetHeaders = ["Name", "Scores[1]", "Scores[0]", "Id", "Scores[2]"];

        var results = RecordFlattener.Flatten(parseResult.RawRecordSchemata[0], parseResult.RecordSchemaCatalog, logger, sheetHeaders);

        Assert.Equal(["Id", "Scores[0]", "Scores[1]", "Scores[2]", "Name"], results);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void WithSheet_NoIndexedHeader_IsZeroLength()
    {
        var logger = CreateLogger();
        var parseResult = SimpleCordParser.Parse(PrimitiveArrayCode, logger);
        string[] sheetHeaders = ["Id", "Name"];

        var results = RecordFlattener.Flatten(parseResult.RawRecordSchemata[0], parseResult.RecordSchemaCatalog, logger, sheetHeaders);

        Assert.Equal(["Id", "Name"], results);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void WithSheet_RecordArray_CountsGroupsByInnerHeaders()
    {
        var logger = CreateLogger();
        var parseResult = SimpleCordParser.Parse(RecordArrayCode, logger);
        string[] sheetHeaders = ["Id", "Points[0].X", "Points[0].Y", "Points[1].X", "Points[1].Y"];

        var results = RecordFlattener.Flatten(parseResult.RawRecordSchemata[0], parseResult.RecordSchemaCatalog, logger, sheetHeaders);

        Assert.Equal(["Id", "Points[0].X", "Points[0].Y", "Points[1].X", "Points[1].Y"], results);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void DeclaredLength_IgnoresExtraSheetColumns()
    {
        var logger = CreateLogger();

        // language=C#
        const string code = """
            [StaticDataRecord("Test", "TestSheet")]
            public sealed partial record MyRecord([Length(2)] ImmutableArray<int> Scores);
            """;
        var parseResult = SimpleCordParser.Parse(code, logger);
        string[] sheetHeaders = ["Scores[0]", "Scores[1]", "Scores[2]"];

        var results = RecordFlattener.Flatten(parseResult.RawRecordSchemata[0], parseResult.RecordSchemaCatalog, logger, sheetHeaders);

        Assert.Equal(["Scores[0]", "Scores[1]"], results);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void WithSheet_SameNamedNestedCollection_CountsByPath()
    {
        var logger = CreateLogger();

        // language=C#
        const string code = """
            public sealed partial record Inner(ImmutableArray<int> Items);

            [StaticDataRecord("Test", "TestSheet")]
            public sealed partial record Outer(int Id, ImmutableArray<Inner> Items);
            """;
        var parseResult = SimpleCordParser.Parse(code, logger);
        string[] sheetHeaders =
        [
            "Id",
            "Items[0].Items[0]", "Items[0].Items[1]",
            "Items[1].Items[0]", "Items[1].Items[1]",
            "Items[2].Items[0]", "Items[2].Items[1]",
        ];

        var results = RecordFlattener.Flatten(parseResult.RawRecordSchemata[0], parseResult.RecordSchemaCatalog, logger, sheetHeaders);

        Assert.Equal(sheetHeaders, results);
        Assert.Empty(logger.Logs);
    }

    [Theory]
    [InlineData("Scores[1]", "Scores[2]")]
    [InlineData("Scores[0]", "Scores[2]")]
    public void WithSheet_NonContiguousIndex_Throws(string first, string second)
    {
        var logger = CreateLogger();
        var parseResult = SimpleCordParser.Parse(PrimitiveArrayCode, logger);
        string[] sheetHeaders = ["Id", first, second, "Name"];

        var ex = Assert.Throws<InvalidOperationException>(
            () => RecordFlattener.Flatten(parseResult.RawRecordSchemata[0], parseResult.RecordSchemaCatalog, logger, sheetHeaders));

        Assert.Contains("Scores", ex.Message, StringComparison.Ordinal);
        Assert.Empty(logger.Logs);
    }

    private TestOutputLogger<DynamicLengthTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<DynamicLengthTests>() is not TestOutputLogger<DynamicLengthTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
