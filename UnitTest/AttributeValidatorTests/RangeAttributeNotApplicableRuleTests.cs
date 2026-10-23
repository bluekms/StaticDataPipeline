using Microsoft.Extensions.Logging;
using SchemaInfoScanner;
using SchemaInfoScanner.Catalogs;
using SchemaInfoScanner.Collectors;
using SchemaInfoScanner.Exceptions;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.AttributeValidatorTests;

public partial class RangeAttributeNotApplicableRuleTests(ITestOutputHelper testOutputHelper)
{
    [Theory]
    [InlineData("byte", "0", "100")]
    [InlineData("int", "0", "100")]
    [InlineData("long", "0", "100")]
    [InlineData("double", "0", "100")]
    [InlineData("decimal", "0", "100")]
    [InlineData("char", "\"a\"", "\"z\"")]
    [InlineData("string", "\"a\"", "\"z\"")]
    public void AllowedOnSupportedTypes(string type, string min, string max)
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<RangeAttributeNotApplicableRuleTests>() is not TestOutputLogger<RangeAttributeNotApplicableRuleTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = $$"""
                     [StaticDataRecord("Test", "TestSheet")]
                     public sealed partial record MyRecord(
                         [Range(typeof({{type}}), {{min}}, {{max}})]
                         {{type}} Property,
                     );
                     """;

        var loadResult = RecordSchemaLoader.OnLoad(code, logger);
        var recordSchemaSet = new RecordSchemaSet(loadResult, logger);
        var recordSchemaCatalog = new RecordSchemaCatalog(recordSchemaSet);

        RecordComplianceChecker.Check(recordSchemaCatalog, logger);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void RejectedOnBool()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<RangeAttributeNotApplicableRuleTests>() is not TestOutputLogger<RangeAttributeNotApplicableRuleTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed partial record MyRecord(
                       [Range(0, 1)] bool Property,
                   );
                   """;

        var loadResult = RecordSchemaLoader.OnLoad(code, logger);
        var recordSchemaSet = new RecordSchemaSet(loadResult, logger);
        var recordSchemaCatalog = new RecordSchemaCatalog(recordSchemaSet);

        Assert.Throws<InvalidAttributeUsageException>(() => RecordComplianceChecker.Check(recordSchemaCatalog, logger));
        Assert.Single(logger.Logs);
    }

    [Fact]
    public void RejectedOnNullableBool()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<RangeAttributeNotApplicableRuleTests>() is not TestOutputLogger<RangeAttributeNotApplicableRuleTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed partial record MyRecord(
                       [NullString("NULL")]
                       [Range(0, 1)] bool? Property,
                   );
                   """;

        var loadResult = RecordSchemaLoader.OnLoad(code, logger);
        var recordSchemaSet = new RecordSchemaSet(loadResult, logger);
        var recordSchemaCatalog = new RecordSchemaCatalog(recordSchemaSet);

        Assert.Throws<InvalidAttributeUsageException>(() => RecordComplianceChecker.Check(recordSchemaCatalog, logger));
        Assert.Single(logger.Logs);
    }
}
