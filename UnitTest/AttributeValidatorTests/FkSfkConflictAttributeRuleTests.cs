using Microsoft.Extensions.Logging;
using SchemaInfoScanner;
using SchemaInfoScanner.Catalogs;
using SchemaInfoScanner.Collectors;
using SchemaInfoScanner.Exceptions;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.AttributeValidatorTests;

public class FkSfkConflictAttributeRuleTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void CanUseForeignKeyOnlyTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<FkSfkConflictAttributeRuleTests>() is not TestOutputLogger<FkSfkConflictAttributeRuleTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed record MyRecord(
                       int Id,
                       [ForeignKey("Other", "Id")]
                       int Reference,
                   );
                   """;

        var loadResult = RecordSchemaLoader.OnLoad(code, logger);
        var recordSchemaSet = new RecordSchemaSet(loadResult, logger);
        var recordSchemaCatalog = new RecordSchemaCatalog(recordSchemaSet);

        RecordComplianceChecker.Check(recordSchemaCatalog, logger);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void CanUseSwitchForeignKeyOnlyTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<FkSfkConflictAttributeRuleTests>() is not TestOutputLogger<FkSfkConflictAttributeRuleTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed record MyRecord(
                       int Id,
                       [SwitchForeignKey("Id", "1", "Other", "Id")]
                       int Reference,
                   );
                   """;

        var loadResult = RecordSchemaLoader.OnLoad(code, logger);
        var recordSchemaSet = new RecordSchemaSet(loadResult, logger);
        var recordSchemaCatalog = new RecordSchemaCatalog(recordSchemaSet);

        RecordComplianceChecker.Check(recordSchemaCatalog, logger);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void DisallowTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<FkSfkConflictAttributeRuleTests>() is not TestOutputLogger<FkSfkConflictAttributeRuleTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed record MyRecord(
                       int Id,
                       [ForeignKey("Other", "Id")]
                       [SwitchForeignKey("Id", "1", "Other", "Id")]
                       int Reference,
                   );
                   """;

        var loadResult = RecordSchemaLoader.OnLoad(code, logger);
        var recordSchemaSet = new RecordSchemaSet(loadResult, logger);
        var recordSchemaCatalog = new RecordSchemaCatalog(recordSchemaSet);

        Assert.Throws<InvalidAttributeUsageException>(() => RecordComplianceChecker.Check(recordSchemaCatalog, logger));
        Assert.Single(logger.Logs);
    }
}
