using Microsoft.Extensions.Logging;
using SchemaInfoScanner;
using SchemaInfoScanner.Catalogs;
using SchemaInfoScanner.Collectors;
using SchemaInfoScanner.Exceptions;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.AttributeValidatorTests;

public class ForeignKeyRuleTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void ForeignKey_ValidColumn_Succeeds()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<ForeignKeyRuleTests>() is not TestOutputLogger<ForeignKeyRuleTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   public record StaticDataTable<TSelf, TRecord, TKey>;

                   [StaticDataRecord("Category", "CategorySheet")]
                   public sealed record CategoryRecord(
                       int Id,
                       string Name);

                   [StaticDataRecord("Item", "ItemSheet")]
                   public sealed record ItemRecord(
                       int Id,
                       [ForeignKey("Category", "Id")] int CategoryId);

                   public record CategoryTable : StaticDataTable<CategoryTable, CategoryRecord, int>;
                   public record ItemTable : StaticDataTable<ItemTable, ItemRecord, int>;

                   public sealed record TableSet(
                       CategoryTable? Category,
                       ItemTable? Item);
                   """;

        var catalog = BuildCatalog(code, logger);
        ForeignKeySchemaChecker.Check(catalog, logger);

        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void ForeignKey_InvalidRecordColumnName_ThrowsAggregateException()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<ForeignKeyRuleTests>() is not TestOutputLogger<ForeignKeyRuleTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   public record StaticDataTable<TSelf, TRecord, TKey>;

                   [StaticDataRecord("Category", "CategorySheet")]
                   public sealed record CategoryRecord(
                       int Id,
                       string Name);

                   [StaticDataRecord("Item", "ItemSheet")]
                   public sealed record ItemRecord(
                       int Id,
                       [ForeignKey("Category", "Id")] int CategoryId);

                   public record CategoryTable : StaticDataTable<CategoryTable, CategoryRecord, int>;
                   public record ItemTable : StaticDataTable<ItemTable, ItemRecord, int>;

                   public sealed record TableSet(
                       CategoryTable? Category,
                       ItemTable? Item);
                   """;

        var catalog = BuildCatalog(code, logger);
        var ex = Assert.Throws<AggregateException>(() => ForeignKeySchemaChecker.Check(catalog, logger));
        Assert.Single(ex.InnerExceptions);
        Assert.IsType<InvalidAttributeUsageException>(ex.InnerExceptions[0]);
        Assert.Single(logger.Logs);
    }

    [Fact]
    public void SwitchForeignKey_InvalidConditionColumnName_ThrowsAggregateException()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<ForeignKeyRuleTests>() is not TestOutputLogger<ForeignKeyRuleTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   public record StaticDataTable<TSelf, TRecord, TKey>;

                   [StaticDataRecord("Category", "CategorySheet")]
                   public sealed record CategoryRecord(
                       int Id,
                       string Name);

                   [StaticDataRecord("Item", "ItemSheet")]
                   public sealed record ItemRecord(
                       int Id,
                       string Kind,
                       [SwitchForeignKey("Kindd", "A", "Category", "Id")] int CategoryId);

                   public record CategoryTable : StaticDataTable<CategoryTable, CategoryRecord, int>;
                   public record ItemTable : StaticDataTable<ItemTable, ItemRecord, int>;

                   public sealed record TableSet(
                       CategoryTable? Category,
                       ItemTable? Item);
                   """;

        var catalog = BuildCatalog(code, logger);
        var ex = Assert.Throws<AggregateException>(() => ForeignKeySchemaChecker.Check(catalog, logger));
        Assert.Single(ex.InnerExceptions);
        Assert.IsType<InvalidAttributeUsageException>(ex.InnerExceptions[0]);
        Assert.Single(logger.Logs);
    }

    [Fact]
    public void SwitchForeignKey_InvalidRecordColumnName_ThrowsAggregateException()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<ForeignKeyRuleTests>() is not TestOutputLogger<ForeignKeyRuleTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   public record StaticDataTable<TSelf, TRecord, TKey>;

                   [StaticDataRecord("Category", "CategorySheet")]
                   public sealed record CategoryRecord(
                       int Id,
                       string Name);

                   [StaticDataRecord("Item", "ItemSheet")]
                   public sealed record ItemRecord(
                       int Id,
                       string Kind,
                       [SwitchForeignKey("Kind", "A", "Category", "Id")] int CategoryId);

                   public record CategoryTable : StaticDataTable<CategoryTable, CategoryRecord, int>;
                   public record ItemTable : StaticDataTable<ItemTable, ItemRecord, int>;

                   public sealed record TableSet(
                       CategoryTable? Category,
                       ItemTable? Item);
                   """;

        var catalog = BuildCatalog(code, logger);
        var ex = Assert.Throws<AggregateException>(() => ForeignKeySchemaChecker.Check(catalog, logger));
        Assert.Single(ex.InnerExceptions);
        Assert.IsType<InvalidAttributeUsageException>(ex.InnerExceptions[0]);
        Assert.Single(logger.Logs);
    }

    [Fact]
    public void ForeignKey_MultipleErrors_AggregatedInSingleException()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<ForeignKeyRuleTests>() is not TestOutputLogger<ForeignKeyRuleTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   public record StaticDataTable<TSelf, TRecord, TKey>;

                   [StaticDataRecord("Category", "CategorySheet")]
                   public sealed record CategoryRecord(
                       int Id,
                       string Name);

                   [StaticDataRecord("Item", "ItemSheet")]
                   public sealed record ItemRecord(
                       int Id,
                       [ForeignKey("Category", "Id")] int CategoryId,
                       [ForeignKey("Category", "Name")] string CategoryName);

                   public record CategoryTable : StaticDataTable<CategoryTable, CategoryRecord, int>;
                   public record ItemTable : StaticDataTable<ItemTable, ItemRecord, int>;

                   public sealed record TableSet(
                       CategoryTable? Category,
                       ItemTable? Item);
                   """;

        var catalog = BuildCatalog(code, logger);
        var ex = Assert.Throws<AggregateException>(() => ForeignKeySchemaChecker.Check(catalog, logger));
        Assert.Equal(2, ex.InnerExceptions.Count);
        Assert.Equal(2, logger.Logs.Count);
    }

    [Fact]
    public void SwitchForeignKey_Valid_Succeeds()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<ForeignKeyRuleTests>() is not TestOutputLogger<ForeignKeyRuleTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   public record StaticDataTable<TSelf, TRecord, TKey>;

                   [StaticDataRecord("Category", "CategorySheet")]
                   public sealed record CategoryRecord(
                       int Id,
                       string Name);

                   [StaticDataRecord("Item", "ItemSheet")]
                   public sealed record ItemRecord(
                       int Id,
                       string Kind,
                       [SwitchForeignKey("Kind", "A", "Category", "Id")] int CategoryId);

                   public record CategoryTable : StaticDataTable<CategoryTable, CategoryRecord, int>;
                   public record ItemTable : StaticDataTable<ItemTable, ItemRecord, int>;

                   public sealed record TableSet(
                       CategoryTable? Category,
                       ItemTable? Item);
                   """;

        var catalog = BuildCatalog(code, logger);
        ForeignKeySchemaChecker.Check(catalog, logger);

        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void ForeignKey_ColumnExistsOnlyInOtherRecord_ThrowsAggregateException()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<ForeignKeyRuleTests>() is not TestOutputLogger<ForeignKeyRuleTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   public record StaticDataTable<TSelf, TRecord, TKey>;

                   [StaticDataRecord("Category", "CategorySheet")]
                   public sealed record CategoryRecord(
                       int Id,
                       string Name);

                   [StaticDataRecord("Tag", "TagSheet")]
                   public sealed record TagRecord(
                       int Id,
                       string Label);

                   [StaticDataRecord("Item", "ItemSheet")]
                   public sealed record ItemRecord(
                       int Id,
                       [ForeignKey("Category", "Label")] int CategoryId);

                   public record CategoryTable : StaticDataTable<CategoryTable, CategoryRecord, int>;
                   public record TagTable : StaticDataTable<TagTable, TagRecord, int>;
                   public record ItemTable : StaticDataTable<ItemTable, ItemRecord, int>;

                   public sealed record TableSet(
                       CategoryTable? Category,
                       TagTable? Tag,
                       ItemTable? Item);
                   """;

        var catalog = BuildCatalog(code, logger);
        var ex = Assert.Throws<AggregateException>(() => ForeignKeySchemaChecker.Check(catalog, logger));
        Assert.Single(ex.InnerExceptions);
        Assert.IsType<InvalidAttributeUsageException>(ex.InnerExceptions[0]);
        Assert.Single(logger.Logs);
    }

    [Fact]
    public void SwitchForeignKey_ColumnExistsOnlyInOtherRecord_ThrowsAggregateException()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<ForeignKeyRuleTests>() is not TestOutputLogger<ForeignKeyRuleTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   public record StaticDataTable<TSelf, TRecord, TKey>;

                   [StaticDataRecord("Category", "CategorySheet")]
                   public sealed record CategoryRecord(
                       int Id,
                       string Name);

                   [StaticDataRecord("Tag", "TagSheet")]
                   public sealed record TagRecord(
                       int Id,
                       string Label);

                   [StaticDataRecord("Item", "ItemSheet")]
                   public sealed record ItemRecord(
                       int Id,
                       string Kind,
                       [SwitchForeignKey("Kind", "A", "Category", "Label")] int CategoryId);

                   public record CategoryTable : StaticDataTable<CategoryTable, CategoryRecord, int>;
                   public record TagTable : StaticDataTable<TagTable, TagRecord, int>;
                   public record ItemTable : StaticDataTable<ItemTable, ItemRecord, int>;

                   public sealed record TableSet(
                       CategoryTable? Category,
                       TagTable? Tag,
                       ItemTable? Item);
                   """;

        var catalog = BuildCatalog(code, logger);
        var ex = Assert.Throws<AggregateException>(() => ForeignKeySchemaChecker.Check(catalog, logger));
        Assert.Single(ex.InnerExceptions);
        Assert.IsType<InvalidAttributeUsageException>(ex.InnerExceptions[0]);
        Assert.Single(logger.Logs);
    }

    private static RecordSchemaCatalog BuildCatalog(string code, ILogger logger)
    {
        var loadResult = RecordSchemaLoader.OnLoad(code, logger);
        var recordSchemaSet = new RecordSchemaSet(loadResult, logger);
        var recordSchemaCatalog = new RecordSchemaCatalog(recordSchemaSet);
        RecordComplianceChecker.Check(recordSchemaCatalog, logger);
        return recordSchemaCatalog;
    }
}
