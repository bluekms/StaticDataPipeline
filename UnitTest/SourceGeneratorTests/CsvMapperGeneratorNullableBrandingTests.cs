using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.SourceGeneratorTests;

public class CsvMapperGeneratorNullableBrandingTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void Emits_null_branch_for_nullable_struct_branding()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            using Sdp.Attributes;
            namespace Test;

            public record struct QuestId(int Value);

            [StaticDataRecord("File", "Sheet")]
            public sealed partial record Foo(
                [Key] QuestId Id,
                [NullString("")] QuestId? PrevQuestId);
            """;

        var (result, final) = SourceGeneratorTestHelper.RunWithFinal(source);

        var code = SourceGeneratorTestHelper.GetSingleTree(result, "CsvMapper.g.cs").ToString();
        Assert.Contains("private static global::Test.QuestId? __MapNullableBranding_PrevQuestId(", code);
        Assert.Contains("PrevQuestId: __MapNullableBranding_PrevQuestId(headers, values, \"PrevQuestId\")", code);
        Assert.Contains("if (__raw == \"\")", code);
        Assert.Contains("return new global::Test.QuestId(global::Sdp.Csv.CsvMapperHelper.ParseInt32(__raw, __key));", code);

        Assert.Empty(SourceGeneratorTestHelper.GetCompilationWarningsAndErrors(final));
        Assert.Empty(result.Diagnostics);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void Emits_null_branch_for_nullable_class_branding()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            using Sdp.Attributes;
            namespace Test;

            public sealed record QuestId(int Value);

            [StaticDataRecord("File", "Sheet")]
            public sealed partial record Foo(
                [Key] QuestId Id,
                [NullString("-")] QuestId? PrevQuestId);
            """;

        var (result, final) = SourceGeneratorTestHelper.RunWithFinal(source);

        var code = SourceGeneratorTestHelper.GetSingleTree(result, "CsvMapper.g.cs").ToString();
        Assert.Contains("private static global::Test.QuestId? __MapNullableBranding_PrevQuestId(", code);
        Assert.Contains("if (__raw == \"-\")", code);

        Assert.Empty(SourceGeneratorTestHelper.GetCompilationWarningsAndErrors(final));
        Assert.Empty(result.Diagnostics);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void Leaves_non_nullable_branding_without_nullable_helper()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            using Sdp.Attributes;
            namespace Test;

            public record struct QuestId(int Value);

            [StaticDataRecord("File", "Sheet")]
            public sealed partial record Foo(
                [Key] QuestId Id,
                QuestId NextQuestId);
            """;

        var (result, final) = SourceGeneratorTestHelper.RunWithFinal(source);

        var code = SourceGeneratorTestHelper.GetSingleTree(result, "CsvMapper.g.cs").ToString();
        Assert.DoesNotContain("__MapNullableBranding_", code);

        Assert.Empty(SourceGeneratorTestHelper.GetCompilationWarningsAndErrors(final));
        Assert.Empty(logger.Logs);
    }

    [Theory]
    [InlineData("public record struct ItemId(int Value);")]
    [InlineData("public sealed record ItemId(int Value);")]
    public void Emits_nullable_element_type_for_array_and_set(string brandingDeclaration)
    {
        var logger = CreateLogger();

        // language=C#
        const string template = """
            using System.Collections.Frozen;
            using System.Collections.Immutable;
            using Sdp.Attributes;
            namespace Test;

            __BRANDING__

            [StaticDataRecord("File", "Sheet")]
            public sealed partial record Foo(
                int Id,
                [Length(2)] [NullString("")] ImmutableArray<ItemId?> Rewards,
                [NullString("-")] FrozenSet<ItemId?> Picks);
            """;
        var source = template.Replace("__BRANDING__", brandingDeclaration, StringComparison.Ordinal);

        var (result, final) = SourceGeneratorTestHelper.RunWithFinal(source);

        var code = SourceGeneratorTestHelper.GetSingleTree(result, "CsvMapper.g.cs").ToString();
        Assert.Contains("global::System.Collections.Immutable.ImmutableArray<global::Test.ItemId?> __MapArray_Rewards(", code);
        Assert.Contains("global::System.Collections.Frozen.FrozenSet<global::Test.ItemId?> __MapSet_Picks(", code);
        Assert.Contains("__MapNullableBranding_Rewards(headers, values, __k0)", code);
        Assert.Contains("__MapNullableBranding_Picks(headers, values, __ek)", code);

        Assert.Empty(SourceGeneratorTestHelper.GetCompilationWarningsAndErrors(final));
        Assert.Empty(result.Diagnostics);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void Compiles_under_csharp9_for_nullable_class_branding()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            using System.Collections.Immutable;
            using Sdp.Attributes;

            namespace Test
            {
                public sealed record QuestId(int Value);

                [StaticDataRecord("File", "Sheet")]
                public sealed partial record Foo(
                    [Key] QuestId Id,
                    [NullString("")] QuestId? PrevQuestId,
                    [Length(2)] [NullString("")] ImmutableArray<QuestId?> Related);
            }
            """;

        var output = SourceGeneratorTestHelper.RunWithFinal(source, LanguageVersion.CSharp9);

        Assert.Empty(output.Run.Diagnostics);
        Assert.Empty(SourceGeneratorTestHelper.GetCompilationErrors(output.Final));
        Assert.Empty(logger.Logs);
    }

    [Theory]
    [InlineData("public record struct QuestId(int Value);", "QuestId? PrevQuestId", "SDP0004")]
    [InlineData("public record struct QuestId([NullString(\"\")] int? Value);", "[NullString(\"\")] QuestId? PrevQuestId", "SDP0021")]
    [InlineData("public record struct QuestId([NullString(\"\")] int? Value);", "QuestId PrevQuestId", "SDP0021")]
    [InlineData("public sealed record QuestId([NullString(\"\")] string? Value);", "QuestId PrevQuestId", "SDP0021")]
    [InlineData("public record struct QuestId([NullString(\"\")] int? Value);", "[Length(2)] ImmutableArray<QuestId> PrevQuestId", "SDP0021")]
    [InlineData("public record struct QuestId([NullString(\"\")] int? Value);", "[Length(2)] FrozenSet<QuestId> PrevQuestId", "SDP0021")]
    [InlineData("public sealed record QuestId([Key] [NullString(\"\")] int? Value);", "[Length(2)] FrozenDictionary<int, QuestId> PrevQuestId", "SDP0022")]
    [InlineData("public sealed record QuestId(int X, int Y);", "QuestId? PrevQuestId", "SDP0004")]
    [InlineData("public sealed record QuestId(int X, int Y);", "[NullString(\"\")] QuestId? PrevQuestId", "SDP0016")]
    [InlineData("public sealed record QuestId(int Value, [Ignore] string Memo);", "[NullString(\"\")] QuestId? PrevQuestId", "SDP0016")]
    [InlineData("public record struct QuestId(int Value);", "[Length(2)] ImmutableArray<QuestId?> PrevQuestId", "SDP0004")]
    public void Rejects_nullable_record_that_is_not_a_null_string_branding(
        string brandingDeclaration,
        string parameter,
        string expectedDiagnosticId)
    {
        var logger = CreateLogger();

        // language=C#
        const string template = """
            using System.Collections.Frozen;
            using System.Collections.Immutable;
            using Sdp.Attributes;
            namespace Test;

            __BRANDING__

            [StaticDataRecord("File", "Sheet")]
            public sealed partial record Foo(
                int Id,
                __PARAMETER__);
            """;
        var source = template
            .Replace("__BRANDING__", brandingDeclaration, StringComparison.Ordinal)
            .Replace("__PARAMETER__", parameter, StringComparison.Ordinal);

        var result = SourceGeneratorTestHelper.Run(source);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(expectedDiagnosticId, diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        var mapperTree = SourceGeneratorTestHelper.GetSingleTree(result, "CsvMapper.g.cs");
        Assert.Contains("global::Sdp.Csv.CsvMapperHelper.MapperNotSupported(", mapperTree.ToString());
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void Emits_inner_value_display_for_nullable_struct_branding_foreign_key()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            using System.Collections.Immutable;
            using Microsoft.Extensions.Logging;
            using Sdp.Attributes;
            using Sdp.Manager;
            using Sdp.Table;
            namespace Test;

            public record struct QuestId(int Value);

            [StaticDataRecord("Quest", "Sheet")]
            public sealed partial record QuestRecord(
                [Key] QuestId Id,
                [NullString("")] [ForeignKey("Quest", "Id")] QuestId? PrevQuestId);

            public sealed partial class QuestTable(ImmutableArray<QuestRecord> records)
                : StaticDataTable<QuestRecord>(records);

            public sealed partial class QuestStaticData(ILogger logger)
                : StaticDataManager<QuestStaticData.TableSet>(logger)
            {
                public sealed partial record TableSet(QuestTable? Quest);
            }
            """;

        var (result, final) = SourceGeneratorTestHelper.RunWithFinal(source);

        var code = string.Join("\n", result.GeneratedTrees.Select(tree => tree.ToString()));
        Assert.Contains("{__v.Value.Value}", code);
        Assert.Contains("if (__v is not null)", code);

        Assert.Empty(SourceGeneratorTestHelper.GetCompilationWarningsAndErrors(final));
        Assert.Empty(result.Diagnostics);
        Assert.Empty(logger.Logs);
    }

    [Theory]
    [InlineData("public record struct ItemId(int Value);", "int?")]
    [InlineData("public sealed record ItemId(int Value);", "int?")]
    [InlineData("public record struct ItemId(string Value);", "string?")]
    [InlineData("public sealed record ItemId(string Value);", "string?")]
    public void Unwraps_nullable_branding_target_with_null_conditional_access(
        string brandingDeclaration,
        string foreignKeyType)
    {
        var logger = CreateLogger();

        // language=C#
        const string template = """
            using System.Collections.Immutable;
            using Microsoft.Extensions.Logging;
            using Sdp.Attributes;
            using Sdp.Manager;
            using Sdp.Table;
            namespace Test;

            __BRANDING__

            [StaticDataRecord("Item", "Sheet")]
            public sealed partial record ItemRecord(
                [Key] int Id,
                [NullString("")] ItemId? Alias);

            [StaticDataRecord("Usage", "Sheet")]
            public sealed partial record UsageRecord(
                [Key] int Id,
                [NullString("")] [ForeignKey("Items", "Alias")] __FK_TYPE__ AliasValue);

            public sealed partial class ItemTable(ImmutableArray<ItemRecord> records)
                : StaticDataTable<ItemRecord>(records);

            public sealed partial class UsageTable(ImmutableArray<UsageRecord> records)
                : StaticDataTable<UsageRecord>(records);

            public sealed partial class GameStaticData(ILogger logger)
                : StaticDataManager<GameStaticData.TableSet>(logger)
            {
                public sealed partial record TableSet(ItemTable? Items, UsageTable? Usages);
            }
            """;
        var source = template
            .Replace("__BRANDING__", brandingDeclaration, StringComparison.Ordinal)
            .Replace("__FK_TYPE__", foreignKeyType, StringComparison.Ordinal);

        var (result, final) = SourceGeneratorTestHelper.RunWithFinal(source);

        var code = string.Join("\n", result.GeneratedTrees.Select(tree => tree.ToString()));
        Assert.Contains(".Add((__t.Alias?.Value)!);", code);

        Assert.Empty(SourceGeneratorTestHelper.GetCompilationWarningsAndErrors(final));
        Assert.Empty(result.Diagnostics);
        Assert.Empty(logger.Logs);
    }

    [Theory]
    [InlineData("public record struct QuestId(int Value);")]
    [InlineData("public sealed record QuestId(int Value);")]
    public void Reports_SDP0022_when_key_is_nullable_branding(string brandingDeclaration)
    {
        var logger = CreateLogger();

        // language=C#
        const string template = """
            using Sdp.Attributes;
            namespace Test;

            __BRANDING__

            [StaticDataRecord("Quest", "Sheet")]
            public sealed partial record QuestRecord(
                [Key] [NullString("")] QuestId? Id,
                string Title);
            """;
        var source = template.Replace("__BRANDING__", brandingDeclaration, StringComparison.Ordinal);

        var result = SourceGeneratorTestHelper.Run(source);

        var diagnostics = result.Results.SelectMany(r => r.Diagnostics).ToList();
        Assert.Single(diagnostics, d => d.Id == "SDP0022");
        Assert.Empty(logger.Logs);
    }

    private TestOutputLogger<CsvMapperGeneratorNullableBrandingTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        var createdLogger = factory.CreateLogger<CsvMapperGeneratorNullableBrandingTests>();
        if (createdLogger is not TestOutputLogger<CsvMapperGeneratorNullableBrandingTests> logger)
        {
            throw new InvalidOperationException("로거를 만들지 못했다.");
        }

        return logger;
    }
}
