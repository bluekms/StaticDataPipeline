using System.Globalization;
using Microsoft.Extensions.Logging;
using SchemaInfoScanner.Collectors;
using SchemaInfoScanner.Schemata.TypedPropertySchemata;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.PropertySchemaCompatibilityTests.PrimitiveTypes;

public class RangeAttributeTimeSpanTests(ITestOutputHelper testOutputHelper)
{
    [Theory]
    [InlineData("00:00:00")]
    [InlineData("00:30:00")]
    [InlineData("01:00:00")]
    public void TimeSpanProperty_WithValueInRange_Succeeds(string value)
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<RangeAttributeTimeSpanTests>() is not TestOutputLogger<RangeAttributeTimeSpanTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed record MyRecord(
                       [TimeSpanFormat("c")]
                       [Range(typeof(TimeSpan), "00:00:00", "01:00:00")]
                       TimeSpan Property,
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
    [InlineData("01:00:00.0000001")]
    [InlineData("01:00:01")]
    [InlineData("23:59:59")]
    public void TimeSpanProperty_WithValueOutOfRange_ThrowsArgumentOutOfRangeException(string value)
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<RangeAttributeTimeSpanTests>() is not TestOutputLogger<RangeAttributeTimeSpanTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed record MyRecord(
                       [TimeSpanFormat("c")]
                       [Range(typeof(TimeSpan), "00:00:00", "01:00:00")]
                       TimeSpan Property,
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
    public void TimeSpanProperty_UnderVariousCultures_BehavesIdentically(string culture)
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<RangeAttributeTimeSpanTests>() is not TestOutputLogger<RangeAttributeTimeSpanTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed record MyRecord(
                       [TimeSpanFormat("c")]
                       [Range(typeof(TimeSpan), "00:00:00", "01:00:00")]
                       TimeSpan Property,
                   );
                   """;

        var savedCulture = CultureInfo.CurrentCulture;
        var savedUiCulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);

        try
        {
            var catalogs = RangeAttributeTestHelpers.CreateCatalogs(code, logger);

            var inContext = CompatibilityContext.CreateNoCollect(catalogs, [new CellData("A1", "00:30:00")]);
            foreach (var recordSchema in catalogs.RecordSchemaCatalog.StaticDataRecordSchemata)
            {
                foreach (var propertySchema in recordSchema.PropertySchemata)
                {
                    propertySchema.CheckCompatibility(inContext);
                }
            }

            var outContext = CompatibilityContext.CreateNoCollect(catalogs, [new CellData("A1", "01:00:01")]);
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
    public void NullableTimeSpanProperty_WithNullStringMatch_SkipsRange()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<RangeAttributeTimeSpanTests>() is not TestOutputLogger<RangeAttributeTimeSpanTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed record MyRecord(
                       [TimeSpanFormat("c")]
                       [NullString("-")]
                       [Range(typeof(TimeSpan), "00:00:00", "01:00:00")]
                       TimeSpan? Property,
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
    public void NullableTimeSpanProperty_WithValueOutOfRange_ThrowsThroughInnerSchema()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<RangeAttributeTimeSpanTests>() is not TestOutputLogger<RangeAttributeTimeSpanTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed record MyRecord(
                       [TimeSpanFormat("c")]
                       [NullString("-")]
                       [Range(typeof(TimeSpan), "00:00:00", "01:00:00")]
                       TimeSpan? Property,
                   );
                   """;

        var catalogs = RangeAttributeTestHelpers.CreateCatalogs(code, logger);
        var cells = new[] { new CellData("A1", "02:00:00") };
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
