using Microsoft.Extensions.Logging;
using SchemaInfoScanner;
using SchemaInfoScanner.Catalogs;
using SchemaInfoScanner.Collectors;
using SchemaInfoScanner.Schemata.TypedPropertySchemata;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.PropertySchemaCompatibilityTests.CollectionPropertySchemaTests;

public class SetTypeTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void PrimitiveSetTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<SetTypeTests>() is not TestOutputLogger<SetTypeTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                     [StaticDataRecord("Test", "TestSheet")]
                     public sealed record MyRecord(
                         [Length(4)]
                         FrozenSet<int> Property,
                     );
                     """;

        var catalogs = CreateCatalogs(code, logger);

        var data = new[]
        {
            new CellData("A1", "1"),
            new CellData("A2", "42"),
            new CellData("A3", "0"),
            new CellData("A4", "-7")
        };

        var context = CompatibilityContext.CreateCollectKey(catalogs, data);

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
    public void PrimitiveSetDuplicationFailTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<SetTypeTests>() is not TestOutputLogger<SetTypeTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                     [StaticDataRecord("Test", "TestSheet")]
                     public sealed record MyRecord(
                         [Length(4)]
                         FrozenSet<int> Property,
                     );
                     """;

        var catalogs = CreateCatalogs(code, logger);

        var cells = new[]
        {
            new CellData("A1", "1"),
            new CellData("A2", "0"),
            new CellData("A3", "-7"),
            new CellData("A4", "-7")
        };

        var context = CompatibilityContext.CreateCollectKey(catalogs, cells);

        foreach (var recordSchema in catalogs.RecordSchemaCatalog.StaticDataRecordSchemata)
        {
            foreach (var propertySchema in recordSchema.PropertySchemata)
            {
                var ex = Assert.Throws<InvalidOperationException>(() => propertySchema.CheckCompatibility(context));
                logger.LogError(ex, ex.Message);
            }
        }

        Assert.Single(logger.Logs);
    }

    [Fact]
    public void EnumSetTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<SetTypeTests>() is not TestOutputLogger<SetTypeTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                     public enum MyEnum { A, a, C }

                     [StaticDataRecord("Test", "TestSheet")]
                     public sealed record MyRecord(
                         [Length(2)]
                         FrozenSet<MyEnum> Property,
                     );
                     """;

        var catalogs = CreateCatalogs(code, logger);

        var cells = new[]
        {
            new CellData("A1", "a"),
            new CellData("A2", "A")
        };

        var context = CompatibilityContext.CreateCollectKey(catalogs, cells, 0);

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
    public void EnumSetDuplicationFailTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<SetTypeTests>() is not TestOutputLogger<SetTypeTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                     public enum MyEnum { A, B, C }

                     [StaticDataRecord("Test", "TestSheet")]
                     public sealed record MyRecord(
                         [Length(3)]
                         FrozenSet<MyEnum> Property,
                     );
                     """;

        var catalogs = CreateCatalogs(code, logger);

        var cells = new[]
        {
            new CellData("A1", "C"),
            new CellData("A2", "A"),
            new CellData("A3", "A")
        };

        var context = CompatibilityContext.CreateCollectKey(catalogs, cells);

        foreach (var recordSchema in catalogs.RecordSchemaCatalog.StaticDataRecordSchemata)
        {
            foreach (var propertySchema in recordSchema.PropertySchemata)
            {
                var ex = Assert.Throws<InvalidOperationException>(() => propertySchema.CheckCompatibility(context));
                logger.LogError(ex, ex.Message);
            }
        }

        Assert.Single(logger.Logs);
    }

    [Fact]
    public void DateTimeSetTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<SetTypeTests>() is not TestOutputLogger<SetTypeTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                     [StaticDataRecord("Test", "TestSheet")]
                     public sealed record MyRecord(
                         [DateTimeFormat("yyyy-MM-dd HH:mm:ss.fff")]
                         [Length(2)]
                         FrozenSet<DateTime> Property,
                     );
                     """;

        var catalogs = CreateCatalogs(code, logger);

        var cells = new[]
        {
            new CellData("A1", "1986-05-26 01:05:00.000"),
            new CellData("A2", "1993-12-28 01:05:00.000")
        };

        var context = CompatibilityContext.CreateCollectKey(catalogs, cells);

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
    public void TimeSpanSetTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<SetTypeTests>() is not TestOutputLogger<SetTypeTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                     [StaticDataRecord("Test", "TestSheet")]
                     public sealed record MyRecord(
                         [TimeSpanFormat("c")]
                         [Length(2)]
                         FrozenSet<TimeSpan> Property,
                     );
                     """;

        var catalogs = CreateCatalogs(code, logger);

        var cells = new[]
        {
            new CellData("A1", "1.02:03:04.5670000"),
            new CellData("A2", "2.02:03:04.5670000")
        };

        var context = CompatibilityContext.CreateCollectKey(catalogs, cells);

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
    public void SingleColumnSetTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<SetTypeTests>() is not TestOutputLogger<SetTypeTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                     [StaticDataRecord("Test", "TestSheet")]
                     public sealed record MyRecord(
                         [SingleColumnCollection(", ")]
                         FrozenSet<int> Property,
                     );
                     """;

        var catalogs = CreateCatalogs(code, logger);

        var cells = new[]
        {
            new CellData("A1", "1, 42, 0, -7")
        };

        var context = CompatibilityContext.CreateCollectKey(catalogs, cells);

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
    public void SingleColumnWithLengthSetTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<SetTypeTests>() is not TestOutputLogger<SetTypeTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                     [StaticDataRecord("Test", "TestSheet")]
                     public sealed record MyRecord(
                         [SingleColumnCollection(", ")]
                         [Length(4)]
                         FrozenSet<int> Property,
                     );
                     """;

        var catalogs = CreateCatalogs(code, logger);

        var cells = new[]
        {
            new CellData("A1", "1, 42, 0, -7")
        };

        var context = CompatibilityContext.CreateCollectKey(catalogs, cells);

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
    public void SingleColumnSetDuplicationFailTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<SetTypeTests>() is not TestOutputLogger<SetTypeTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                     [StaticDataRecord("Test", "TestSheet")]
                     public sealed record MyRecord(
                         [SingleColumnCollection(", ")]
                         FrozenSet<int> Property,
                     );
                     """;

        var catalogs = CreateCatalogs(code, logger);

        var cells = new[]
        {
            new CellData("A1", "-7, 42, 0, -7")
        };

        var context = CompatibilityContext.CreateCollectKey(catalogs, cells);

        foreach (var recordSchema in catalogs.RecordSchemaCatalog.StaticDataRecordSchemata)
        {
            foreach (var propertySchema in recordSchema.PropertySchemata)
            {
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
