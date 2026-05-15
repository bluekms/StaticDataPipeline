using Microsoft.Extensions.Logging;
using SchemaInfoScanner.Collectors;
using SchemaInfoScanner.Schemata.TypedPropertySchemata;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.PropertySchemaCompatibilityTests.PrimitiveTypes;

public class RangeAttributeEnumTests(ITestOutputHelper testOutputHelper)
{
    [Theory]
    [InlineData("Low")]
    [InlineData("Mid")]
    [InlineData("High")]
    public void EnumProperty_WithValueInRange_Succeeds(string value)
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<RangeAttributeEnumTests>() is not TestOutputLogger<RangeAttributeEnumTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   public enum Tier { Lowest, Low, Mid, High, Highest }

                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed record MyRecord(
                       [Range(typeof(Tier), "Low", "High")]
                       Tier Property,
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
    [InlineData("Lowest")]
    [InlineData("Highest")]
    public void EnumProperty_WithValueOutOfRange_ThrowsArgumentOutOfRangeException(string value)
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<RangeAttributeEnumTests>() is not TestOutputLogger<RangeAttributeEnumTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   public enum Tier { Lowest, Low, Mid, High, Highest }

                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed record MyRecord(
                       [Range(typeof(Tier), "Low", "High")]
                       Tier Property,
                   );
                   """;

        var catalogs = RangeAttributeTestHelpers.CreateCatalogs(code, logger);
        var cells = new[] { new CellData("A1", value) };
        var context = CompatibilityContext.CreateNoCollect(catalogs, cells);

        foreach (var recordSchema in catalogs.RecordSchemaCatalog.StaticDataRecordSchemata)
        {
            foreach (var propertySchema in recordSchema.PropertySchemata)
            {
                var ex = Assert.Throws<ArgumentOutOfRangeException>(() => propertySchema.CheckCompatibility(context));
                logger.LogError(ex, ex.Message);
            }
        }

        Assert.Single(logger.Logs);
    }

    [Fact]
    public void NullableEnumProperty_WithNullStringMatch_SkipsRange()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<RangeAttributeEnumTests>() is not TestOutputLogger<RangeAttributeEnumTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   public enum Tier { Lowest, Low, Mid, High, Highest }

                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed record MyRecord(
                       [NullString("-")]
                       [Range(typeof(Tier), "Low", "High")]
                       Tier? Property,
                   );
                   """;

        var catalogs = RangeAttributeTestHelpers.CreateCatalogs(code, logger);
        var cells = new[] { new CellData("A1", "-") };
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

    [Fact]
    public void NullableEnumProperty_WithValueOutOfRange_ThrowsThroughInnerSchema()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<RangeAttributeEnumTests>() is not TestOutputLogger<RangeAttributeEnumTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   public enum Tier { Lowest, Low, Mid, High, Highest }

                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed record MyRecord(
                       [NullString("-")]
                       [Range(typeof(Tier), "Low", "High")]
                       Tier? Property,
                   );
                   """;

        var catalogs = RangeAttributeTestHelpers.CreateCatalogs(code, logger);
        var cells = new[] { new CellData("A1", "Highest") };
        var context = CompatibilityContext.CreateNoCollect(catalogs, cells);

        foreach (var recordSchema in catalogs.RecordSchemaCatalog.StaticDataRecordSchemata)
        {
            foreach (var propertySchema in recordSchema.PropertySchemata)
            {
                var ex = Assert.Throws<ArgumentOutOfRangeException>(() => propertySchema.CheckCompatibility(context));
                logger.LogError(ex, ex.Message);
            }
        }

        Assert.Single(logger.Logs);
    }
}
