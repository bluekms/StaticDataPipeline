using Microsoft.Extensions.Logging;
using SchemaInfoScanner;
using SchemaInfoScanner.Catalogs;
using SchemaInfoScanner.Collectors;
using SchemaInfoScanner.Extensions;
using SchemaInfoScanner.Schemata;
using SchemaInfoScanner.Schemata.TypedPropertySchemata;
using SchemaInfoScanner.Schemata.TypedPropertySchemata.CollectionTypes;
using Sdp.Attributes;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.RecordScanTests;

public partial class AttributeArgumentEvaluationTests(ITestOutputHelper testOutputHelper)
{
    [Theory]
    [InlineData("[RegularExpression(\"^\\\\d+$\")] string Code", "123", true)]
    [InlineData("[RegularExpression(\"^\\\\d+$\")] string Code", "abc", false)]
    [InlineData("[RegularExpression(@\"^\\d+$\")] string Code", "123", true)]
    [InlineData("[TimeSpanFormat(\"hh\\\\:mm\")] TimeSpan Duration", "01:30", true)]
    [InlineData("[Range(-1, 5)] int Delta", "-1", true)]
    [InlineData("[Range(-1, 5)] int Delta", "-2", false)]
    [InlineData("[SingleColumnCollection] ImmutableArray<int> Tags", "1,2", true)]
    [InlineData("[SingleColumnCollection(\"\\t\")] ImmutableArray<int> Tags", "1\t2", true)]
    public void ConstantArguments_AreEvaluatedLikeTheCompiler(string parameterDeclaration, string cellValue, bool accepted)
    {
        var logger = CreateLogger();
        var setup = CreateSetup(parameterDeclaration, logger);
        var context = CompatibilityContext.CreateNoCollect(setup.Catalogs, [new CellData("A1", cellValue)]);

        var ex = Record.Exception(() => setup.Schema.CheckCompatibility(context));

        Assert.Equal(accepted, ex is null);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void OmittedArgument_UsesAttributeDefault()
    {
        var logger = CreateLogger();
        var setup = CreateSetup("[SingleColumnCollection] ImmutableArray<int> Tags", logger);

        var schema = Assert.IsType<SingleColumnPrimitiveArrayPropertySchema>(setup.Schema);
        Assert.Equal(",", schema.Separator);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void ConstAndNameofAndFullAttributeName_AreUnderstood()
    {
        var logger = CreateLogger();

        // language=C#
        const string code = """
                            public static class Names
                            {
                                public const string Sheet = "Items";
                            }

                            [StaticDataRecord("Book", Names.Sheet)]
                            public sealed partial record MyRecord(
                                [KeyAttribute] int Id,
                                [ColumnName(nameof(MyRecord))] string Name
                            );
                            """;
        var parseResult = SimpleCordParser.Parse(code, logger);
        var recordSchema = parseResult.RawRecordSchemata[0];

        Assert.Equal(["Book", "Items"], recordSchema.GetAttributeValueList<StaticDataRecordAttribute>());
        Assert.True(recordSchema.PropertySchemata[0].HasAttribute<KeyAttribute>());
        Assert.True(recordSchema.PropertySchemata[1].TryGetAttributeValue<ColumnNameAttribute, string>(out var columnName));
        Assert.Equal("MyRecord", columnName);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void EnumMemberBound_KeepsMemberNameAsText()
    {
        var logger = CreateLogger();

        // language=C#
        const string code = """
                            public enum Grade { A, B, C, D }

                            [StaticDataRecord("Book", "Items")]
                            public sealed partial record MyRecord([Range(Grade.A, Grade.C)] Grade Value);
                            """;
        var parseResult = SimpleCordParser.Parse(code, logger);

        Assert.Equal(["A", "C"], parseResult.RawRecordSchemata[0].PropertySchemata[0].GetAttributeValueList<RangeAttribute>());
        Assert.Empty(logger.Logs);
    }

    private sealed record Setup(PropertySchemaBase Schema, MetadataCatalogs Catalogs);

    private static Setup CreateSetup(string parameterDeclaration, ILogger logger)
    {
        // language=C#
        var code = $"""
                    [StaticDataRecord("Test", "TestSheet")]
                    public sealed partial record MyRecord(
                        {parameterDeclaration}
                    );
                    """;

        var loadResult = RecordSchemaLoader.OnLoad(code, logger);
        var recordSchemaSet = new RecordSchemaSet(loadResult, logger);
        var recordSchemaCatalog = new RecordSchemaCatalog(recordSchemaSet);
        RecordComplianceChecker.Check(recordSchemaCatalog, logger);
        var catalogs = new MetadataCatalogs(recordSchemaCatalog, new EnumMemberCatalog(loadResult));

        return new Setup(Assert.Single(recordSchemaCatalog.StaticDataRecordSchemata[0].PropertySchemata), catalogs);
    }

    private TestOutputLogger<AttributeArgumentEvaluationTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<AttributeArgumentEvaluationTests>() is not TestOutputLogger<AttributeArgumentEvaluationTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
