using Microsoft.Extensions.Logging;
using SchemaInfoScanner.Schemata.TypedPropertySchemata;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.PropertySchemaCompatibilityTests.PrimitiveTypes;

public class RangeAttributeExplicitValueEnumTests(ITestOutputHelper testOutputHelper)
{
    [Theory]
    [InlineData("Slime")]
    [InlineData("BossDragon")]
    public void ExplicitValueEnum_MemberNameInRange_Succeeds(string value)
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<RangeAttributeExplicitValueEnumTests>() is not TestOutputLogger<RangeAttributeExplicitValueEnumTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   public enum MonsterId { Slime = 1001, BossDragon = 9001 }

                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed partial record MyRecord(
                       [Range(typeof(MonsterId), "1000", "9999")]
                       MonsterId Property,
                   );
                   """;

        var catalogs = RangeAttributeTestHelpers.CreateCatalogs(code, logger);
        var cells = new[] { new CellData("A1", value) };
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
    public void ExplicitValueEnum_MemberNameOutOfRange_Throws()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<RangeAttributeExplicitValueEnumTests>() is not TestOutputLogger<RangeAttributeExplicitValueEnumTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   public enum MonsterId { Slime = 1001, BossDragon = 9001 }

                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed partial record MyRecord(
                       [Range(typeof(MonsterId), "2000", "9999")]
                       MonsterId Property,
                   );
                   """;

        var catalogs = RangeAttributeTestHelpers.CreateCatalogs(code, logger);
        var cells = new[] { new CellData("A1", "Slime") };
        var context = CompatibilityContext.CreateNoCollect(catalogs, cells);

