using Microsoft.Extensions.Logging;
using SchemaInfoScanner;
using SchemaInfoScanner.Catalogs;
using SchemaInfoScanner.Collectors;
using SchemaInfoScanner.Schemata.TypedPropertySchemata;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.PropertySchemaCompatibilityTests.CollectionPropertySchemaTests.NullableTypes;

public class ArrayTypeTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void PrimitiveArrayTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<ArrayTypeTests>() is not TestOutputLogger<ArrayTypeTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed record MyRecord(
                       [Length(3)][NullString("-")] ImmutableArray<int?> Property,
                   );
                   """;

        var catalogs = CreateCatalogs(code, logger);

        var cells = new[]
        {
            new CellData("A1", "1"),
            new CellData("A2", "42"),
            new CellData("A3", "-"),
            new CellData("A4", "-7")
        };

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
    public void EnumArrayTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<ArrayTypeTests>() is not TestOutputLogger<ArrayTypeTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   public enum MyEnum { A, B, C }

                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed record MyRecord(
                       [Length(3)][NullString("-")] ImmutableArray<MyEnum?> Property,
                   );
                   """;

        var catalogs = CreateCatalogs(code, logger);

        var cells = new[]
        {
            new CellData("A1", "B"),
            new CellData("A2", "A"),
            new CellData("A3", "-")
        };

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
    public void DateTimeArrayTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<ArrayTypeTests>() is not TestOutputLogger<ArrayTypeTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed record MyRecord(
                       [DateTimeFormat("yyyy-MM-dd HH:mm:ss.fff")]
                       [NullString("-")]
                       [Length(3)]
                       ImmutableArray<DateTime?> Property,
                   );
                   """;

        var catalogs = CreateCatalogs(code, logger);

        var cells = new[]
        {
            new CellData("A1", "-"),
            new CellData("A2", "1986-05-26 01:05:00.000"),
            new CellData("A3", "1993-12-28 01:05:00.000")
        };

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
    public void TimeSpanArrayTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<ArrayTypeTests>() is not TestOutputLogger<ArrayTypeTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed record MyRecord(
                       [TimeSpanFormat("c")]
                       [NullString("-")]
                       [Length(2)]
                       ImmutableArray<TimeSpan?> Property,
                   );
                   """;

        var catalogs = CreateCatalogs(code, logger);

        var cells = new[]
        {
            new CellData("A1", "1.02:03:04.5670000"),
            new CellData("A2", "2.02:03:04.5670000")
        };

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
    public void SingleColumnArrayTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<ArrayTypeTests>() is not TestOutputLogger<ArrayTypeTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed record MyRecord(
                       [NullString("")]
                       [SingleColumnCollection(", ")]
                       ImmutableArray<int?> Property,
                   );
                   """;

        var catalogs = CreateCatalogs(code, logger);

        var cells = new[]
        {
            new CellData("A1", "1, 42, , -7")
        };

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
    public void SingleColumnEnumArrayTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<ArrayTypeTests>() is not TestOutputLogger<ArrayTypeTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   public enum MyEnum { A, B, C }

                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed record MyRecord(
                       [SingleColumnCollection(", ")]
                       [NullString("")]
                       ImmutableArray<MyEnum?> Property,
                   );
                   """;

        var catalogs = CreateCatalogs(code, logger);

        var cells = new[]
        {
            new CellData("A1", "C, A, ")
        };

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
    public void SingleColumnDateTimeArrayTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<ArrayTypeTests>() is not TestOutputLogger<ArrayTypeTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed record MyRecord(
                       [SingleColumnCollection(", ")]
                       [NullString("")]
                       [DateTimeFormat("yyyy-MM-dd HH:mm:ss.fff")]
                       ImmutableArray<DateTime?> Property,
                   );
                   """;

        var catalogs = CreateCatalogs(code, logger);

        var cells = new[]
        {
            new CellData("A1", ", 1986-05-26 01:05:00.000, 1993-12-28 01:05:00.000")
        };

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
    public void SingleColumnTimeSpanArrayTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<ArrayTypeTests>() is not TestOutputLogger<ArrayTypeTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed record MyRecord(
                       [SingleColumnCollection(", ")]
                       [TimeSpanFormat("c")]
                       [NullString("")]
                       ImmutableArray<TimeSpan?> Property,
                   );
                   """;

        var catalogs = CreateCatalogs(code, logger);

        var cells = new[]
        {
            new CellData("A1", "1.02:03:04.5670000, , 2.02:03:04.5670000")
        };

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
