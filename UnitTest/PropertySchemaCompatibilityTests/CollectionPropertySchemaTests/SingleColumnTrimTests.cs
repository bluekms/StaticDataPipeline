using Microsoft.Extensions.Logging;
using SchemaInfoScanner;
using SchemaInfoScanner.Catalogs;
using SchemaInfoScanner.Collectors;
using SchemaInfoScanner.Schemata;
using SchemaInfoScanner.Schemata.TypedPropertySchemata;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.PropertySchemaCompatibilityTests.CollectionPropertySchemaTests;

public partial class SingleColumnTrimTests(ITestOutputHelper testOutputHelper)
{
    [Theory]
    [InlineData("[SingleColumnCollection(\",\")] ImmutableArray<Category> Property", "Weapon, Armor")]
    [InlineData("[SingleColumnCollection(\",\")] ImmutableArray<int> Property", " 1 , 2 ")]
    [InlineData("[SingleColumnCollection(\",\")] FrozenSet<int> Property", "1, 2")]
    [InlineData("[SingleColumnCollection(\",\")] [NullString(\"NULL\")] ImmutableArray<int?> Property", "1, NULL ,3")]
    [InlineData("[SingleColumnCollection(\",\")] [NullString(\"NULL\")] FrozenSet<int?> Property", " 1 ,NULL")]
    public void SurroundingSpaces_AreIgnored(string parameterDeclaration, string cellValue)
    {
        var logger = CreateLogger();
        var setup = CreateContext(parameterDeclaration, cellValue, logger);

        setup.PropertySchema.CheckCompatibility(setup.Context);
        Assert.Empty(logger.Logs);
    }

    [Theory]
    [InlineData("[SingleColumnCollection(\",\")] ImmutableArray<int> Property", "1, ,2")]
    [InlineData("[SingleColumnCollection(\",\")] ImmutableArray<int> Property", "1,,2")]
    public void BlankPiece_IsRejected(string parameterDeclaration, string cellValue)
    {
        var logger = CreateLogger();
        var setup = CreateContext(parameterDeclaration, cellValue, logger);

        Assert.Throws<InvalidOperationException>(() => setup.PropertySchema.CheckCompatibility(setup.Context));
        Assert.Empty(logger.Logs);
    }

    private sealed record CompatibilitySetup(PropertySchemaBase PropertySchema, CompatibilityContext Context);

    private static CompatibilitySetup CreateContext(string parameterDeclaration, string cellValue, ILogger logger)
    {
        // language=C#
        var code = $$"""
                    public enum Category
                    {
                        Weapon,
                        Armor,
                    }

                    [StaticDataRecord("Test", "TestSheet")]
                    public sealed partial record MyRecord(
                        {{parameterDeclaration}},
                    );
                    """;

        var loadResult = RecordSchemaLoader.OnLoad(code, logger);
        var recordSchemaSet = new RecordSchemaSet(loadResult, logger);
        var recordSchemaCatalog = new RecordSchemaCatalog(recordSchemaSet);
        var enumMemberCatalog = new EnumMemberCatalog(loadResult);
        RecordComplianceChecker.Check(recordSchemaCatalog, logger);
        var catalogs = new MetadataCatalogs(recordSchemaCatalog, enumMemberCatalog);

        var cells = new[] { new CellData("A1", cellValue) };
        var context = CompatibilityContext.CreateCollectKey(catalogs, cells);
        var propertySchema = Assert.Single(recordSchemaCatalog.StaticDataRecordSchemata[0].PropertySchemata);

        return new CompatibilitySetup(propertySchema, context);
    }

    private TestOutputLogger<SingleColumnTrimTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<SingleColumnTrimTests>() is not TestOutputLogger<SingleColumnTrimTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
