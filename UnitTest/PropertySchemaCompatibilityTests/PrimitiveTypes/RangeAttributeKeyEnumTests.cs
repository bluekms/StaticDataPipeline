using Microsoft.Extensions.Logging;
using SchemaInfoScanner.Collectors;
using SchemaInfoScanner.Schemata.TypedPropertySchemata;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.PropertySchemaCompatibilityTests.PrimitiveTypes;

public class RangeAttributeKeyEnumTests(ITestOutputHelper testOutputHelper)
{
    [Theory]
    [InlineData("100")]
    [InlineData("500")]
    [InlineData("1000")]
    public void KeyEnumProperty_WithUnderlyingValueInRange_Succeeds(string value)
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<RangeAttributeKeyEnumTests>() is not TestOutputLogger<RangeAttributeKeyEnumTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // [Key] enum은 IsDefined 검사가 풀려 있어 정의되지 않은 underlying 정수도 cell value로 허용.
        // Range는 underlying integer 비교.
        // language=C#
        var code = """
                   public enum ItemId { MinId = 100, MaxId = 1000 }

                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed record MyRecord(
                       [Key]
                       [Range(typeof(ItemId), "100", "1000")]
                       ItemId Property,
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
    [InlineData("99")]
    [InlineData("1001")]
    public void KeyEnumProperty_WithUnderlyingValueOutOfRange_ThrowsArgumentOutOfRangeException(string value)
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<RangeAttributeKeyEnumTests>() is not TestOutputLogger<RangeAttributeKeyEnumTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   public enum ItemId { MinId = 100, MaxId = 1000 }

                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed record MyRecord(
                       [Key]
                       [Range(typeof(ItemId), "100", "1000")]
                       ItemId Property,
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
}
