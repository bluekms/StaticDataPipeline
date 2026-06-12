using Microsoft.Extensions.Logging;
using SchemaInfoScanner;
using SchemaInfoScanner.Catalogs;
using SchemaInfoScanner.Collectors;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.PropertySchemaTests.RecordTypeSchemaTests;

public partial class RecordTypeTests(ITestOutputHelper testOutputHelper)
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
                   public sealed partial record MyRecord(
                       Identifier Id
                   )
                   {
                      public partial record struct Identifier(int Value);
                   }
                   """;

        var loadResult = RecordSchemaLoader.OnLoad(code, logger);

        var recordSchemaSet = new RecordSchemaSet(loadResult, logger);
        var recordSchemaCatalog = new RecordSchemaCatalog(recordSchemaSet);
        RecordComplianceChecker.Check(recordSchemaCatalog, logger);

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
                   public sealed partial record MyRecord(
                       MyData Data
                   );

                   public partial record struct MyData(int Value);
                   """;

        var loadResult = RecordSchemaLoader.OnLoad(code, logger);

        var recordSchemaSet = new RecordSchemaSet(loadResult, logger);
        var recordSchemaCatalog = new RecordSchemaCatalog(recordSchemaSet);
        RecordComplianceChecker.Check(recordSchemaCatalog, logger);

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
                   public sealed partial record MyRecord(
                       [Length(3)] ImmutableArray<MyData> Data
                   );

                   public partial record struct MyData(int Value);
                   """;

        var loadResult = RecordSchemaLoader.OnLoad(code, logger);

        var recordSchemaSet = new RecordSchemaSet(loadResult, logger);
        var recordSchemaCatalog = new RecordSchemaCatalog(recordSchemaSet);
        RecordComplianceChecker.Check(recordSchemaCatalog, logger);

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
                   public sealed partial record MyRecord(
                       [Length(3)] FrozenSet<MyData> Data
                   );

                   public partial record struct MyData(int Value);
                   """;

        var loadResult = RecordSchemaLoader.OnLoad(code, logger);

        var recordSchemaSet = new RecordSchemaSet(loadResult, logger);
        var recordSchemaCatalog = new RecordSchemaCatalog(recordSchemaSet);
        RecordComplianceChecker.Check(recordSchemaCatalog, logger);

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
                   public sealed partial record MyRecord(
                       [Length(3)] FrozenDictionary<int, MyData> Data
                   );

                   public partial record struct MyData([Key] int Id, string Value);
                   """;

        var loadResult = RecordSchemaLoader.OnLoad(code, logger);

        var recordSchemaSet = new RecordSchemaSet(loadResult, logger);
        var recordSchemaCatalog = new RecordSchemaCatalog(recordSchemaSet);
        RecordComplianceChecker.Check(recordSchemaCatalog, logger);

        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void RecordKeyAndRecordValueMapTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<RecordTypeTests>() is not TestOutputLogger<RecordTypeTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed partial record MyRecord(
                       [Length(3)] FrozenDictionary<KeyData, MyData> Data
                   );

                   public partial record struct KeyData(int Key1, string Key2);
                   public partial record struct MyData([Key] KeyData Key, string Value);
                   """;

        var loadResult = RecordSchemaLoader.OnLoad(code, logger);

        var recordSchemaSet = new RecordSchemaSet(loadResult, logger);
        var recordSchemaCatalog = new RecordSchemaCatalog(recordSchemaSet);
        RecordComplianceChecker.Check(recordSchemaCatalog, logger);

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
                   public sealed partial record MyRecord(
                       [Length(3)] FrozenDictionary<KeyData, MyData> Data
                   );

                   public partial record struct KeyData(InnerKey Inner, int Key1)
                   {
                       public partial record struct InnerKey(int X, int Y);
                   }

                   public partial record struct MyData([Key] KeyData Key, string Value);
                   """;

        var loadResult = RecordSchemaLoader.OnLoad(code, logger);

        var recordSchemaSet = new RecordSchemaSet(loadResult, logger);
        var recordSchemaCatalog = new RecordSchemaCatalog(recordSchemaSet);
        RecordComplianceChecker.Check(recordSchemaCatalog, logger);

        Assert.Empty(logger.Logs);
    }
}
