using Microsoft.Extensions.Logging;
using SchemaInfoScanner;
using SchemaInfoScanner.Catalogs;
using SchemaInfoScanner.Collectors;
using SchemaInfoScanner.Exceptions;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.AttributeValidatorTests;

public class KeyAttributeRuleTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void FrozenDictionary_ValueWithKeyAttribute_Succeeds()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<KeyAttributeRuleTests>() is not TestOutputLogger<KeyAttributeRuleTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   public sealed record ValueRecord(
                       [Key] int Id,
                       string Name,
                   );

                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed record MyRecord(
                       [Length(2)] FrozenDictionary<int, ValueRecord> Properties,
                   );
                   """;

        var loadResult = RecordSchemaLoader.OnLoad(code, logger);
        var recordSchemaSet = new RecordSchemaSet(loadResult, logger);
        var recordSchemaCatalog = new RecordSchemaCatalog(recordSchemaSet);

        RecordComplianceChecker.Check(recordSchemaCatalog, logger);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void FrozenDictionary_ValueWithoutKeyAttribute_ThrowsInvalidAttributeUsageException()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<KeyAttributeRuleTests>() is not TestOutputLogger<KeyAttributeRuleTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   public sealed record ValueRecord(
                       int Id,
                       string Name,
                   );

                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed record MyRecord(
                       [Length(2)] FrozenDictionary<int, ValueRecord> Properties,
                   );
                   """;

        var loadResult = RecordSchemaLoader.OnLoad(code, logger);
        var recordSchemaSet = new RecordSchemaSet(loadResult, logger);
        var recordSchemaCatalog = new RecordSchemaCatalog(recordSchemaSet);

        Assert.Throws<InvalidAttributeUsageException>(() => RecordComplianceChecker.Check(recordSchemaCatalog, logger));
        Assert.Single(logger.Logs);
    }

    [Fact]
    public void FrozenDictionary_ValueWithMultipleKeyAttributes_ThrowsInvalidOperationException()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<KeyAttributeRuleTests>() is not TestOutputLogger<KeyAttributeRuleTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   public sealed record ValueRecord(
                       [Key] int Id,
                       [Key] string Name,
                   );

                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed record MyRecord(
                       [Length(2)] FrozenDictionary<int, ValueRecord> Properties,
                   );
                   """;

        var loadResult = RecordSchemaLoader.OnLoad(code, logger);
        var recordSchemaSet = new RecordSchemaSet(loadResult, logger);
        var recordSchemaCatalog = new RecordSchemaCatalog(recordSchemaSet);

        Assert.Throws<InvalidOperationException>(() => RecordComplianceChecker.Check(recordSchemaCatalog, logger));
        Assert.Single(logger.Logs);
    }
}
