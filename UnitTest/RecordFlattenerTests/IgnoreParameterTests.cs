using Microsoft.Extensions.Logging;
using SchemaInfoScanner;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.RecordFlattenerTests;

public partial class IgnoreParameterTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void IgnoredParameter_IsExcludedFromHeaders()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<IgnoreParameterTests>() is not TestOutputLogger<IgnoreParameterTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed partial record MyRecord(
                       int Id,
                       [Ignore] int Cache,
                       string Name
                   );
                   """;

        var parseResult = SimpleCordParser.Parse(code, logger);

        var results = RecordFlattener.Flatten(
            parseResult.RawRecordSchemata[0],
            parseResult.RecordSchemaCatalog,
            logger);

        Assert.Equal(["Id", "Name"], results);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void IgnoredParameter_InNestedRecord_IsExcludedFromHeaders()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<IgnoreParameterTests>() is not TestOutputLogger<IgnoreParameterTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   public sealed partial record Inner(
                       int Value,
                       [Ignore] string Memo,
                       int Weight
                   );

                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed partial record MyRecord(
                       int Id,
                       Inner Inner
                   );
                   """;

        var parseResult = SimpleCordParser.Parse(code, logger);

        var results = RecordFlattener.Flatten(
            parseResult.RawRecordSchemata[0],
            parseResult.RecordSchemaCatalog,
            logger);

        Assert.Equal(["Id", "Inner.Value", "Inner.Weight"], results);
        Assert.Empty(logger.Logs);
    }
}
