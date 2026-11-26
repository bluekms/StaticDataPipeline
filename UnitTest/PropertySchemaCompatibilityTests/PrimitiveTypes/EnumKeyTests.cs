using System.Globalization;
using Microsoft.Extensions.Logging;
using SchemaInfoScanner;
using SchemaInfoScanner.Catalogs;
using SchemaInfoScanner.Collectors;
using SchemaInfoScanner.Schemata;
using SchemaInfoScanner.Schemata.TypedPropertySchemata;
using UnitTest.Utility;
using Xunit.Abstractions;
using ScannerMessages = SchemaInfoScanner.Resources.Messages;

namespace UnitTest.PropertySchemaCompatibilityTests.PrimitiveTypes;

public partial class EnumKeyTests(ITestOutputHelper testOutputHelper)
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
                   public sealed partial record MyRecord(
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
                   public sealed partial record MyRecord(
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
                   public sealed partial record MyRecord(
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

    [Theory]
    [InlineData("None")]
    [InlineData("-7")]
    public void KeyEnum_MemberNameOrNegativeInt_Passes(string argument)
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
                   public sealed partial record MyRecord(
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
    [InlineData("Sword")]
    [InlineData("none")]
    [InlineData("1.5")]
    [InlineData("0x10")]
    [InlineData("99999999999")]
    public void KeyEnum_NeitherMemberNameNorInt_Throws(string argument)
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
                   public sealed partial record MyRecord(
                       [Key] ItemId Id,
                   );
                   """;

        var catalogs = CreateCatalogs(code, logger);
        var cells = new[] { new CellData("A1", argument) };
        var context = CompatibilityContext.CreateNoCollect(catalogs, cells);

        var checkedCount = 0;
        foreach (var recordSchema in catalogs.RecordSchemaCatalog.StaticDataRecordSchemata)
        {
            foreach (var propertySchema in recordSchema.PropertySchemata)
            {
                var exception = Assert.Throws<InvalidOperationException>(() => propertySchema.CheckCompatibility(context));
                Assert.Equal(KeyEnumMessage(argument, "A1", propertySchema), exception.Message);
                checkedCount++;
            }
        }

        Assert.Equal(1, checkedCount);
        Assert.Empty(logger.Logs);
    }

    [Theory]
    [InlineData("sbyte", "-128", false)]
    [InlineData("sbyte", "127", false)]
    [InlineData("sbyte", "128", true)]
    [InlineData("byte", "255", false)]
    [InlineData("byte", "256", true)]
    [InlineData("byte", "-1", true)]
    [InlineData("short", "32767", false)]
    [InlineData("short", "32768", true)]
    [InlineData("ushort", "65535", false)]
    [InlineData("ushort", "65536", true)]
    [InlineData("ushort", "-1", true)]
    [InlineData("int", "2147483647", false)]
    [InlineData("int", "2147483648", true)]
    [InlineData("uint", "4294967295", false)]
    [InlineData("uint", "4294967296", true)]
    [InlineData("uint", "-1", true)]
    [InlineData("long", "9223372036854775807", false)]
    [InlineData("long", "9223372036854775808", true)]
    [InlineData("ulong", "18446744073709551615", false)]
    [InlineData("ulong", "18446744073709551616", true)]
    [InlineData("ulong", "-1", true)]
    public void KeyEnum_UndefinedInt_IsReadAsTheUnderlyingType(string underlyingType, string argument, bool shouldThrow)
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<EnumKeyTests>() is not TestOutputLogger<EnumKeyTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = $$"""
                     public enum Slot : {{underlyingType}} { None = 0 }

                     [StaticDataRecord("Test", "TestSheet")]
                     public sealed partial record MyRecord(
                         [Key] Slot Id,
                     );
                     """;

        var catalogs = CreateCatalogs(code, logger);
        var cells = new[] { new CellData("A1", argument) };
        var context = CompatibilityContext.CreateNoCollect(catalogs, cells);

        var checkedCount = 0;
        foreach (var recordSchema in catalogs.RecordSchemaCatalog.StaticDataRecordSchemata)
        {
            foreach (var propertySchema in recordSchema.PropertySchemata)
            {
                var exception = Record.Exception(() => propertySchema.CheckCompatibility(context));
                if (shouldThrow)
                {
                    var invalid = Assert.IsType<InvalidOperationException>(exception);
                    Assert.Equal(KeyEnumMessage(argument, "A1", propertySchema), invalid.Message);
                }
                else
                {
                    Assert.Null(exception);
                }

                checkedCount++;
            }
        }

        Assert.Equal(1, checkedCount);
        Assert.Empty(logger.Logs);
    }

    [Theory]
    [InlineData("Slime")]
    [InlineData("1500")]
    public void KeyEnumWithRange_MemberNameOrIntInsideRange_Passes(string argument)
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<EnumKeyTests>() is not TestOutputLogger<EnumKeyTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   public enum MonsterId { Slime = 1001 }

                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed partial record MyRecord(
                       [Key] [Range(typeof(MonsterId), "1000", "9999")] MonsterId Id,
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

    [Fact]
    public void KeyEnumWithRange_UnknownName_ThrowsNamingTheCellBeforeRangeIsChecked()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<EnumKeyTests>() is not TestOutputLogger<EnumKeyTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   public enum MonsterId { Slime = 1001 }

                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed partial record MyRecord(
                       [Key] [Range(typeof(MonsterId), "1000", "9999")] MonsterId Id,
                   );
                   """;

        var catalogs = CreateCatalogs(code, logger);
        var cells = new[] { new CellData("A1", "Dragon") };
        var context = CompatibilityContext.CreateNoCollect(catalogs, cells);

        var checkedCount = 0;
        foreach (var recordSchema in catalogs.RecordSchemaCatalog.StaticDataRecordSchemata)
        {
            foreach (var propertySchema in recordSchema.PropertySchemata)
            {
                var exception = Assert.Throws<InvalidOperationException>(() => propertySchema.CheckCompatibility(context));
                Assert.Equal(KeyEnumMessage("Dragon", "A1", propertySchema), exception.Message);
                checkedCount++;
            }
        }

        Assert.Equal(1, checkedCount);
        Assert.Empty(logger.Logs);
    }

    private static string KeyEnumMessage(string value, string address, PropertySchemaBase propertySchema)
    {
        return string.Format(
            CultureInfo.CurrentCulture,
            ScannerMessages.Composite.InvalidCellValueForKeyEnum,
            value,
            address,
            propertySchema.NamedTypeSymbol.ToString());
    }

    private static MetadataCatalogs CreateCatalogs(string code, ILogger logger)
    {
        var loadResult = RecordSchemaLoader.OnLoad(code, logger);
        var recordSchemaSet = new RecordSchemaSet(loadResult, logger);
        var recordSchemaCatalog = new RecordSchemaCatalog(recordSchemaSet);
        RecordComplianceChecker.Check(recordSchemaCatalog, logger);

        return new(recordSchemaCatalog);
    }
}
