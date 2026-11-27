using Microsoft.Extensions.Logging;
using SchemaInfoScanner;
using SchemaInfoScanner.Exceptions;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.RecordScanTests;

public class NullableBrandingScanTests(ITestOutputHelper testOutputHelper)
{
    [Theory]
    [InlineData("public record struct QuestId(int Value);")]
    [InlineData("public sealed record QuestId(int Value);")]
    public void AcceptsNullableBrandingWithNullString(string brandingDeclaration)
    {
        var logger = CreateLogger();

        // language=C#
        const string template = """
            __BRANDING__

            [StaticDataRecord("Test", "TestSheet")]
            public sealed partial record MyRecord(
                [Key] QuestId Id,
                [NullString("")] QuestId? PrevQuestId,
                [Length(2)] [NullString("")] ImmutableArray<QuestId?> Related,
                [Length(2)] [NullString("-")] FrozenSet<QuestId?> Unlocks
            );
            """;
        var code = template.Replace("__BRANDING__", brandingDeclaration, StringComparison.Ordinal);

        var parseResult = SimpleCordParser.Parse(code, logger);

        var headers = RecordFlattener.Flatten(
            parseResult.RawRecordSchemata[0],
            parseResult.RecordSchemaCatalog,
            logger);

        Assert.Equal(["Id", "PrevQuestId", "Related[0]", "Related[1]", "Unlocks[0]", "Unlocks[1]"], headers);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void AcceptsNullableBrandingInsideNestedRecordAndMapValue()
    {
        var logger = CreateLogger();

        // language=C#
        const string code = """
            public record struct QuestId(int Value);

            public sealed record Reward([NullString("")] QuestId? NextQuest, int Count);

            public sealed record Stage([Key] int Order, [NullString("-")] QuestId? Unlock);

            [StaticDataRecord("Test", "TestSheet")]
            public sealed partial record MyRecord(
                int Id,
                Reward Reward,
                [Length(1)] FrozenDictionary<int, Stage> Stages
            );
            """;

        var parseResult = SimpleCordParser.Parse(code, logger);

        var headers = RecordFlattener.Flatten(
            parseResult.RawRecordSchemata[0],
            parseResult.RecordSchemaCatalog,
            logger);

        Assert.Equal(["Id", "Reward.NextQuest", "Reward.Count", "Stages[0].Order", "Stages[0].Unlock"], headers);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void TreatsNullableOfRecordStructWrittenWithoutQuestionMarkAsNullable()
    {
        var logger = CreateLogger();

        // language=C#
        const string code = """
            public record struct QuestId(int Value);

            [StaticDataRecord("Test", "TestSheet")]
            public sealed partial record MyRecord(
                int Id,
                [NullString("")] System.Nullable<QuestId> PrevQuestId
            );
            """;

        var parseResult = SimpleCordParser.Parse(code, logger);

        var headers = RecordFlattener.Flatten(
            parseResult.RawRecordSchemata[0],
            parseResult.RecordSchemaCatalog,
            logger);

        Assert.Equal(["Id", "PrevQuestId"], headers);
        Assert.Empty(logger.Logs);
    }

    [Theory]
    [InlineData("System.Nullable<QuestId> PrevQuestId")]
    [InlineData("QuestId? PrevQuestId")]
    [InlineData("[Length(2)] ImmutableArray<QuestId?> PrevQuestId")]
    [InlineData("[Length(2)] FrozenSet<QuestId?> PrevQuestId")]
    public void RejectsNullableBrandingWithoutNullString(string parameter)
    {
        var logger = CreateLogger();

        // language=C#
        const string template = """
            public record struct QuestId(int Value);

            [StaticDataRecord("Test", "TestSheet")]
            public sealed partial record MyRecord(
                int Id,
                __PARAMETER__
            );
            """;
        var code = template.Replace("__PARAMETER__", parameter, StringComparison.Ordinal);

        var ex = Assert.Throws<InvalidAttributeUsageException>(() => SimpleCordParser.Parse(code, logger));

        Assert.Contains("NullStringAttribute", ex.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("NpcId Giver")]
    [InlineData("[Length(2)] ImmutableArray<NpcId> Giver")]
    [InlineData("[Length(2)] FrozenSet<NpcId> Giver")]
    [InlineData("[NullString(\"\")] NpcId? Giver")]
    [InlineData("[Length(2)] [NullString(\"\")] ImmutableArray<NpcId?> Giver")]
    public void RejectsSingleValueRecordWhoseInnerValueIsNullable(string parameter)
    {
        var logger = CreateLogger();

        // language=C#
        const string template = """
            public record struct NpcId([NullString("-")] int? Value);

            [StaticDataRecord("Test", "TestSheet")]
            public sealed partial record MyRecord(
                int Id,
                __PARAMETER__
            );
            """;
        var code = template.Replace("__PARAMETER__", parameter, StringComparison.Ordinal);

        var ex = Assert.Throws<NotSupportedException>(() => SimpleCordParser.Parse(code, logger));

        Assert.Contains("MyRecord.Giver", ex.Message, StringComparison.Ordinal);
        Assert.Contains("NpcId.Value", ex.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("[NullString(\"\")] Position? Point")]
    [InlineData("[Length(2)] [NullString(\"\")] ImmutableArray<Position?> Point")]
    [InlineData("[Length(2)] [NullString(\"\")] FrozenSet<Position?> Point")]
    public void RejectsNullableRecordWithSeveralParameters(string parameter)
    {
        var logger = CreateLogger();

        // language=C#
        const string template = """
            public sealed record Position(int X, int Y);

            [StaticDataRecord("Test", "TestSheet")]
            public sealed partial record MyRecord(
                int Id,
                __PARAMETER__
            );
            """;
        var code = template.Replace("__PARAMETER__", parameter, StringComparison.Ordinal);

        Assert.ThrowsAny<Exception>(() => SimpleCordParser.Parse(code, logger));
    }

    [Fact]
    public void RejectsNullableRecordWithIgnoredSecondParameter()
    {
        var logger = CreateLogger();

        // language=C#
        const string code = """
            public sealed record QuestId(int Value, [Ignore] string Memo);

            [StaticDataRecord("Test", "TestSheet")]
            public sealed partial record MyRecord(
                int Id,
                [NullString("")] QuestId? PrevQuestId
            );
            """;

        Assert.ThrowsAny<Exception>(() => SimpleCordParser.Parse(code, logger));
    }

    [Fact]
    public void DoesNotFoldHeaderOfRecordWithIgnoredSecondParameter()
    {
        var logger = CreateLogger();

        // language=C#
        const string code = """
            public sealed record QuestId(int Value, [Ignore] string Memo);

            [StaticDataRecord("Test", "TestSheet")]
            public sealed partial record MyRecord(
                int Id,
                QuestId PrevQuestId
            );
            """;

        var parseResult = SimpleCordParser.Parse(code, logger);

        var headers = RecordFlattener.Flatten(
            parseResult.RawRecordSchemata[0],
            parseResult.RecordSchemaCatalog,
            logger);

        Assert.Equal(["Id", "PrevQuestId.Value"], headers);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void RejectsNullableBrandingAsKey()
    {
        var logger = CreateLogger();

        // language=C#
        const string code = """
            public record struct QuestId(int Value);

            [StaticDataRecord("Test", "TestSheet")]
            public sealed partial record MyRecord(
                [Key] [NullString("")] QuestId? Id
            );
            """;

        Assert.Throws<InvalidAttributeUsageException>(() => SimpleCordParser.Parse(code, logger));
    }

    private TestOutputLogger<NullableBrandingScanTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        var createdLogger = factory.CreateLogger<NullableBrandingScanTests>();
        if (createdLogger is not TestOutputLogger<NullableBrandingScanTests> logger)
        {
            throw new InvalidOperationException("로거를 만들지 못했다.");
        }

        return logger;
    }
}
