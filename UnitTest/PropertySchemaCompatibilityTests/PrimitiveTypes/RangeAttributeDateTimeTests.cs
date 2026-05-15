using System.Globalization;
using Microsoft.Extensions.Logging;
using SchemaInfoScanner.Collectors;
using SchemaInfoScanner.Schemata.TypedPropertySchemata;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.PropertySchemaCompatibilityTests.PrimitiveTypes;

public class RangeAttributeDateTimeTests(ITestOutputHelper testOutputHelper)
{
    [Theory]
    [InlineData("2020-01-01")]
    [InlineData("2020-06-15")]
    [InlineData("2020-12-31")]
    public void DateTimeProperty_WithValueInRange_Succeeds(string value)
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<RangeAttributeDateTimeTests>() is not TestOutputLogger<RangeAttributeDateTimeTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed record MyRecord(
                       [DateTimeFormat("yyyy-MM-dd")]
                       [Range(typeof(DateTime), "2020-01-01", "2020-12-31")]
                       DateTime Property,
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
    public void DateTimeProperty_WithValueOutOfRange_ThrowsArgumentOutOfRangeException(string value)
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<RangeAttributeDateTimeTests>() is not TestOutputLogger<RangeAttributeDateTimeTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed record MyRecord(
                       [DateTimeFormat("yyyy-MM-dd")]
                       [Range(typeof(DateTime), "2020-01-01", "2020-12-31")]
                       DateTime Property,
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
    [InlineData("ja-JP")]
    public void DateTimeProperty_UnderVariousCultures_BehavesIdentically(string culture)
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<RangeAttributeDateTimeTests>() is not TestOutputLogger<RangeAttributeDateTimeTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed record MyRecord(
                       [DateTimeFormat("yyyy-MM-dd")]
                       [Range(typeof(DateTime), "2020-01-01", "2020-12-31")]
                       DateTime Property,
                   );
                   """;

        var savedCulture = CultureInfo.CurrentCulture;
        var savedUiCulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);

        try
        {
            var catalogs = RangeAttributeTestHelpers.CreateCatalogs(code, logger);

            // InRange는 어느 문화권에서도 통과해야 한다.
            var inContext = CompatibilityContext.CreateNoCollect(catalogs, [new CellData("A1", "2020-06-15")]);
            foreach (var recordSchema in catalogs.RecordSchemaCatalog.StaticDataRecordSchemata)
            {
                foreach (var propertySchema in recordSchema.PropertySchemata)
                {
                    propertySchema.CheckCompatibility(inContext);
                }
            }

            // OutOfRange는 어느 문화권에서도 ArgumentOutOfRangeException을 던져야 한다.
            var outContext = CompatibilityContext.CreateNoCollect(catalogs, [new CellData("A1", "2021-01-01")]);
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
    public void NullableDateTimeProperty_WithNullStringMatch_SkipsRange()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<RangeAttributeDateTimeTests>() is not TestOutputLogger<RangeAttributeDateTimeTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed record MyRecord(
                       [DateTimeFormat("yyyy-MM-dd")]
                       [NullString("-")]
                       [Range(typeof(DateTime), "2020-01-01", "2020-12-31")]
                       DateTime? Property,
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
    public void NullableDateTimeProperty_WithValueOutOfRange_ThrowsThroughInnerSchema()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<RangeAttributeDateTimeTests>() is not TestOutputLogger<RangeAttributeDateTimeTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed record MyRecord(
                       [DateTimeFormat("yyyy-MM-dd")]
                       [NullString("-")]
                       [Range(typeof(DateTime), "2020-01-01", "2020-12-31")]
                       DateTime? Property,
                   );
                   """;

        var catalogs = RangeAttributeTestHelpers.CreateCatalogs(code, logger);
        var cells = new[] { new CellData("A1", "2025-01-01") };
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
