using System.Globalization;
using Microsoft.Extensions.Logging;
using SchemaInfoScanner.Collectors;
using SchemaInfoScanner.Schemata.TypedPropertySchemata;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.PropertySchemaCompatibilityTests.PrimitiveTypes;

public class RangeAttributeStringTests(ITestOutputHelper testOutputHelper)
{
    [Theory]
    [InlineData("apple")]
    [InlineData("mango")]
    [InlineData("zebra")]
    public void StringProperty_WithValueInLexicalRange_Succeeds(string value)
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<RangeAttributeStringTests>() is not TestOutputLogger<RangeAttributeStringTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed record MyRecord(
                       [Range(typeof(string), "apple", "zebra")]
                       string Property,
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
    [InlineData("ant")]
    [InlineData("zzz")]
    public void StringProperty_WithValueOutOfLexicalRange_ThrowsArgumentOutOfRangeException(string value)
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<RangeAttributeStringTests>() is not TestOutputLogger<RangeAttributeStringTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed record MyRecord(
                       [Range(typeof(string), "apple", "zebra")]
                       string Property,
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

    [Theory]
    [InlineData("en-US")]
    [InlineData("ko-KR")]
    [InlineData("fr-FR")]
    [InlineData("de-DE")]
    [InlineData("tr-TR")]
    public void StringProperty_UnderVariousCultures_BehavesIdentically(string culture)
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<RangeAttributeStringTests>() is not TestOutputLogger<RangeAttributeStringTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed record MyRecord(
                       [Range(typeof(string), "apple", "zebra")]
                       string Property,
                   );
                   """;

        var savedCulture = CultureInfo.CurrentCulture;
        var savedUiCulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);

        try
        {
            var catalogs = RangeAttributeTestHelpers.CreateCatalogs(code, logger);

            var inContext = CompatibilityContext.CreateNoCollect(catalogs, [new CellData("A1", "mango")]);
            foreach (var recordSchema in catalogs.RecordSchemaCatalog.StaticDataRecordSchemata)
            {
                foreach (var propertySchema in recordSchema.PropertySchemata)
                {
                    propertySchema.CheckCompatibility(inContext);
                }
            }

            var outContext = CompatibilityContext.CreateNoCollect(catalogs, [new CellData("A1", "zzz")]);
            foreach (var recordSchema in catalogs.RecordSchemaCatalog.StaticDataRecordSchemata)
            {
                foreach (var propertySchema in recordSchema.PropertySchemata)
                {
                    Assert.Throws<ArgumentOutOfRangeException>(() => propertySchema.CheckCompatibility(outContext));
                }
            }

            Assert.Empty(logger.Logs);
        }
        finally
        {
            CultureInfo.CurrentCulture = savedCulture;
            CultureInfo.CurrentUICulture = savedUiCulture;
        }
    }

    [Fact]
    public void NullableStringProperty_WithNullStringMatch_SkipsRange()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<RangeAttributeStringTests>() is not TestOutputLogger<RangeAttributeStringTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed record MyRecord(
                       [NullString("-")]
                       [Range(typeof(string), "apple", "zebra")]
                       string? Property,
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
}
