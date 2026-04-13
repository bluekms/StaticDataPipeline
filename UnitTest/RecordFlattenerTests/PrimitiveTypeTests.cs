using Microsoft.Extensions.Logging;
using SchemaInfoScanner;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.RecordFlattenerTests;

public class PrimitiveTypeTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void SinglePrimitiveTypeTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<PrimitiveTypeTests>() is not TestOutputLogger<PrimitiveTypeTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
            [StaticDataRecord("Test", "TestSheet")]
            public sealed record MyRecord(
                int Id
            );
            """;

        var parseResult = SimpleCordParser.Parse(code, logger);

        var results = RecordFlattener.Flatten(
            parseResult.RawRecordSchemata[0],
            parseResult.RecordSchemaCatalog,
            logger);

        Assert.Single(results);
        Assert.Equal("Id", results[0]);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void MultiplePrimitiveTypesTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<PrimitiveTypeTests>() is not TestOutputLogger<PrimitiveTypeTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
            [StaticDataRecord("Test", "TestSheet")]
            public sealed record MyRecord(
                int Id,
                string Name,
                double Score,
                bool IsActive
            );
            """;

        var parseResult = SimpleCordParser.Parse(code, logger);

        var results = RecordFlattener.Flatten(
            parseResult.RawRecordSchemata[0],
            parseResult.RecordSchemaCatalog,
            logger);

        Assert.Equal(4, results.Count);
        Assert.Equal("Id", results[0]);
        Assert.Equal("Name", results[1]);
        Assert.Equal("Score", results[2]);
        Assert.Equal("IsActive", results[3]);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void PrimitiveTypeWithColumnNameAttributeTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<PrimitiveTypeTests>() is not TestOutputLogger<PrimitiveTypeTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
            [StaticDataRecord("Test", "TestSheet")]
            public sealed record MyRecord(
                [ColumnName("UserId")] int Id,
                [ColumnName("UserName")] string Name
            );
            """;

        var parseResult = SimpleCordParser.Parse(code, logger);

        var results = RecordFlattener.Flatten(
            parseResult.RawRecordSchemata[0],
            parseResult.RecordSchemaCatalog,
            logger);

        Assert.Equal(2, results.Count);
        Assert.Equal("UserId", results[0]);
        Assert.Equal("UserName", results[1]);
        Assert.Empty(logger.Logs);
    }
}
