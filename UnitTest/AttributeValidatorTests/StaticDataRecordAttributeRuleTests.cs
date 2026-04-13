using Microsoft.Extensions.Logging;
using SchemaInfoScanner;
using SchemaInfoScanner.Catalogs;
using SchemaInfoScanner.Collectors;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.AttributeValidatorTests;

public class StaticDataRecordAttributeRuleTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void RecordWithAttribute_IsIncludedInStaticDataSchemata()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<StaticDataRecordAttributeRuleTests>() is not TestOutputLogger<StaticDataRecordAttributeRuleTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed record MyRecord(
                       int Id,
                       string Name,
                   );
                   """;

        var loadResult = RecordSchemaLoader.OnLoad(code, logger);
        var recordSchemaSet = new RecordSchemaSet(loadResult, logger);
        var recordSchemaCatalog = new RecordSchemaCatalog(recordSchemaSet);

        RecordComplianceChecker.Check(recordSchemaCatalog, logger);
        Assert.Single(recordSchemaCatalog.StaticDataRecordSchemata);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void RecordWithoutAttribute_IsNotIncludedInStaticDataSchemata()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<StaticDataRecordAttributeRuleTests>() is not TestOutputLogger<StaticDataRecordAttributeRuleTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed record MyRecord(
                       int Id,
                       SubRecord Sub,
                   );

                   public sealed record SubRecord(
                       int Value,
                   );
                   """;

        var loadResult = RecordSchemaLoader.OnLoad(code, logger);
        var recordSchemaSet = new RecordSchemaSet(loadResult, logger);
        var recordSchemaCatalog = new RecordSchemaCatalog(recordSchemaSet);

        RecordComplianceChecker.Check(recordSchemaCatalog, logger);
        Assert.Single(recordSchemaCatalog.StaticDataRecordSchemata);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void NoStaticDataRecord_ComplianceCheckThrowsInvalidOperationException()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<StaticDataRecordAttributeRuleTests>() is not TestOutputLogger<StaticDataRecordAttributeRuleTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   public sealed record MyRecord(
                       int Id,
                       string Name,
                   );
                   """;

        var loadResult = RecordSchemaLoader.OnLoad(code, logger);
        var recordSchemaSet = new RecordSchemaSet(loadResult, logger);
        var recordSchemaCatalog = new RecordSchemaCatalog(recordSchemaSet);

        Assert.Throws<InvalidOperationException>(() => RecordComplianceChecker.Check(recordSchemaCatalog, logger));
        Assert.Empty(recordSchemaCatalog.StaticDataRecordSchemata);
        Assert.Empty(logger.Logs);
    }
}
