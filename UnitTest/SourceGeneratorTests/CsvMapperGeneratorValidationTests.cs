using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.SourceGeneratorTests;

public class CsvMapperGeneratorValidationTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void Emits_range_helper_for_int_with_Range()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            using Sdp.Attributes;
            namespace Test;

            [StaticDataRecord("File", "Sheet")]
            public sealed partial record Foo(
                int Id,
                [Range(0, 1000000)] int Price);
            """;

        var (result, final) = SourceGeneratorTestHelper.RunWithFinal(source);

        var mapperTree = SourceGeneratorTestHelper.GetSingleTree(result, "CsvMapper.g.cs");

        var code = mapperTree.ToString();
        Assert.Contains("__ValidateRange_Price(int.Parse(", code);
        Assert.Contains("value < 0", code);
        Assert.Contains("value > 1000000", code);
        Assert.Contains("ArgumentOutOfRangeException", code);

        var errors = SourceGeneratorTestHelper.GetCompilationErrors(final);
        Assert.Empty(errors);

        logger.LogInformation("__ValidateRange_Price emitted, compilation errors: {ErrorCount}", errors.Count);
    }

    [Fact]
    public void Emits_range_helper_for_double_with_Range()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            using Sdp.Attributes;
            namespace Test;

            [StaticDataRecord("File", "Sheet")]
            public sealed partial record Foo(
                int Id,
                [Range(0.0, 1.0)] double Ratio);
            """;

        var (result, final) = SourceGeneratorTestHelper.RunWithFinal(source);

        var mapperTree = SourceGeneratorTestHelper.GetSingleTree(result, "CsvMapper.g.cs");

        var code = mapperTree.ToString();
        Assert.Contains("__ValidateRange_Ratio", code);
        Assert.Contains("private static double __ValidateRange_Ratio(double value)", code);

        var errors = SourceGeneratorTestHelper.GetCompilationErrors(final);
        Assert.Empty(errors);

        logger.LogInformation("__ValidateRange_Ratio emitted, compilation errors: {ErrorCount}", errors.Count);
    }

    [Fact]
    public void Skips_emit_when_Range_on_non_numeric_scalar()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            using Sdp.Attributes;
            namespace Test;

            [StaticDataRecord("File", "Sheet")]
            public sealed partial record Foo(
                int Id,
                [Range(0, 100)] string Name);
            """;

        var result = SourceGeneratorTestHelper.Run(source);

        Assert.Contains(result.Diagnostics, d => d.Id == "SDP0016" && d.Severity == DiagnosticSeverity.Error);

        var mapperTree = SourceGeneratorTestHelper.GetSingleTree(result, "CsvMapper.g.cs");
        Assert.Contains("NotSupportedException", mapperTree.ToString());

        logger.LogInformation("NotSupportedException fallback emitted: {FilePath}", mapperTree.FilePath);
    }

    [Fact]
    public void Emits_regex_helper_for_string_with_RegularExpression()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            using Sdp.Attributes;
            namespace Test;

            [StaticDataRecord("File", "Sheet")]
            public sealed partial record Foo(
                int Id,
                [RegularExpression(@"^.{1,50}$")] string Title);
            """;

        var (result, final) = SourceGeneratorTestHelper.RunWithFinal(source);

        var mapperTree = SourceGeneratorTestHelper.GetSingleTree(result, "CsvMapper.g.cs");

        var code = mapperTree.ToString();
        Assert.Contains("__Pattern_Title", code);
        Assert.Contains("__ValidatePattern_Title", code);
        Assert.Contains("IsMatch(value)", code);

        var errors = SourceGeneratorTestHelper.GetCompilationErrors(final);
        Assert.Empty(errors);

        logger.LogInformation("__ValidatePattern_Title emitted, compilation errors: {ErrorCount}", errors.Count);
    }

    [Fact]
    public void Reports_SDP0004_when_parameter_is_unsupported()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            using Sdp.Attributes;
            namespace Test;

            [StaticDataRecord("File", "Sheet")]
            public sealed partial record Foo(
                int Id,
                object Data);
            """;

        var result = SourceGeneratorTestHelper.Run(source);

        var diagnostic = Assert.Single(result.Diagnostics, d => d.Id == "SDP0004");
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);

        var mapperTree = SourceGeneratorTestHelper.GetSingleTree(result, "CsvMapper.g.cs");
        Assert.Contains("NotSupportedException", mapperTree.ToString());

        logger.LogInformation("Diagnostic {Id} reported with severity {Severity}.", diagnostic.Id, diagnostic.Severity);
    }

    [Fact]
    public void Emits_AllowThousands_for_floating_point_parse()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            using Sdp.Attributes;
            namespace Test;

            [StaticDataRecord("File", "Sheet")]
            public sealed partial record Foo(int Id, double Ratio, float Scale);
            """;

        var result = SourceGeneratorTestHelper.Run(source);

        var mapperTree = SourceGeneratorTestHelper.GetSingleTree(result, "CsvMapper.g.cs");

        var code = mapperTree.ToString();
        Assert.Contains("NumberStyles.Float | global::System.Globalization.NumberStyles.AllowThousands", code);

        logger.LogInformation("AllowThousands number style emitted: {FilePath}", mapperTree.FilePath);
    }

    [Fact]
    public void Emits_parsed_bound_range_helper_for_typed_DateTime_Range()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            using System;
            using Sdp.Attributes;
            namespace Test;

            [StaticDataRecord("File", "Sheet")]
            public sealed partial record Foo(
                int Id,
                [DateTimeFormat("yyyy-MM-dd")]
                [Range(typeof(DateTime), "2020-01-01", "2020-12-31")] DateTime Date);
            """;

        var (result, final) = SourceGeneratorTestHelper.RunWithFinal(source);

        var mapperTree = SourceGeneratorTestHelper.GetSingleTree(result, "CsvMapper.g.cs");

        var code = mapperTree.ToString();
        Assert.Contains("__RangeMin_Date", code);
        Assert.Contains("__RangeMax_Date", code);
        Assert.Contains("__ValidateRange_Date", code);
        Assert.DoesNotContain("NotSupportedException", code);

        var errors = SourceGeneratorTestHelper.GetCompilationErrors(final);
        Assert.Empty(errors);

        logger.LogInformation("__ValidateRange_Date emitted, compilation errors: {ErrorCount}", errors.Count);
    }

    [Fact]
    public void Combines_Range_with_NullString_for_nullable_int()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            using Sdp.Attributes;
            namespace Test;

            [StaticDataRecord("File", "Sheet")]
            public sealed partial record Foo(
                int Id,
                [NullString("-")][Range(0, 100)] int? Score);
            """;

        var (result, final) = SourceGeneratorTestHelper.RunWithFinal(source);

        var mapperTree = SourceGeneratorTestHelper.GetSingleTree(result, "CsvMapper.g.cs");

        var code = mapperTree.ToString();
        Assert.Contains("== \"-\"", code);
        Assert.Contains("private static int? __MapNullable_Score(string value)", code);
        Assert.Contains("__ValidateRange_Score", code);

        var errors = SourceGeneratorTestHelper.GetCompilationErrors(final);
        Assert.Empty(errors);

        logger.LogInformation("__ValidateRange_Score emitted, compilation errors: {ErrorCount}", errors.Count);
    }

    private TestOutputLogger<CsvMapperGeneratorValidationTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Information);
        if (factory.CreateLogger<CsvMapperGeneratorValidationTests>()
            is not TestOutputLogger<CsvMapperGeneratorValidationTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
