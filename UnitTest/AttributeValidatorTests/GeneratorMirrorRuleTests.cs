using Microsoft.Extensions.Logging;
using SchemaInfoScanner;
using SchemaInfoScanner.Catalogs;
using SchemaInfoScanner.Collectors;
using SchemaInfoScanner.Exceptions;
using SchemaInfoScanner.Schemata.TypedPropertySchemata;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.AttributeValidatorTests;

public partial class GeneratorMirrorRuleTests(ITestOutputHelper testOutputHelper)
{
    [Theory]
    [InlineData("[SingleColumnCollection(\",\")] [CountRange(3, 1)] ImmutableArray<int> Tags", "MinCount")]
    [InlineData("[Length(0)] ImmutableArray<int> Tags", "LengthAttribute")]
    [InlineData("[Length(2)] int Count", "LengthAttribute")]
    [InlineData("[Key] ImmutableArray<int> Ids", "KeyAttribute")]
    public void DeclarationRejectedLikeTheGenerator(string parameterDeclaration, string messagePart)
    {
        var logger = CreateLogger();

        // language=C#
        var code = $"""
                    [StaticDataRecord("Test", "TestSheet")]
                    public sealed partial record MyRecord({parameterDeclaration});
                    """;

        var ex = Assert.Throws<InvalidAttributeUsageException>(() => SimpleCordParser.Parse(code, logger));

        Assert.Contains(messagePart, ex.Message, StringComparison.Ordinal);
        Assert.Single(logger.Logs);
    }

    [Fact]
    public void DateTimeMapKey_MatchingValueKey_IsAccepted()
    {
        var logger = CreateLogger();

        // language=C#
        const string code = """
                            public sealed partial record Entry([Key] [DateTimeFormat("yyyy-MM-dd")] DateTime When, int Score);

                            [StaticDataRecord("Test", "TestSheet")]
                            public sealed partial record MyRecord([Key] int Id, [Length(2)] FrozenDictionary<DateTime, Entry> Entries);
                            """;

        SimpleCordParser.Parse(code, logger);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void MapKeyMember_NotFirstParameter_StillDetectsDuplicateKeys()
    {
        var logger = CreateLogger();

        // language=C#
        const string code = """
                            public sealed partial record Entry(int Score, [Key] string Subject);

                            [StaticDataRecord("Test", "TestSheet")]
                            public sealed partial record MyRecord([Length(2)] FrozenDictionary<string, Entry> Scores);
                            """;
        var loadResult = RecordSchemaLoader.OnLoad(code, logger);
        var recordSchemaCatalog = new RecordSchemaCatalog(new RecordSchemaSet(loadResult, logger));
        RecordComplianceChecker.Check(recordSchemaCatalog, logger);
        var catalogs = new MetadataCatalogs(recordSchemaCatalog, new EnumMemberCatalog(loadResult));
        var mapSchema = Assert.Single(recordSchemaCatalog.StaticDataRecordSchemata[0].PropertySchemata);

        var distinct = CompatibilityContext.CreateNoCollect(catalogs, [new("A1", "1"), new("B1", "Math"), new("C1", "2"), new("D1", "Art")]);
        mapSchema.CheckCompatibility(distinct);
        Assert.Equal(4, distinct.Position);

        var duplicate = CompatibilityContext.CreateNoCollect(catalogs, [new("A1", "1"), new("B1", "Math"), new("C1", "2"), new("D1", "Math")]);
        Assert.Throws<InvalidOperationException>(() => mapSchema.CheckCompatibility(duplicate));
        Assert.Empty(logger.Logs);
    }

    private TestOutputLogger<GeneratorMirrorRuleTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<GeneratorMirrorRuleTests>() is not TestOutputLogger<GeneratorMirrorRuleTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
