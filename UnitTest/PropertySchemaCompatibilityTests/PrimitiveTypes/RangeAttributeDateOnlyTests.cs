using Microsoft.Extensions.Logging;
using SchemaInfoScanner.Schemata.TypedPropertySchemata;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.PropertySchemaCompatibilityTests.PrimitiveTypes;

public partial class RangeAttributeDateOnlyTests(ITestOutputHelper testOutputHelper)
{
    [Theory]
    [InlineData("2020-01-01")]
    [InlineData("2020-06-15")]
    [InlineData("2020-12-31")]
    public void DateOnlyProperty_WithValueInRange_Succeeds(string value)
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<RangeAttributeDateOnlyTests>() is not TestOutputLogger<RangeAttributeDateOnlyTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed partial record MyRecord(
                       [DateTimeFormat("yyyy-MM-dd")]
                       [Range(typeof(DateOnly), "2020-01-01", "2020-12-31")]
                       DateOnly Property,
                   );
                   """;

        var catalogs = RangeAttributeTestHelpers.CreateCatalogs(code, logger);
        var cells = new[] { new CellData("A1", value) };
        var context = CompatibilityContext.CreateNoCollect(catalogs, cells);

        foreach (var recordSchema in catalogs.RecordSchemaCatalog.StaticDataRecordSchemata)
        {
            foreach (var propertySchema in recordSchema.PropertySchemata)
            {
                propertySchema.CheckCompatibility(context);
            }
        }

        Assert.Empty(logger.Logs);
    }

    [Theory]
    [InlineData("2019-12-31")]
    [InlineData("2021-01-01")]
    public void DateOnlyProperty_WithValueOutOfRange_ThrowsArgumentOutOfRangeException(string value)
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<RangeAttributeDateOnlyTests>() is not TestOutputLogger<RangeAttributeDateOnlyTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed partial record MyRecord(
                       [DateTimeFormat("yyyy-MM-dd")]
                       [Range(typeof(DateOnly), "2020-01-01", "2020-12-31")]
                       DateOnly Property,
                   );
                   """;

        var catalogs = RangeAttributeTestHelpers.CreateCatalogs(code, logger);
        var cells = new[] { new CellData("A1", value) };
        var context = CompatibilityContext.CreateNoCollect(catalogs, cells);

        foreach (var recordSchema in catalogs.RecordSchemaCatalog.StaticDataRecordSchemata)
        {
            foreach (var propertySchema in recordSchema.PropertySchemata)
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => propertySchema.CheckCompatibility(context));
            }
        }

        Assert.Empty(logger.Logs);
    }
}
