using Microsoft.Extensions.Logging;
using SchemaInfoScanner;
using SchemaInfoScanner.Catalogs;
using SchemaInfoScanner.Collectors;
using SchemaInfoScanner.Schemata.TypedPropertySchemata;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.PropertySchemaCompatibilityTests.PrimitiveTypes;

public class EnumKeyTests(ITestOutputHelper testOutputHelper)
{
    [Theory]
    [InlineData("1")]
    [InlineData("42")]
    [InlineData("999999")]
    public void KeyEnum_UndefinedIntValue_Passes(string argument)
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<EnumKeyTests>() is not TestOutputLogger<EnumKeyTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   public enum ItemId { None = 0 }

                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed record MyRecord(
                       [Key] ItemId Id,
                   );
                   """;

        var catalogs = CreateCatalogs(code, logger);
        var cells = new[] { new CellData("A1", argument) };
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
    [InlineData("1")]
    [InlineData("42")]
    public void NonKeyEnum_UndefinedIntValue_Throws(string argument)
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<EnumKeyTests>() is not TestOutputLogger<EnumKeyTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   public enum ItemCategory { Consumable, Weapon, Armor }

                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed record MyRecord(
                       ItemCategory Category,
                   );
                   """;

        var catalogs = CreateCatalogs(code, logger);
        var cells = new[] { new CellData("A1", argument) };
        var context = CompatibilityContext.CreateNoCollect(catalogs, cells);

        foreach (var recordSchema in catalogs.RecordSchemaCatalog.StaticDataRecordSchemata)
        {
            foreach (var propertySchema in recordSchema.PropertySchemata)
            {
                Assert.Throws<InvalidOperationException>(() => propertySchema.CheckCompatibility(context));
            }
        }

        Assert.Empty(logger.Logs);
    }

    [Theory]
    [InlineData("Consumable")]
    [InlineData("Weapon")]
    public void NonKeyEnum_DefinedMember_Passes(string argument)
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<EnumKeyTests>() is not TestOutputLogger<EnumKeyTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   public enum ItemCategory { Consumable, Weapon, Armor }

                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed record MyRecord(
                       ItemCategory Category,
                   );
                   """;

        var catalogs = CreateCatalogs(code, logger);
        var cells = new[] { new CellData("A1", argument) };
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
