using System.Globalization;
using Microsoft.Extensions.Logging;
using SchemaInfoScanner;
using SchemaInfoScanner.Catalogs;
using SchemaInfoScanner.Collectors;
using SchemaInfoScanner.Schemata.TypedPropertySchemata;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.PropertySchemaCompatibilityTests.PrimitiveTypes;

public class ErrorMessageTests(ITestOutputHelper testOutputHelper)
{
    [Theory]
    [InlineData("bool", "invalid_value")]
    [InlineData("byte", "invalid_value")]
    [InlineData("char", "invalid_value")]
    [InlineData("decimal", "invalid_value")]
    [InlineData("double", "invalid_value")]
    [InlineData("float", "invalid_value")]
    [InlineData("int", "invalid_value")]
    [InlineData("long", "invalid_value")]
    [InlineData("sbyte", "invalid_value")]
    [InlineData("short", "invalid_value")]
    [InlineData("uint", "invalid_value")]
    [InlineData("ulong", "invalid_value")]
    [InlineData("ushort", "invalid_value")]
    public void PrimitiveTest(string type, string argument)
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<ErrorMessageTests>() is not TestOutputLogger<ErrorMessageTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        var code = $$"""
                     [StaticDataRecord("Test", "TestSheet")]
                     public sealed record MyRecord(
                         {{type}} Property,
                     );
                     """;

        var catalogs = CreateCatalogs(code, logger);

        var cells = new[]
        {
            new CellData("A1", argument)
        };

        var context = CompatibilityContext.CreateNoCollect(catalogs, cells);

        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            foreach (var recordSchema in catalogs.RecordSchemaCatalog.StaticDataRecordSchemata)
            {
                foreach (var propertySchema in recordSchema.PropertySchemata)
                {
                    propertySchema.CheckCompatibility(context);
                }
            }
        });

        logger.LogError(ex, ex.Message);
        Assert.StartsWith($"Invalid value 'invalid_value' in cell A1.", ex.Message);
        Assert.Single(logger.Logs);
    }

    [Theory]
    [InlineData("string", "invalid_value")]
    public void StringRegexTest(string type, string argument)
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<ErrorMessageTests>() is not TestOutputLogger<ErrorMessageTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        var code = $$"""
                     [StaticDataRecord("Test", "TestSheet")]
                     public sealed record MyRecord(
                         [RegularExpression("^[A-Z]{3}$")]
                         {{type}} Property,
                     );
                     """;

        var catalogs = CreateCatalogs(code, logger);

        var cells = new[]
        {
            new CellData("A1", argument)
        };

        var context = CompatibilityContext.CreateNoCollect(catalogs, cells);

        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            foreach (var recordSchema in catalogs.RecordSchemaCatalog.StaticDataRecordSchemata)
            {
                foreach (var propertySchema in recordSchema.PropertySchemata)
                {
                    propertySchema.CheckCompatibility(context);
                }
            }
        });

        logger.LogError(ex, ex.Message);
        Assert.StartsWith($"Invalid value 'invalid_value' in cell A1.", ex.Message);
        Assert.Single(logger.Logs);
    }

    [Theory]
    [InlineData("MyEnum", "invalid_value")]
    public void EnumTest(string type, string argument)
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<ErrorMessageTests>() is not TestOutputLogger<ErrorMessageTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        var code = $$"""
                     public enum MyEnum { A, B, C }

                     [StaticDataRecord("Test", "TestSheet")]
                     public sealed record MyRecord(
                         {{type}} Property,
                     );
                     """;

        var catalogs = CreateCatalogs(code, logger);

        var cells = new[]
        {
            new CellData("A1", argument)
        };

        var context = CompatibilityContext.CreateNoCollect(catalogs, cells);

        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            foreach (var recordSchema in catalogs.RecordSchemaCatalog.StaticDataRecordSchemata)
            {
                foreach (var propertySchema in recordSchema.PropertySchemata)
                {
                    propertySchema.CheckCompatibility(context);
                }
            }
        });

        logger.LogError(ex, ex.Message);
        Assert.StartsWith($"Invalid value 'invalid_value' in cell A1.", ex.Message);
        Assert.Single(logger.Logs);
    }

    [Theory]
    [InlineData("DateTime", "invalid_value")]
    public void DateTimeTest(string type, string argument)
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<ErrorMessageTests>() is not TestOutputLogger<ErrorMessageTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        var code = $$"""
                     [StaticDataRecord("Test", "TestSheet")]
                     public sealed record MyRecord(
                         [DateTimeFormat("yyyy-MM-dd HH:mm:ss.fff")]
                         {{type}} Property,
                     );
                     """;

        var catalogs = CreateCatalogs(code, logger);

        var cells = new[]
        {
            new CellData("A1", argument)
        };

        var context = CompatibilityContext.CreateNoCollect(catalogs, cells);

        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            foreach (var recordSchema in catalogs.RecordSchemaCatalog.StaticDataRecordSchemata)
            {
                foreach (var propertySchema in recordSchema.PropertySchemata)
                {
                    propertySchema.CheckCompatibility(context);
                }
            }
        });

        logger.LogError(ex, ex.Message);
        Assert.StartsWith($"Invalid value 'invalid_value' in cell A1.", ex.Message);
        Assert.Single(logger.Logs);
    }

    [Theory]
    [InlineData("TimeSpan", "invalid_value")]
    public void TimeSpanTest(string type, string argument)
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<ErrorMessageTests>() is not TestOutputLogger<ErrorMessageTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        var code = $$"""
                     [StaticDataRecord("Test", "TestSheet")]
                     public sealed record MyRecord(
                         [TimeSpanFormat("c")]
                         {{type}} Property,
                     );
                     """;

        var catalogs = CreateCatalogs(code, logger);

        var cells = new[]
        {
            new CellData("A1", argument)
        };

        var context = CompatibilityContext.CreateNoCollect(catalogs, cells);

        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            foreach (var recordSchema in catalogs.RecordSchemaCatalog.StaticDataRecordSchemata)
            {
                foreach (var propertySchema in recordSchema.PropertySchemata)
                {
                    propertySchema.CheckCompatibility(context);
                }
            }
        });

        logger.LogError(ex, ex.Message);
        Assert.StartsWith($"Invalid value 'invalid_value' in cell A1.", ex.Message);
        Assert.Single(logger.Logs);
    }

    private static MetadataCatalogs CreateCatalogs(string code, ILogger logger)
    {
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

        var loadResult = RecordSchemaLoader.OnLoad(code, logger);
        var recordSchemaSet = new RecordSchemaSet(loadResult, logger);
        var recordSchemaCatalog = new RecordSchemaCatalog(recordSchemaSet);
        var enumMemberCatalog = new EnumMemberCatalog(loadResult);
        RecordComplianceChecker.Check(recordSchemaCatalog, logger);

        return new(recordSchemaCatalog, enumMemberCatalog);
    }
}
