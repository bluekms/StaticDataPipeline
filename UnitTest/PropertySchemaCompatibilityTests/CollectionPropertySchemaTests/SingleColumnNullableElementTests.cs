using Microsoft.Extensions.Logging;
using SchemaInfoScanner;
using SchemaInfoScanner.Catalogs;
using SchemaInfoScanner.Collectors;
using SchemaInfoScanner.Schemata;
using SchemaInfoScanner.Schemata.TypedPropertySchemata;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.PropertySchemaCompatibilityTests.CollectionPropertySchemaTests;

public partial class SingleColumnNullableElementTests(ITestOutputHelper testOutputHelper)
{
    [Theory]
    [InlineData("ImmutableArray<int?>")]
    [InlineData("FrozenSet<int?>")]
    public void NullPart_ConsumesOnlyItsOwnCell(string collectionType)
    {
        var logger = CreateLogger();
        var setup = CreateContext($"[SingleColumnCollection(\",\")] [NullString(\"-\")] {collectionType} Tags", "1,-,2", logger);

        setup.Schemata[0].CheckCompatibility(setup.Context);
        Assert.Equal(1, setup.Context.Position);

        setup.Schemata[1].CheckCompatibility(setup.Context);
        Assert.Equal(2, setup.Context.Position);
        Assert.Empty(logger.Logs);
    }

    [Theory]
    [InlineData("1,1")]
    [InlineData("-,-")]
    public void Set_DuplicateParts_AreRejected(string cellValue)
    {
        var logger = CreateLogger();
        var setup = CreateContext("[SingleColumnCollection(\",\")] [NullString(\"-\")] FrozenSet<int?> Tags", cellValue, logger);

        Assert.Throws<InvalidOperationException>(() => setup.Schemata[0].CheckCompatibility(setup.Context));
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void Array_DuplicateParts_AreAccepted()
    {
        var logger = CreateLogger();
        var setup = CreateContext("[SingleColumnCollection(\",\")] [NullString(\"-\")] ImmutableArray<int?> Tags", "1,1,-,-", logger);

        setup.Schemata[0].CheckCompatibility(setup.Context);
        Assert.Equal(1, setup.Context.Position);
        Assert.Empty(logger.Logs);
    }

    private sealed record CompatibilitySetup(IReadOnlyList<PropertySchemaBase> Schemata, CompatibilityContext Context);

    private static CompatibilitySetup CreateContext(string parameterDeclaration, string cellValue, ILogger logger)
    {
        // language=C#
        var code = $"""
                    [StaticDataRecord("Test", "TestSheet")]
                    public sealed partial record MyRecord(
                        {parameterDeclaration},
                        string Name
                    );
                    """;

        var loadResult = RecordSchemaLoader.OnLoad(code, logger);
        var recordSchemaSet = new RecordSchemaSet(loadResult, logger);
        var recordSchemaCatalog = new RecordSchemaCatalog(recordSchemaSet);
        var enumMemberCatalog = new EnumMemberCatalog(loadResult);
        RecordComplianceChecker.Check(recordSchemaCatalog, logger);
        var catalogs = new MetadataCatalogs(recordSchemaCatalog, enumMemberCatalog);

        var cells = new[] { new CellData("A1", cellValue), new CellData("B1", "Potion") };
        var context = CompatibilityContext.CreateNoCollect(catalogs, cells);

        return new CompatibilitySetup(recordSchemaCatalog.StaticDataRecordSchemata[0].PropertySchemata, context);
    }

    private TestOutputLogger<SingleColumnNullableElementTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<SingleColumnNullableElementTests>() is not TestOutputLogger<SingleColumnNullableElementTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
