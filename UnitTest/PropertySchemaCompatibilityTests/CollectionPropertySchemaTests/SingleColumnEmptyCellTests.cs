using Microsoft.Extensions.Logging;
using SchemaInfoScanner;
using SchemaInfoScanner.Catalogs;
using SchemaInfoScanner.Collectors;
using SchemaInfoScanner.Schemata;
using SchemaInfoScanner.Schemata.TypedPropertySchemata;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.PropertySchemaCompatibilityTests.CollectionPropertySchemaTests;

public partial class SingleColumnEmptyCellTests(ITestOutputHelper testOutputHelper)
{
    [Theory]
    [InlineData("ImmutableArray<int>")]
    [InlineData("FrozenSet<int>")]
    public void EmptyCell_WithZeroLowerBound_IsAccepted(string collectionType)
    {
        var logger = CreateLogger();
        var context = CreateContext($"[SingleColumnCollection(\",\")] [CountRange(0, 3)] {collectionType} Property", string.Empty, logger);

        context.PropertySchema.CheckCompatibility(context.Context);
        Assert.Empty(logger.Logs);
    }

    [Theory]
    [InlineData("ImmutableArray<int>")]
    [InlineData("FrozenSet<int>")]
    public void EmptyCell_WithoutCountRange_IsAccepted(string collectionType)
    {
        var logger = CreateLogger();
        var context = CreateContext($"[SingleColumnCollection(\",\")] {collectionType} Property", string.Empty, logger);

        context.PropertySchema.CheckCompatibility(context.Context);
        Assert.Empty(logger.Logs);
    }

    [Theory]
    [InlineData("ImmutableArray<int>")]
    [InlineData("FrozenSet<int>")]
    public void WhitespaceOnlyCell_IsEmptyCollection(string collectionType)
    {
        var logger = CreateLogger();
        var context = CreateContext($"[SingleColumnCollection(\",\")] [CountRange(0, 3)] {collectionType} Property", "  ", logger);

        context.PropertySchema.CheckCompatibility(context.Context);
        Assert.Empty(logger.Logs);
    }

    [Theory]
    [InlineData("ImmutableArray<int>")]
    [InlineData("FrozenSet<int>")]
    public void EmptyCell_WithPositiveLowerBound_IsRejected(string collectionType)
    {
        var logger = CreateLogger();
        var context = CreateContext($"[SingleColumnCollection(\",\")] [CountRange(1, 3)] {collectionType} Property", string.Empty, logger);

        Assert.Throws<ArgumentOutOfRangeException>(() => context.PropertySchema.CheckCompatibility(context.Context));
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void EmptyCell_NullableElementWithEmptyNullString_IsOneNullElement()
    {
        var logger = CreateLogger();
        var context = CreateContext("[SingleColumnCollection(\",\")] [NullString(\"\")] [CountRange(1, 3)] ImmutableArray<int?> Property", string.Empty, logger);

        context.PropertySchema.CheckCompatibility(context.Context);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void EmptyCell_NullableElementWithNonEmptyNullString_IsEmptyCollection()
    {
        var logger = CreateLogger();
        var context = CreateContext("[SingleColumnCollection(\",\")] [NullString(\"NULL\")] [CountRange(1, 3)] ImmutableArray<int?> Property", string.Empty, logger);

        Assert.Throws<ArgumentOutOfRangeException>(() => context.PropertySchema.CheckCompatibility(context.Context));
        Assert.Empty(logger.Logs);
    }

    private sealed record CompatibilitySetup(PropertySchemaBase PropertySchema, CompatibilityContext Context);

    private static CompatibilitySetup CreateContext(string parameterDeclaration, string cellValue, ILogger logger)
    {
        // language=C#
        var code = $"""
                    [StaticDataRecord("Test", "TestSheet")]
                    public sealed partial record MyRecord(
                        {parameterDeclaration},
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

    private TestOutputLogger<SingleColumnEmptyCellTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<SingleColumnEmptyCellTests>() is not TestOutputLogger<SingleColumnEmptyCellTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
