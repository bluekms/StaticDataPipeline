using Microsoft.Extensions.Logging;
using SchemaInfoScanner;
using SchemaInfoScanner.Catalogs;
using SchemaInfoScanner.Collectors;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.NotSupportedPropertySchemaTests.CollectionPropertySchemaTests;

public class MapTypeTests(ITestOutputHelper testOutputHelper)
{
    [Theory]
    [InlineData("List")]
    [InlineData("HashSet")]
    public void RejectsNestedCollectionTypes(string collection)
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<MapTypeTests>() is not TestOutputLogger<MapTypeTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = $"""
                    [StaticDataRecord("Test", "TestSheet")]
                    public sealed record MyRecord(
                        Dictionary<int, {collection}<int>> Property,
                    );
                    """;

        var loadResult = RecordSchemaLoader.OnLoad(code, logger);
        Assert.Throws<NotSupportedException>(() => new RecordSchemaSet(loadResult, logger));
        Assert.Single(logger.Logs);
    }

    [Fact]
    public void RejectsNestedDictionaryTypes()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<MapTypeTests>() is not TestOutputLogger<MapTypeTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed record MyRecord(
                       Dictionary<int, Dictionary<int, string>> Property,
                   );
                   """;

        var loadResult = RecordSchemaLoader.OnLoad(code, logger);
        Assert.Throws<NotSupportedException>(() => new RecordSchemaSet(loadResult, logger));
        Assert.Single(logger.Logs);
    }

    [Theory]
    [InlineData("bool")]
    [InlineData("byte")]
    [InlineData("char")]
    [InlineData("decimal")]
    [InlineData("double")]
    [InlineData("float")]
    [InlineData("int")]
    [InlineData("long")]
    [InlineData("sbyte")]
    [InlineData("short")]
    [InlineData("string")]
    [InlineData("uint")]
    [InlineData("ulong")]
    [InlineData("ushort")]
    public void PrimitiveKeyPrimitiveValueMapNotSupportedTest(string value)
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<MapTypeTests>() is not TestOutputLogger<MapTypeTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = $"""
                    [StaticDataRecord("Test", "TestSheet")]
                    public sealed record MyRecord(
                        [Length(3)] FrozenDictionary<int, {value}> Property
                    );
                    """;

        var loadResult = RecordSchemaLoader.OnLoad(code, logger);
        Assert.Throws<NotSupportedException>(() => new RecordSchemaSet(loadResult, logger));
        Assert.Single(logger.Logs);
    }

    [Fact]
    public void EnumKeyEnumValueMapNotSupportedTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<MapTypeTests>() is not TestOutputLogger<MapTypeTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   public enum KeyEnum { A, B, C }
                   public enum ValueEnum { A, B, C }

                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed record MyRecord(
                       [Length(3)] FrozenDictionary<KeyEnum, ValueEnum> Property
                   );
                   """;

        var loadResult = RecordSchemaLoader.OnLoad(code, logger);
        Assert.Throws<NotSupportedException>(() => new RecordSchemaSet(loadResult, logger));
        Assert.Single(logger.Logs);
    }

    [Fact]
    public void EnumKeyDictionary_DifferentEnumTypeInValueRecord_NotSupported()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<MapTypeTests>() is not TestOutputLogger<MapTypeTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   public enum ItemIdA { None = 0 }
                   public enum ItemIdB { None = 0 }

                   public sealed record ValueRecord(
                       [Key] ItemIdB Id,
                       string Name,
                   );

                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed record MyRecord(
                       [Length(2)] FrozenDictionary<ItemIdA, ValueRecord> Properties,
                   );
                   """;

        var loadResult = RecordSchemaLoader.OnLoad(code, logger);
        var recordSchemaSet = new RecordSchemaSet(loadResult, logger);
        var recordSchemaCatalog = new RecordSchemaCatalog(recordSchemaSet);

        Assert.Throws<NotSupportedException>(() => RecordComplianceChecker.Check(recordSchemaCatalog, logger));
        Assert.Single(logger.Logs);
    }

    [Fact]
    public void EnumKeyDictionary_SameEnumTypeInValueRecord_Supported()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<MapTypeTests>() is not TestOutputLogger<MapTypeTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   public enum ItemId { None = 0 }

                   public sealed record ValueRecord(
                       [Key] ItemId Id,
                       string Name,
                   );

                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed record MyRecord(
                       [Length(2)] FrozenDictionary<ItemId, ValueRecord> Properties,
                   );
                   """;

        var loadResult = RecordSchemaLoader.OnLoad(code, logger);
        var recordSchemaSet = new RecordSchemaSet(loadResult, logger);
        var recordSchemaCatalog = new RecordSchemaCatalog(recordSchemaSet);

        RecordComplianceChecker.Check(recordSchemaCatalog, logger);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void DateTimeKeyAndValueMapNotSupportedTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<MapTypeTests>() is not TestOutputLogger<MapTypeTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed record MyRecord(
                       [DateTimeFormat("yyyy-MM-dd HH:mm:ss.fff")]
                       [Length(3)]
                       FrozenDictionary<DateTime, DateTime> Property
                   );
                   """;

        var loadResult = RecordSchemaLoader.OnLoad(code, logger);
        Assert.Throws<NotSupportedException>(() => new RecordSchemaSet(loadResult, logger));
        Assert.Single(logger.Logs);
    }

    [Fact]
    public void TimeSpanKeyAndValueMapNotSupportedTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<MapTypeTests>() is not TestOutputLogger<MapTypeTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed record MyRecord(
                       [TimeSpanFormat("c")]
                       [Length(3)]
                       FrozenDictionary<TimeSpan, TimeSpan> Property
                   );
                   """;

        var loadResult = RecordSchemaLoader.OnLoad(code, logger);
        Assert.Throws<NotSupportedException>(() => new RecordSchemaSet(loadResult, logger));
        Assert.Single(logger.Logs);
    }
}
