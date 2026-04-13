using Microsoft.Extensions.Logging;
using SchemaInfoScanner;
using SchemaInfoScanner.Catalogs;
using SchemaInfoScanner.Collectors;
using SchemaInfoScanner.Schemata.TypedPropertySchemata;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.PropertySchemaCompatibilityTests.RecordTypes;

public class RecordTypeTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void InnerRecordTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<RecordTypeTests>() is not TestOutputLogger<RecordTypeTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                     [StaticDataRecord("Test", "TestSheet")]
                     public sealed record MyRecord(
                         Identifier Id
                     )
                     {
                        public record struct Identifier(int Value);
                     }
                     """;

        var catalogs = CreateCatalogs(code, logger);

        var data = new[]
        {
            new CellData("A1", "1")
        };

        var context = CompatibilityContext.CreateNoCollect(catalogs, data);

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
    public void AnotherRecordTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<RecordTypeTests>() is not TestOutputLogger<RecordTypeTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                     [StaticDataRecord("Test", "TestSheet")]
                     public sealed record MyRecord(
                         MyData Data
                     );

                     public record struct MyData(int Key, string Value);
                     """;

        var catalogs = CreateCatalogs(code, logger);

        var data = new[]
        {
            new CellData("A1", "1"),
            new CellData("A2", "AAA")
        };

        var context = CompatibilityContext.CreateNoCollect(catalogs, data);

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
    public void RecordArrayTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<RecordTypeTests>() is not TestOutputLogger<RecordTypeTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                     [StaticDataRecord("Test", "TestSheet")]
                     public sealed record MyRecord(
                         [Length(3)] ImmutableArray<MyData> Data
                     );

                     public record struct MyData(int Key, string Value);
                     """;

        var catalogs = CreateCatalogs(code, logger);

        var data = new[]
        {
            new CellData("A1", "1"),
            new CellData("A2", "AAA"),
            new CellData("A3", "2"),
            new CellData("A4", "BBB"),
            new CellData("A5", "2"),
            new CellData("A6", "BBB")
        };

        var context = CompatibilityContext.CreateNoCollect(catalogs, data);

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
    public void RecordSetTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<RecordTypeTests>() is not TestOutputLogger<RecordTypeTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                     [StaticDataRecord("Test", "TestSheet")]
                     public sealed record MyRecord(
                         [Length(3)] FrozenSet<MyData> Data
                     );

                     public record struct MyData(int Key, string Value);
                     """;

        var catalogs = CreateCatalogs(code, logger);

        var data = new[]
        {
            new CellData("A1", "1"),
            new CellData("A2", "AAA"),
            new CellData("A3", "2"),
            new CellData("A4", "BBB"),
            new CellData("A5", "3"),
            new CellData("A6", "CCC")
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
    public void RecordMapTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<RecordTypeTests>() is not TestOutputLogger<RecordTypeTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                     [StaticDataRecord("Test", "TestSheet")]
                     public sealed record MyRecord(
                         [Length(3)] FrozenDictionary<int, MyData> Data
                     );

                     public record struct MyData([Key] int Id, string Value);
                     """;

        var catalogs = CreateCatalogs(code, logger);

        var data = new[]
        {
            new CellData("A1", "1"),
            new CellData("A2", "AAA"),
            new CellData("A3", "2"),
            new CellData("A4", "BBB"),
            new CellData("A5", "3"),
            new CellData("A6", "CCC")
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
    public void RecordRecordKeyAndRecordValueMapTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<RecordTypeTests>() is not TestOutputLogger<RecordTypeTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                     [StaticDataRecord("Test", "TestSheet")]
                     public sealed record MyRecord(
                         [Length(3)] FrozenDictionary<KeyData, MyData> Data
                     );

                     public record struct KeyData(InnerKey Inner, int Key1)
                     {
                         public record struct InnerKey(int X, int Y);
                     }

                     public record struct MyData([Key] KeyData Key, string Value);
                     """;

        var catalogs = CreateCatalogs(code, logger);

        var data = new[]
        {
            // KeyData #1
            new CellData("A1", "1"),   // X
            new CellData("A2", "1"),   // Y
            new CellData("A3", "10"),  // Key1

            // MyData #1
            new CellData("A4", "AAA"),

            // KeyData #2
            new CellData("A5", "2"),   // X
            new CellData("A6", "1"),   // Y
            new CellData("A7", "20"),  // Key1

            // MyData #2
            new CellData("A8", "BBB"),

            // KeyData #3
            new CellData("A9", "3"),   // X
            new CellData("A10", "1"),  // Y
            new CellData("A11", "30"), // Key1

            // MyData #3
            new CellData("A12", "CCC"),
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
    public void RecordInnerArrayLengthTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<RecordTypeTests>() is not TestOutputLogger<RecordTypeTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                     public enum Grades
                     {
                         A,
                         B,
                         C,
                         D,
                         F,
                     }

                     [StaticDataRecord("Test", "TestSheet")]
                     public sealed record MyRecord(
                         [Length(3)] ImmutableArray<NameScore> Data
                     );

                     public record struct NameScore(string Name, [Length(2)] ImmutableArray<Grades> Grade);
                     """;

        var catalogs = CreateCatalogs(code, logger);

        var data = new[]
        {
            new CellData("A1", "Alice"),
            new CellData("B1", "A"),
            new CellData("C1", "B"),

            new CellData("A2", "Bob"),
            new CellData("B2", "C"),
            new CellData("C2", "D"),

            new CellData("A3", "Carol"),
            new CellData("B3", "A"),
            new CellData("C3", "F")
        };

        var context = CompatibilityContext.CreateNoCollect(catalogs, data);

        foreach (var recordSchema in catalogs.RecordSchemaCatalog.StaticDataRecordSchemata)
        {
            foreach (var propertySchema in recordSchema.PropertySchemata)
            {
                propertySchema.CheckCompatibility(context);
            }
        }

        Assert.Empty(logger.Logs);
    }

    // TODO 키 스코프 문제가 발생한다
    [Fact]
    public void RecordInnerSetLengthTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<RecordTypeTests>() is not TestOutputLogger<RecordTypeTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                     public enum Grades
                     {
                         A,
                         B,
                         C,
                         D,
                         F,
                     }

                     [StaticDataRecord("Test", "TestSheet")]
                     public sealed record MyRecord(
                         [Length(3)] FrozenSet<NameScore> Data
                     );

                     public record struct NameScore(string Name, [Length(2)] ImmutableArray<Grades> Grade);
                     """;

        var catalogs = CreateCatalogs(code, logger);

        var data = new[]
        {
            new CellData("A1", "Alice"),
            new CellData("B1", "A"),
            new CellData("C1", "B"),

            new CellData("A2", "Bob"),
            new CellData("B2", "C"),
            new CellData("C2", "D"),

            new CellData("A3", "Carol"),
            new CellData("B3", "A"),
            new CellData("C3", "F")
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
    public void RecordInnerMapLengthTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<RecordTypeTests>() is not TestOutputLogger<RecordTypeTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                     public enum Grades
                     {
                         A,
                         B,
                         C,
                         D,
                         F,
                     }

                     [StaticDataRecord("Test", "TestSheet")]
                     public sealed record MyRecord(
                         [Length(3)] FrozenDictionary<string, NameScore> Data
                     );

                     public record struct NameScore([Key] string Name, [Length(2)] ImmutableArray<Grades> Grade);
                     """;

        var catalogs = CreateCatalogs(code, logger);

        var data = new[]
        {
            new CellData("A1", "Alice"),
            new CellData("B1", "A"),
            new CellData("C1", "B"),

            new CellData("A2", "Bob"),
            new CellData("B2", "C"),
            new CellData("C2", "D"),

            new CellData("A3", "Carol"),
            new CellData("B3", "A"),
            new CellData("C3", "F")
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
