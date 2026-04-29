using Microsoft.Extensions.Logging;
using SchemaInfoScanner;
using SchemaInfoScanner.Catalogs;
using SchemaInfoScanner.Collectors;
using SchemaInfoScanner.Exceptions;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.AttributeValidatorTests;

public class TableSetRuleTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void ForeignKey_ValidTableSetName_Succeeds()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<TableSetRuleTests>() is not TestOutputLogger<TableSetRuleTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   [StaticDataRecord("Category", "CategorySheet")]
                   public sealed record CategoryRecord(
                       int Id,
                       string Name,
                   );

                   [StaticDataRecord("Item", "ItemSheet")]
                   public sealed record ItemRecord(
                       int Id,
                       [ForeignKey("Categories", "Id")] int CategoryId,
                   );

                   public sealed record TableSet(
                       int? Items,
                       int? Categories,
                   );
                   """;

        var catalog = BuildCatalog(code, logger);
        TableSetSchemaChecker.Check(catalog, logger);

        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void ForeignKey_InvalidTableSetName_ThrowsAggregateException()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<TableSetRuleTests>() is not TestOutputLogger<TableSetRuleTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   [StaticDataRecord("Category", "CategorySheet")]
                   public sealed record CategoryRecord(
                       int Id,
                       string Name,
                   );

                   [StaticDataRecord("Item", "ItemSheet")]
                   public sealed record ItemRecord(
                       int Id,
                       [ForeignKey("Categoriess", "Id")] int CategoryId,
                   );

                   public sealed record TableSet(
                       int? Items,
                       int? Categories,
                   );
                   """;

        var catalog = BuildCatalog(code, logger);
        var ex = Assert.Throws<AggregateException>(() => TableSetSchemaChecker.Check(catalog, logger));
        Assert.Single(ex.InnerExceptions);
        Assert.IsType<InvalidAttributeUsageException>(ex.InnerExceptions[0]);
        Assert.Single(logger.Logs);
    }

    [Fact]
    public void SwitchForeignKey_InvalidTableSetName_ThrowsAggregateException()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<TableSetRuleTests>() is not TestOutputLogger<TableSetRuleTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   [StaticDataRecord("Category", "CategorySheet")]
                   public sealed record CategoryRecord(
                       int Id,
                       string Name,
                   );

                   [StaticDataRecord("Item", "ItemSheet")]
                   public sealed record ItemRecord(
                       int Id,
                       string Kind,
                       [SwitchForeignKey("Kind", "A", "Categoriess", "Id")] int CategoryId,
                   );

                   public sealed record TableSet(
                       int? Items,
                       int? Categories,
                   );
                   """;

        var catalog = BuildCatalog(code, logger);
        var ex = Assert.Throws<AggregateException>(() => TableSetSchemaChecker.Check(catalog, logger));
        Assert.Single(ex.InnerExceptions);
        Assert.IsType<InvalidAttributeUsageException>(ex.InnerExceptions[0]);
        Assert.Single(logger.Logs);
    }

    [Fact]
    public void NoTableSet_SkipsValidation()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<TableSetRuleTests>() is not TestOutputLogger<TableSetRuleTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   [StaticDataRecord("Category", "CategorySheet")]
                   public sealed record CategoryRecord(
                       int Id,
                       string Name,
                   );

                   [StaticDataRecord("Item", "ItemSheet")]
                   public sealed record ItemRecord(
                       int Id,
                       [ForeignKey("SomeTable", "Id")] int CategoryId,
                   );
                   """;

        var catalog = BuildCatalog(code, logger);
        TableSetSchemaChecker.Check(catalog, logger);

        Assert.Empty(logger.Logs);
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
