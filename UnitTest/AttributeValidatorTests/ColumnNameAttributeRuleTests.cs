using Microsoft.Extensions.Logging;
using SchemaInfoScanner;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.AttributeValidatorTests;

public class ColumnNameAttributeRuleTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void PrimitiveProperty_ColumnNameUsedAsHeader()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<ColumnNameAttributeRuleTests>() is not TestOutputLogger<ColumnNameAttributeRuleTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed record MyRecord(
                       [ColumnName("UserId")] int Id,
                       [ColumnName("UserName")] string Name,
                   );
                   """;

        var parseResult = SimpleCordParser.Parse(code, logger);
        var headers = RecordFlattener.Flatten(
            parseResult.RawRecordSchemata[0],
            parseResult.RecordSchemaCatalog,
            logger);

        Assert.Equal(2, headers.Count);
        Assert.Equal("UserId", headers[0]);
        Assert.Equal("UserName", headers[1]);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void CollectionProperty_ColumnNameUsedAsHeaderPrefix()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<ColumnNameAttributeRuleTests>() is not TestOutputLogger<ColumnNameAttributeRuleTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed record MyRecord(
                       [ColumnName("Tag")] [Length(3)] ImmutableArray<string> Items,
                   );
                   """;

        var parseResult = SimpleCordParser.Parse(code, logger);
        var headers = RecordFlattener.Flatten(
            parseResult.RawRecordSchemata[0],
            parseResult.RecordSchemaCatalog,
            logger);

        Assert.Equal(3, headers.Count);
        Assert.Equal("Tag[0]", headers[0]);
        Assert.Equal("Tag[1]", headers[1]);
        Assert.Equal("Tag[2]", headers[2]);
        Assert.Empty(logger.Logs);
    }
}
