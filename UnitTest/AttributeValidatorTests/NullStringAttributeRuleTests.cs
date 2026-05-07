using Microsoft.Extensions.Logging;
using SchemaInfoScanner;
using SchemaInfoScanner.Catalogs;
using SchemaInfoScanner.Collectors;
using SchemaInfoScanner.Exceptions;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.AttributeValidatorTests;

public class NullStringAttributeRuleTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void RequireTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<NullStringAttributeRuleTests>() is not TestOutputLogger<NullStringAttributeRuleTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed record MyRecord(
                       [NullString("-")]
                       int? Property
                   );
                   """;

        var loadResult = RecordSchemaLoader.OnLoad(code, logger);
        var recordSchemaSet = new RecordSchemaSet(loadResult, logger);
        var recordSchemaCatalog = new RecordSchemaCatalog(recordSchemaSet);

        RecordComplianceChecker.Check(recordSchemaCatalog, logger);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void RequireArrayTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<NullStringAttributeRuleTests>() is not TestOutputLogger<NullStringAttributeRuleTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed record MyRecord(
                       [NullString("-")]
                       [Length(3)]
                       ImmutableArray<int?> Property
                   );
                   """;

        var loadResult = RecordSchemaLoader.OnLoad(code, logger);
        var recordSchemaSet = new RecordSchemaSet(loadResult, logger);
        var recordSchemaCatalog = new RecordSchemaCatalog(recordSchemaSet);

        RecordComplianceChecker.Check(recordSchemaCatalog, logger);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void RequireSetTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<NullStringAttributeRuleTests>() is not TestOutputLogger<NullStringAttributeRuleTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed record MyRecord(
                       [NullString("-")]
                       [Length(3)]
                       FrozenSet<int?> Property
                   );
                   """;

        var loadResult = RecordSchemaLoader.OnLoad(code, logger);
        var recordSchemaSet = new RecordSchemaSet(loadResult, logger);
        var recordSchemaCatalog = new RecordSchemaCatalog(recordSchemaSet);

        RecordComplianceChecker.Check(recordSchemaCatalog, logger);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void MissingTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<NullStringAttributeRuleTests>() is not TestOutputLogger<NullStringAttributeRuleTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed record MyRecord(
                       int? Property
                   );
                   """;

        var loadResult = RecordSchemaLoader.OnLoad(code, logger);
        var recordSchemaSet = new RecordSchemaSet(loadResult, logger);
        var recordSchemaCatalog = new RecordSchemaCatalog(recordSchemaSet);

        Assert.Throws<InvalidAttributeUsageException>(() => RecordComplianceChecker.Check(recordSchemaCatalog, logger));
        Assert.Single(logger.Logs);
    }

    [Fact]
    public void MissingArrayTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<NullStringAttributeRuleTests>() is not TestOutputLogger<NullStringAttributeRuleTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed record MyRecord(
                       ImmutableArray<int?> Property
                   );
                   """;

        var loadResult = RecordSchemaLoader.OnLoad(code, logger);
        var recordSchemaSet = new RecordSchemaSet(loadResult, logger);
        var recordSchemaCatalog = new RecordSchemaCatalog(recordSchemaSet);

        Assert.Throws<InvalidAttributeUsageException>(() => RecordComplianceChecker.Check(recordSchemaCatalog, logger));
        Assert.Single(logger.Logs);
    }

    [Fact]
    public void MissingSetTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<NullStringAttributeRuleTests>() is not TestOutputLogger<NullStringAttributeRuleTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed record MyRecord(
                       FrozenSet<int?> Property
                   );
                   """;

        var loadResult = RecordSchemaLoader.OnLoad(code, logger);
        var recordSchemaSet = new RecordSchemaSet(loadResult, logger);
        var recordSchemaCatalog = new RecordSchemaCatalog(recordSchemaSet);

        Assert.Throws<InvalidAttributeUsageException>(() => RecordComplianceChecker.Check(recordSchemaCatalog, logger));
        Assert.Single(logger.Logs);
    }

    [Fact]
    public void MissingMapValueTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<NullStringAttributeRuleTests>() is not TestOutputLogger<NullStringAttributeRuleTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   public sealed record ValueRecord(
                       [Key]
                       int Id,
                       string? Name
                   );

                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed record MyRecord(
                       FrozenDictionary<int, ValueRecord> Property
                   );
                   """;

        var loadResult = RecordSchemaLoader.OnLoad(code, logger);
        var recordSchemaSet = new RecordSchemaSet(loadResult, logger);
        var recordSchemaCatalog = new RecordSchemaCatalog(recordSchemaSet);

        Assert.Throws<InvalidAttributeUsageException>(() => RecordComplianceChecker.Check(recordSchemaCatalog, logger));
        Assert.Single(logger.Logs);
    }

    [Fact]
    public void MissingMapNestedValueTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<NullStringAttributeRuleTests>() is not TestOutputLogger<NullStringAttributeRuleTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed record MyRecord(
                       FrozenDictionary<int, MyRecord.ValueRecord> Property
                   )
                   {
                       public sealed record ValueRecord(
                           [Key]
                           int Id,
                           string? Name
                       );
                   }
                   """;

        var loadResult = RecordSchemaLoader.OnLoad(code, logger);
        var recordSchemaSet = new RecordSchemaSet(loadResult, logger);
        var recordSchemaCatalog = new RecordSchemaCatalog(recordSchemaSet);

        Assert.Throws<InvalidAttributeUsageException>(() => RecordComplianceChecker.Check(recordSchemaCatalog, logger));
        Assert.Single(logger.Logs);
    }

    [Fact]
    public void DisallowTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<NullStringAttributeRuleTests>() is not TestOutputLogger<NullStringAttributeRuleTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed record MyRecord(
                       [NullString("-")]
                       int Property
                   );
                   """;

        var loadResult = RecordSchemaLoader.OnLoad(code, logger);
        var recordSchemaSet = new RecordSchemaSet(loadResult, logger);
        var recordSchemaCatalog = new RecordSchemaCatalog(recordSchemaSet);

        Assert.Throws<InvalidAttributeUsageException>(() => RecordComplianceChecker.Check(recordSchemaCatalog, logger));
        Assert.Single(logger.Logs);
    }

    [Fact]
    public void DisallowArrayTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<NullStringAttributeRuleTests>() is not TestOutputLogger<NullStringAttributeRuleTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed record MyRecord(
                       [NullString("-")]
                       ImmutableArray<int> Property
                   );
                   """;

        var loadResult = RecordSchemaLoader.OnLoad(code, logger);
        var recordSchemaSet = new RecordSchemaSet(loadResult, logger);
        var recordSchemaCatalog = new RecordSchemaCatalog(recordSchemaSet);

        Assert.Throws<InvalidAttributeUsageException>(() => RecordComplianceChecker.Check(recordSchemaCatalog, logger));
        Assert.Single(logger.Logs);
    }

    [Fact]
    public void DisallowSetTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<NullStringAttributeRuleTests>() is not TestOutputLogger<NullStringAttributeRuleTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed record MyRecord(
                       [NullString("-")]
                       FrozenSet<int> Property
                   );
                   """;

        var loadResult = RecordSchemaLoader.OnLoad(code, logger);
        var recordSchemaSet = new RecordSchemaSet(loadResult, logger);
        var recordSchemaCatalog = new RecordSchemaCatalog(recordSchemaSet);

        Assert.Throws<InvalidAttributeUsageException>(() => RecordComplianceChecker.Check(recordSchemaCatalog, logger));
        Assert.Single(logger.Logs);
    }

    [Fact]
    public void DisallowMapValueTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<NullStringAttributeRuleTests>() is not TestOutputLogger<NullStringAttributeRuleTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   public sealed record ValueRecord(
                       [Key]
                       int Id,
                       string Name
                   );

                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed record MyRecord(
                       [NullString("-")]
                       FrozenDictionary<int, ValueRecord> Property
                   );
                   """;

        var loadResult = RecordSchemaLoader.OnLoad(code, logger);
        var recordSchemaSet = new RecordSchemaSet(loadResult, logger);
        var recordSchemaCatalog = new RecordSchemaCatalog(recordSchemaSet);

        Assert.Throws<InvalidAttributeUsageException>(() => RecordComplianceChecker.Check(recordSchemaCatalog, logger));
        Assert.Single(logger.Logs);
    }

    [Fact]
    public void DisallowMapNestedValueTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<NullStringAttributeRuleTests>() is not TestOutputLogger<NullStringAttributeRuleTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed record MyRecord(
                       [NullString("-")]
                       FrozenDictionary<int, MyRecord.ValueRecord> Property
                   )
                   {
                       public sealed record ValueRecord(
                           [Key]
                           int Id,
                           string Name
                       );
                   }
                   """;

        var loadResult = RecordSchemaLoader.OnLoad(code, logger);
        var recordSchemaSet = new RecordSchemaSet(loadResult, logger);
        var recordSchemaCatalog = new RecordSchemaCatalog(recordSchemaSet);

        Assert.Throws<InvalidAttributeUsageException>(() => RecordComplianceChecker.Check(recordSchemaCatalog, logger));
        Assert.Single(logger.Logs);
    }
}
