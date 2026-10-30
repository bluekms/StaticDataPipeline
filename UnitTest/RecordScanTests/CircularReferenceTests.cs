using Microsoft.Extensions.Logging;
using SchemaInfoScanner;
using SchemaInfoScanner.Collectors;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.RecordScanTests;

public partial class CircularReferenceTests(ITestOutputHelper testOutputHelper)
{
    [Theory]
    [InlineData("public sealed partial record Node(int Value, Node Next);", "Node")]
    [InlineData("public sealed partial record A(int X, B B); public sealed partial record B(int Y, A A);", "A")]
    public void CyclicRecords_AreRejectedWithoutStackOverflow(string declarations, string childType)
    {
        var logger = CreateLogger();

        // language=C#
        var code = $$"""
                     {{declarations}}

                     [StaticDataRecord("Test", "TestSheet")]
                     public sealed partial record Root([Key] int Id, {{childType}} Child);
                     """;

        var loadResult = RecordSchemaLoader.OnLoad(code, logger);

        var ex = Assert.Throws<NotSupportedException>(() => new RecordSchemaSet(loadResult, logger));

        Assert.Single(logger.Logs);
        Assert.Contains(logger.Logs, log => log.Message == ex.Message);
    }

    private TestOutputLogger<CircularReferenceTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<CircularReferenceTests>() is not TestOutputLogger<CircularReferenceTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
