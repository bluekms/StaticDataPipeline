using System.Reflection;
using Microsoft.Extensions.Logging;
using SchemaInfoScanner;
using SchemaInfoScanner.Catalogs;
using SchemaInfoScanner.Collectors;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest;

public class RecordScanTest(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void LoadAndCheckTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<RecordScanTest>() is not TestOutputLogger<RecordScanTest> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        var csPath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
            "..",
            "..",
            "..",
            "..",
            "Docs",
            "SampleRecord");

        var loadResults = RecordSchemaLoader.Load(csPath, logger);
        var recordSchemaSet = new RecordSchemaSet(loadResults, logger);
        var enumDefinitionSet = new EnumDefinitionSet(loadResults);
        var semanticModelSet = new SemanticModelSet(loadResults);

        var recordSchemaCatalog = new RecordSchemaCatalog(recordSchemaSet);
        RecordComplianceChecker.Check(recordSchemaCatalog, logger);

        Assert.Empty(logger.Logs);
    }
}