        foreach (var recordSchema in catalogs.RecordSchemaCatalog.StaticDataRecordSchemata)
        {
            foreach (var propertySchema in recordSchema.PropertySchemata)
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => propertySchema.CheckCompatibility(context));
            }
        }

        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void ExplicitValueEnum_MemberNameBounds_Succeeds()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<RangeAttributeExplicitValueEnumTests>() is not TestOutputLogger<RangeAttributeExplicitValueEnumTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   public enum MonsterId { Slime = 1001, BossDragon = 9001 }

                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed partial record MyRecord(
                       [Range(typeof(MonsterId), "Slime", "BossDragon")]
                       MonsterId Property,
                   );
                   """;

        var catalogs = RangeAttributeTestHelpers.CreateCatalogs(code, logger);
        var cells = new[] { new CellData("A1", "Slime") };
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
    public void MixedEnum_ImplicitMemberFollowsExplicitValue_Succeeds()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<RangeAttributeExplicitValueEnumTests>() is not TestOutputLogger<RangeAttributeExplicitValueEnumTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   public enum Tier { Bronze = 10, Silver, Gold = 20 }

                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed partial record MyRecord(
                       [Range(typeof(Tier), "11", "11")]
                       Tier Property,
                   );
                   """;

        var catalogs = RangeAttributeTestHelpers.CreateCatalogs(code, logger);
        var cells = new[] { new CellData("A1", "Silver") };
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
    public void MixedEnum_ImplicitMemberFollowsExplicitValue_OutOfRangeThrows()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<RangeAttributeExplicitValueEnumTests>() is not TestOutputLogger<RangeAttributeExplicitValueEnumTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   public enum Tier { Bronze = 10, Silver, Gold = 20 }

                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed partial record MyRecord(
                       [Range(typeof(Tier), "12", "12")]
                       Tier Property,
                   );
                   """;

        var catalogs = RangeAttributeTestHelpers.CreateCatalogs(code, logger);
        var cells = new[] { new CellData("A1", "Silver") };
        var context = CompatibilityContext.CreateNoCollect(catalogs, cells);

        foreach (var recordSchema in catalogs.RecordSchemaCatalog.StaticDataRecordSchemata)
        {
            foreach (var propertySchema in recordSchema.PropertySchemata)
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => propertySchema.CheckCompatibility(context));
            }
        }

        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void NegativeLiteralEnum_ResolvesSign_Succeeds()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<RangeAttributeExplicitValueEnumTests>() is not TestOutputLogger<RangeAttributeExplicitValueEnumTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   public enum Delta { Down = -5, Zero = 0 }

                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed partial record MyRecord(
                       [Range(typeof(Delta), "-10", "-1")]
                       Delta Property,
                   );
                   """;

        var catalogs = RangeAttributeTestHelpers.CreateCatalogs(code, logger);
        var cells = new[] { new CellData("A1", "Down") };
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
    public void NegativeLiteralEnum_ResolvesSign_OutOfRangeThrows()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<RangeAttributeExplicitValueEnumTests>() is not TestOutputLogger<RangeAttributeExplicitValueEnumTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   public enum Delta { Down = -5, Zero = 0 }

                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed partial record MyRecord(
                       [Range(typeof(Delta), "-10", "-1")]
                       Delta Property,
                   );
                   """;

        var catalogs = RangeAttributeTestHelpers.CreateCatalogs(code, logger);
        var cells = new[] { new CellData("A1", "Zero") };
        var context = CompatibilityContext.CreateNoCollect(catalogs, cells);

        foreach (var recordSchema in catalogs.RecordSchemaCatalog.StaticDataRecordSchemata)
        {
            foreach (var propertySchema in recordSchema.PropertySchemata)
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => propertySchema.CheckCompatibility(context));
            }
        }

        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void NonLiteralMemberValue_ThrowsWithMemberName()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<RangeAttributeExplicitValueEnumTests>() is not TestOutputLogger<RangeAttributeExplicitValueEnumTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   public enum Flags { A = 1, B = A << 1 }

                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed partial record MyRecord(
                       [Range(typeof(Flags), "0", "10")]
                       Flags Property,
                   );
                   """;

        var catalogs = RangeAttributeTestHelpers.CreateCatalogs(code, logger);
        var cells = new[] { new CellData("A1", "B") };
        var context = CompatibilityContext.CreateNoCollect(catalogs, cells);

        foreach (var recordSchema in catalogs.RecordSchemaCatalog.StaticDataRecordSchemata)
        {
            foreach (var propertySchema in recordSchema.PropertySchemata)
            {
                var exception = Assert.Throws<InvalidOperationException>(() => propertySchema.CheckCompatibility(context));
                Assert.Contains("'B'", exception.Message, StringComparison.Ordinal);
                Assert.Contains("[Range]", exception.Message, StringComparison.Ordinal);
            }
        }

        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void LiteralMemberAfterNonLiteralMember_Succeeds()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<RangeAttributeExplicitValueEnumTests>() is not TestOutputLogger<RangeAttributeExplicitValueEnumTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   public enum Flags { A = 1, B = A << 1, C = 4 }

                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed partial record MyRecord(
                       [Range(typeof(Flags), "0", "10")]
                       Flags Property,
                   );
                   """;

        var catalogs = RangeAttributeTestHelpers.CreateCatalogs(code, logger);
        var cells = new[] { new CellData("A1", "C") };
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
    public void ImplicitMemberAfterNonLiteralMember_ThrowsNamingNonLiteralMember()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<RangeAttributeExplicitValueEnumTests>() is not TestOutputLogger<RangeAttributeExplicitValueEnumTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   public enum Flags { A = 1, B = A << 1, C }

                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed partial record MyRecord(
                       [Range(typeof(Flags), "0", "10")]
                       Flags Property,
                   );
                   """;

        var catalogs = RangeAttributeTestHelpers.CreateCatalogs(code, logger);
        var cells = new[] { new CellData("A1", "C") };
        var context = CompatibilityContext.CreateNoCollect(catalogs, cells);

        foreach (var recordSchema in catalogs.RecordSchemaCatalog.StaticDataRecordSchemata)
        {
            foreach (var propertySchema in recordSchema.PropertySchemata)
            {
                var exception = Assert.Throws<InvalidOperationException>(() => propertySchema.CheckCompatibility(context));
                Assert.Contains("'B'", exception.Message, StringComparison.Ordinal);
                Assert.Contains("[Range]", exception.Message, StringComparison.Ordinal);
            }
        }

        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void UlongLiteralBeyondLongRange_ThrowsWithMemberName()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<RangeAttributeExplicitValueEnumTests>() is not TestOutputLogger<RangeAttributeExplicitValueEnumTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   public enum Mask : ulong { All = 0xFFFFFFFFFFFFFFFF }

                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed partial record MyRecord(
                       [Range(typeof(Mask), "0", "10")]
                       Mask Property,
                   );
                   """;

        var catalogs = RangeAttributeTestHelpers.CreateCatalogs(code, logger);
        var cells = new[] { new CellData("A1", "All") };
        var context = CompatibilityContext.CreateNoCollect(catalogs, cells);

        foreach (var recordSchema in catalogs.RecordSchemaCatalog.StaticDataRecordSchemata)
        {
            foreach (var propertySchema in recordSchema.PropertySchemata)
            {
                var exception = Assert.Throws<InvalidOperationException>(() => propertySchema.CheckCompatibility(context));
                Assert.Contains("'All'", exception.Message, StringComparison.Ordinal);
                Assert.Contains("[Range]", exception.Message, StringComparison.Ordinal);
            }
        }

        Assert.Empty(logger.Logs);
    }
}
