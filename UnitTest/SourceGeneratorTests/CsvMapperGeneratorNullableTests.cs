using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.SourceGeneratorTests;

public class CsvMapperGeneratorNullableTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void Emits_null_branch_for_nullable_string_with_NullString()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            using Sdp.Attributes;
            namespace Test;

            [StaticDataRecord("File", "Sheet")]
            public sealed partial record Foo(
                int Id,
                [NullString("")] string? Phone);
            """;

        var (result, final) = SourceGeneratorTestHelper.RunWithFinal(source);

        var mapperTree = SourceGeneratorTestHelper.GetSingleTree(result, "CsvMapper.g.cs");
        var code = mapperTree.ToString();
        Assert.Contains("== \"\"", code);
        Assert.Contains("private static string? __MapNullable_Phone(string value)", code);

        var errors = SourceGeneratorTestHelper.GetCompilationErrors(final);
        Assert.Empty(errors);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void Emits_null_branch_for_nullable_value_type_with_NullString()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            using Sdp.Attributes;
            namespace Test;

            [StaticDataRecord("File", "Sheet")]
            public sealed partial record Foo(
                int Id,
                [NullString("-")] int? Score);
            """;

        var (result, final) = SourceGeneratorTestHelper.RunWithFinal(source);

        var mapperTree = SourceGeneratorTestHelper.GetSingleTree(result, "CsvMapper.g.cs");
        var code = mapperTree.ToString();
        Assert.Contains("== \"-\"", code);
        Assert.Contains("private static int? __MapNullable_Score(string value)", code);

        var errors = SourceGeneratorTestHelper.GetCompilationErrors(final);
        Assert.Empty(errors);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void Skips_emit_for_nullable_without_NullString()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            using Sdp.Attributes;
            namespace Test;

            [StaticDataRecord("File", "Sheet")]
            public sealed partial record Foo(
                int Id,
                int? Score);
            """;

        var result = SourceGeneratorTestHelper.Run(source);
        Assert.Contains(result.Diagnostics, d => d.Id == "SDP0004" && d.Severity == DiagnosticSeverity.Error);

        var mapperTree = SourceGeneratorTestHelper.GetSingleTree(result, "CsvMapper.g.cs");
        Assert.Contains("NotSupportedException", mapperTree.ToString());
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void Escapes_special_characters_in_NullString_literal()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            using Sdp.Attributes;
            namespace Test;

            [StaticDataRecord("File", "Sheet")]
            public sealed partial record Foo(
                int Id,
                [NullString("\"NULL\"")] string? Note);
            """;

        var (result, final) = SourceGeneratorTestHelper.RunWithFinal(source);

        SourceGeneratorTestHelper.GetSingleTree(result, "CsvMapper.g.cs");

        var errors = SourceGeneratorTestHelper.GetCompilationErrors(final);
        Assert.Empty(errors);
        Assert.Empty(logger.Logs);
    }

    private TestOutputLogger<CsvMapperGeneratorNullableTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<CsvMapperGeneratorNullableTests>()
            is not TestOutputLogger<CsvMapperGeneratorNullableTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
