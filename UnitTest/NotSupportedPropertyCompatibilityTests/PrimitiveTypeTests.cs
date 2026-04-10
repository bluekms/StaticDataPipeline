using Microsoft.Extensions.Logging;
using SchemaInfoScanner;
using SchemaInfoScanner.Catalogs;
using SchemaInfoScanner.Collectors;
using SchemaInfoScanner.Schemata.TypedPropertySchemata;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.NotSupportedPropertyCompatibilityTests;

public class PrimitiveTypeTests(ITestOutputHelper testOutputHelper)
{
    [Theory]
    [InlineData("bool", "0")]
    [InlineData("bool", "1")]
    [InlineData("bool", "참")]
    [InlineData("bool", "거짓")]
    public void BooleanTest(string type, string argument)
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<PrimitiveTypeTests>() is not TestOutputLogger<PrimitiveTypeTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = $"""
                     [StaticDataRecord("Test", "TestSheet")]
                     public sealed record MyRecord(
                         {type} Property,
                     );
                     """;

        var catalogs = CreateCatalogs(code, logger);
        foreach (var recordSchema in catalogs.RecordSchemaCatalog.StaticDataRecordSchemata)
        {
            foreach (var propertySchema in recordSchema.PropertySchemata)
            {
                var cells = new[]
                {
                    new CellData("A1", argument)
                };

                var context = CompatibilityContext.CreateNoCollect(catalogs, cells);

                var ex = Assert.Throws<InvalidOperationException>(() => propertySchema.CheckCompatibility(context));
                logger.LogError(ex, ex.Message);
            }
        }

        Assert.Single(logger.Logs);
    }

    [Theory]
    [InlineData("byte", "ff")]
    [InlineData("byte", "0xff")]
    [InlineData("byte", "FF")]
    [InlineData("byte", "0xFF")]
    public void ByteTest(string type, string argument)
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<PrimitiveTypeTests>() is not TestOutputLogger<PrimitiveTypeTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = $"""
                     [StaticDataRecord("Test", "TestSheet")]
                     public sealed record MyRecord(
                         {type} Property,
                     );
                     """;

        var catalogs = CreateCatalogs(code, logger);
        foreach (var recordSchema in catalogs.RecordSchemaCatalog.StaticDataRecordSchemata)
        {
            foreach (var propertySchema in recordSchema.PropertySchemata)
            {
                var cells = new[]
                {
                    new CellData("A1", argument)
                };

                var context = CompatibilityContext.CreateNoCollect(catalogs, cells);

                var ex = Assert.Throws<InvalidOperationException>(() => propertySchema.CheckCompatibility(context));
                logger.LogError(ex, ex.Message);
            }
        }

        Assert.Single(logger.Logs);
    }

    private static MetadataCatalogs CreateCatalogs(string code, ILogger logger)
    {
        var loadResult = RecordSchemaLoader.OnLoad(code, logger);
        var recordSchemaSet = new RecordSchemaSet(loadResult, logger);
        var recordSchemaCatalog = new RecordSchemaCatalog(recordSchemaSet);
        var enumMemberCatalog = new EnumMemberCatalog(loadResult);
        RecordComplianceChecker.Check(recordSchemaCatalog, logger);

        return new(recordSchemaCatalog, enumMemberCatalog);
    }
}
