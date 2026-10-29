using Microsoft.Extensions.Logging;
using SchemaInfoScanner;
using SchemaInfoScanner.Catalogs;
using SchemaInfoScanner.Collectors;
using SchemaInfoScanner.Exceptions;
using SchemaInfoScanner.Schemata.TypedPropertySchemata;
using SchemaInfoScanner.Schemata.TypedPropertySchemata.CollectionTypes;
using SchemaInfoScanner.Schemata.TypedPropertySchemata.PrimitiveTypes.NullableTypes;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.RecordScanTests;

public partial class SymbolNameTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void NestedBlockNamespaces_ResolveInDeclarationOrder()
    {
        var logger = CreateLogger();

        // language=C#
        const string code = """
                            namespace Game
                            {
                                namespace Items
                                {
                                    public sealed partial record Inner(int Value);

                                    [StaticDataRecord("Book", "Items")]
                                    public sealed partial record ItemRecord([Key] int Id, Inner Inner);
                                }
                            }
                            """;
        var parseResult = SimpleCordParser.Parse(code, logger);

        Assert.Equal("Game.Items.ItemRecord", parseResult.RawRecordSchemata[0].RecordName.FullName);
        Assert.Equal(["Id", "Inner"], RecordFlattener.Flatten(parseResult.RawRecordSchemata[0], parseResult.RecordSchemaCatalog, logger));
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void NestedEnum_IsFoundForCellCheck()
    {
        var logger = CreateLogger();

        // language=C#
        const string code = """
                            [StaticDataRecord("Book", "Heroes")]
                            public sealed partial record HeroRecord([Key] int Id, HeroRecord.Kind Class)
                            {
                                public enum Kind { Warrior, Archer }
                            }
                            """;
        var loadResult = RecordSchemaLoader.OnLoad(code, logger);
        var recordSchemaCatalog = new RecordSchemaCatalog(new RecordSchemaSet(loadResult, logger));
        RecordComplianceChecker.Check(recordSchemaCatalog, logger);
        var catalogs = new MetadataCatalogs(recordSchemaCatalog, new EnumMemberCatalog(loadResult));
        var kindSchema = recordSchemaCatalog.StaticDataRecordSchemata[0].PropertySchemata[1];

        kindSchema.CheckCompatibility(CompatibilityContext.CreateNoCollect(catalogs, [new CellData("B2", "Archer")]));
        Assert.Throws<InvalidOperationException>(() => kindSchema.CheckCompatibility(CompatibilityContext.CreateNoCollect(catalogs, [new CellData("B3", "Mage")])));
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void SameSimpleNameInDifferentContainers_IsAllowed()
    {
        var logger = CreateLogger();

        // language=C#
        const string code = """
                            public static partial class A
                            {
                                public sealed partial record Item(int Value);
                            }

                            public static partial class B
                            {
                                public sealed partial record Item(string Label);
                            }

                            [StaticDataRecord("Book", "Pairs")]
                            public sealed partial record PairRecord([Key] int Id, A.Item First, B.Item Second);
                            """;
        var parseResult = SimpleCordParser.Parse(code, logger);

        Assert.Equal(["Id", "First", "Second"], RecordFlattener.Flatten(parseResult.RawRecordSchemata[0], parseResult.RecordSchemaCatalog, logger));
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void NullableString_RequiresNullString_AndBecomesNullableSchema()
    {
        var logger = CreateLogger();

        // language=C#
        const string missing = """
                               [StaticDataRecord("Book", "Items")]
                               public sealed partial record ItemRecord([Key] int Id, string? Name);
                               """;
        Assert.Throws<InvalidAttributeUsageException>(() => SimpleCordParser.Parse(missing, logger));
        Assert.Single(logger.Logs);
        logger.Logs.Clear();

        // language=C#
        const string declared = """
                                [StaticDataRecord("Book", "Items")]
                                public sealed partial record ItemRecord([Key] int Id, [NullString("-")] string? Name);
                                """;
        var parseResult = SimpleCordParser.Parse(declared, logger);
        Assert.IsType<NullableStringPropertySchema>(parseResult.RawRecordSchemata[0].PropertySchemata[1]);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void NullableStringKey_IsRejected()
    {
        var logger = CreateLogger();

        // language=C#
        const string code = """
                            [StaticDataRecord("Book", "Items")]
                            public sealed partial record ItemRecord([Key] [NullString("-")] string? Id);
                            """;

        Assert.Throws<InvalidAttributeUsageException>(() => SimpleCordParser.Parse(code, logger));
        Assert.Single(logger.Logs);
    }

    [Fact]
    public void NamespaceQualifiedCollection_IsRecognized()
    {
        var logger = CreateLogger();

        // language=C#
        const string code = """
                            [StaticDataRecord("Book", "Items")]
                            public sealed partial record ItemRecord([Key] int Id, [Length(2)] System.Collections.Immutable.ImmutableArray<int> Scores);
                            """;
        var parseResult = SimpleCordParser.Parse(code, logger);

        Assert.IsType<PrimitiveArrayPropertySchema>(parseResult.RawRecordSchemata[0].PropertySchemata[1]);
        Assert.Empty(logger.Logs);
    }

    private TestOutputLogger<SymbolNameTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<SymbolNameTests>() is not TestOutputLogger<SymbolNameTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
