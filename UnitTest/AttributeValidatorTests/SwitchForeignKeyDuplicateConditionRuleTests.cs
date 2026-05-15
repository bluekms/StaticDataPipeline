using Microsoft.Extensions.Logging;
using SchemaInfoScanner;
using SchemaInfoScanner.Catalogs;
using SchemaInfoScanner.Collectors;
using SchemaInfoScanner.Exceptions;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.AttributeValidatorTests;

public class SwitchForeignKeyDuplicateConditionRuleTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void DisjointConditionValuesAllowedTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<SwitchForeignKeyDuplicateConditionRuleTests>()
            is not TestOutputLogger<SwitchForeignKeyDuplicateConditionRuleTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed record MyRecord(
                       int Id,
                       [SwitchForeignKey("Id", "1", "Other", "Id")]
                       [SwitchForeignKey("Id", "2", "Foo",   "Id")]
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
    public void DuplicateConditionValueRejectedTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<SwitchForeignKeyDuplicateConditionRuleTests>()
            is not TestOutputLogger<SwitchForeignKeyDuplicateConditionRuleTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed record MyRecord(
                       int Id,
                       [SwitchForeignKey("Id", "1", "Other", "Id")]
                       [SwitchForeignKey("Id", "1", "Foo",   "Id")]
                       int Reference,
                   );
                   """;

        var loadResult = RecordSchemaLoader.OnLoad(code, logger);
        var recordSchemaSet = new RecordSchemaSet(loadResult, logger);
        var recordSchemaCatalog = new RecordSchemaCatalog(recordSchemaSet);

        var ex = Assert.Throws<InvalidAttributeUsageException>(() => RecordComplianceChecker.Check(recordSchemaCatalog, logger));
        Assert.Contains("Column=Id", ex.Message);
        Assert.Contains("Value=1", ex.Message);
        Assert.Single(logger.Logs);
    }

    [Fact]
    public void ThreeAttributesSameConditionReportedOnceTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<SwitchForeignKeyDuplicateConditionRuleTests>()
            is not TestOutputLogger<SwitchForeignKeyDuplicateConditionRuleTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed record MyRecord(
                       int Id,
                       [SwitchForeignKey("Id", "1", "Other", "Id")]
                       [SwitchForeignKey("Id", "1", "Foo",   "Id")]
                       [SwitchForeignKey("Id", "1", "Bar",   "Id")]
                       int Reference,
                   );
                   """;

        var loadResult = RecordSchemaLoader.OnLoad(code, logger);
        var recordSchemaSet = new RecordSchemaSet(loadResult, logger);
        var recordSchemaCatalog = new RecordSchemaCatalog(recordSchemaSet);

        var ex = Assert.Throws<InvalidAttributeUsageException>(() => RecordComplianceChecker.Check(recordSchemaCatalog, logger));
        Assert.Equal(1, CountOccurrences(ex.Message, "Column=Id"));
        Assert.Single(logger.Logs);
    }

    [Fact]
    public void MultipleDuplicateGroupsReportedSeparatelyTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<SwitchForeignKeyDuplicateConditionRuleTests>()
            is not TestOutputLogger<SwitchForeignKeyDuplicateConditionRuleTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed record MyRecord(
                       int Id,
                       [SwitchForeignKey("Id", "1", "OtherA", "Id")]
                       [SwitchForeignKey("Id", "1", "FooA",   "Id")]
                       [SwitchForeignKey("Id", "2", "OtherB", "Id")]
                       [SwitchForeignKey("Id", "2", "FooB",   "Id")]
                       int Reference,
                   );
                   """;

        var loadResult = RecordSchemaLoader.OnLoad(code, logger);
        var recordSchemaSet = new RecordSchemaSet(loadResult, logger);
        var recordSchemaCatalog = new RecordSchemaCatalog(recordSchemaSet);

        var ex = Assert.Throws<InvalidAttributeUsageException>(() => RecordComplianceChecker.Check(recordSchemaCatalog, logger));
        Assert.Contains("Value=1", ex.Message);
        Assert.Contains("Value=2", ex.Message);
        Assert.Single(logger.Logs);
    }

    private static int CountOccurrences(string source, string needle)
    {
        var count = 0;
        var index = 0;
        while ((index = source.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += needle.Length;
        }

        return count;
    }
}
