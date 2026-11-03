using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.SourceGeneratorTests;

public class CsvMapperGeneratorCharRangeTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void Emits_parsed_bound_range_helper_for_typed_char_Range()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            using Sdp.Attributes;
            namespace Test;

            [StaticDataRecord("File", "Sheet")]
            public sealed partial record Foo(
                int Id,
                [Range(typeof(char), "A", "E")] char Rank);
            """;

        var (result, final) = SourceGeneratorTestHelper.RunWithFinal(source);
        Assert.Empty(result.Diagnostics);

        var mapperTree = SourceGeneratorTestHelper.GetSingleTree(result, "CsvMapper.g.cs");
        var code = mapperTree.ToString();
        Assert.Contains("private static readonly char __RangeMin_Rank = char.Parse(\"A\");", code);
        Assert.Contains("private static readonly char __RangeMax_Rank = char.Parse(\"E\");", code);
        Assert.Contains("__ValidateRange_Rank(global::Sdp.Csv.CsvMapperHelper.ParseChar(", code);
        Assert.Contains("global::Sdp.Csv.CsvMapperHelper.ValueOutOfRange(\"Rank\", \"A\", \"E\", value)", code);

        var errors = SourceGeneratorTestHelper.GetCompilationErrors(final);
        Assert.Empty(errors);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void Emits_range_helper_for_nullable_char_with_NullString()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            using Sdp.Attributes;
            namespace Test;

            [StaticDataRecord("File", "Sheet")]
            public sealed partial record Foo(
                int Id,
                [NullString("-")] [Range(typeof(char), "A", "E")] char? Rank);
            """;

        var (result, final) = SourceGeneratorTestHelper.RunWithFinal(source);
        Assert.Empty(result.Diagnostics);

        var mapperTree = SourceGeneratorTestHelper.GetSingleTree(result, "CsvMapper.g.cs");
        var code = mapperTree.ToString();
        Assert.Contains("private static char? __MapNullable_Rank(string value, string column)", code);
        Assert.Contains("__ValidateRange_Rank", code);

        var errors = SourceGeneratorTestHelper.GetCompilationErrors(final);
        Assert.Empty(errors);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void Emits_range_helper_for_char_element_collection()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            using System.Collections.Immutable;
            using Sdp.Attributes;
            namespace Test;

            [StaticDataRecord("File", "Sheet")]
            public sealed partial record Foo(
                int Id,
                [Length(2)] [Range(typeof(char), "A", "E")] ImmutableArray<char> Ranks);
            """;

        var (result, final) = SourceGeneratorTestHelper.RunWithFinal(source);
        Assert.Empty(result.Diagnostics);

        var mapperTree = SourceGeneratorTestHelper.GetSingleTree(result, "CsvMapper.g.cs");
        var code = mapperTree.ToString();
        Assert.Contains("private static readonly char __RangeMin_Ranks = char.Parse(\"A\");", code);
        Assert.Contains("__ValidateRange_Ranks(global::Sdp.Csv.CsvMapperHelper.ParseChar(", code);

        var errors = SourceGeneratorTestHelper.GetCompilationErrors(final);
        Assert.Empty(errors);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void Reports_SDP0016_when_numeric_Range_on_char()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            using Sdp.Attributes;
            namespace Test;

            [StaticDataRecord("File", "Sheet")]
            public sealed partial record Foo(
                int Id,
                [Range(65, 69)] char Rank);
            """;

        var result = SourceGeneratorTestHelper.Run(source);
        Assert.Contains(result.Diagnostics, d => d.Id == "SDP0016" && d.Severity == DiagnosticSeverity.Error);

        var mapperTree = SourceGeneratorTestHelper.GetSingleTree(result, "CsvMapper.g.cs");
        Assert.Contains("global::Sdp.Csv.CsvMapperHelper.MapperNotSupported(", mapperTree.ToString());
        Assert.Empty(logger.Logs);
    }

    [Theory]
    [InlineData("AB", "E")]
    [InlineData("A", "")]
    public void Reports_SDP0010_when_char_Range_bound_is_not_single_character(string minimum, string maximum)
    {
        var logger = CreateLogger();

        // language=C#
        var source = $$"""
            using Sdp.Attributes;
            namespace Test;

            [StaticDataRecord("File", "Sheet")]
            public sealed partial record Foo(
                int Id,
                [Range(typeof(char), "{{minimum}}", "{{maximum}}")] char Rank);
            """;

        var result = SourceGeneratorTestHelper.Run(source);
        var diagnostic = Assert.Single(result.Diagnostics, d => d.Id == "SDP0010");
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);

        var mapperTree = SourceGeneratorTestHelper.GetSingleTree(result, "CsvMapper.g.cs");
        Assert.Contains("global::Sdp.Csv.CsvMapperHelper.MapperNotSupported(", mapperTree.ToString());
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void Reports_SDP0015_when_char_Range_minimum_exceeds_maximum()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            using Sdp.Attributes;
            namespace Test;

            [StaticDataRecord("File", "Sheet")]
            public sealed partial record Foo(
                int Id,
                [Range(typeof(char), "E", "A")] char Rank);
            """;

        var result = SourceGeneratorTestHelper.Run(source);
        var diagnostic = Assert.Single(result.Diagnostics, d => d.Id == "SDP0015");
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Empty(logger.Logs);
    }

    private TestOutputLogger<CsvMapperGeneratorCharRangeTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<CsvMapperGeneratorCharRangeTests>()
            is not TestOutputLogger<CsvMapperGeneratorCharRangeTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
