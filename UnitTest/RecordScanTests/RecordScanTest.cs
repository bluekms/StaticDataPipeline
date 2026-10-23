using Microsoft.Extensions.Logging;
using SchemaInfoScanner;
using SchemaInfoScanner.Catalogs;
using SchemaInfoScanner.Collectors;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.RecordScanTests;

public partial class RecordScanTest(ITestOutputHelper testOutputHelper)
{
    private static readonly string[] RecordResourceFileNames =
    [
        "Excel1Records.cs",
        "Excel2Records.cs",
        "Excel3Records.cs",
    ];

    [Fact]
    public void LoadAndCheckTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<RecordScanTest>() is not TestOutputLogger<RecordScanTest> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        using var testData = new TestDataDirectory(RecordResourceFileNames);

        var loadResults = RecordSchemaLoader.Load(testData.Path, logger);
        var recordSchemaSet = new RecordSchemaSet(loadResults, logger);
        var enumDefinitionSet = new EnumDefinitionSet(loadResults);
        var semanticModelSet = new SemanticModelSet(loadResults);

        var recordSchemaCatalog = new RecordSchemaCatalog(recordSchemaSet);
        RecordComplianceChecker.Check(recordSchemaCatalog, logger);

        Assert.Empty(logger.Logs);
    }
}
