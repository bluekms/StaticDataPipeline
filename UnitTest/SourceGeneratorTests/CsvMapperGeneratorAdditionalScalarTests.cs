using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.SourceGeneratorTests;

public class CsvMapperGeneratorAdditionalScalarTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void Emits_char_parse()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            using Sdp.Attributes;
            namespace Test;

            [StaticDataRecord("File", "Sheet")]
            public sealed partial record Foo(int Id, char Grade);
            """;

        var (result, final) = SourceGeneratorTestHelper.RunWithFinal(source);

        var mapperTree = SourceGeneratorTestHelper.GetSingleTree(result, "CsvMapper.g.cs");

        Assert.Contains("char.Parse(", mapperTree.ToString());

        var errors = SourceGeneratorTestHelper.GetCompilationErrors(final);
        Assert.Empty(errors);

        logger.LogInformation("char.Parse emitted, compilation errors: {ErrorCount}", errors.Count);
    }

    [Fact]
    public void Emits_ParseExact_for_DateOnly_with_DateTimeFormat()
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
                [DateTimeFormat("yyyy-MM-dd")] DateOnly Date);
            """;

        var (result, final) = SourceGeneratorTestHelper.RunWithFinal(source);

        var mapperTree = SourceGeneratorTestHelper.GetSingleTree(result, "CsvMapper.g.cs");

        Assert.Contains("System.DateOnly.ParseExact(", mapperTree.ToString());

        var errors = SourceGeneratorTestHelper.GetCompilationErrors(final);
        Assert.Empty(errors);

        logger.LogInformation("DateOnly.ParseExact emitted, compilation errors: {ErrorCount}", errors.Count);
    }

    [Fact]
    public void Emits_ParseExact_for_TimeOnly_with_DateTimeFormat()
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
                [DateTimeFormat("HH:mm")] TimeOnly Open);
            """;

        var (result, final) = SourceGeneratorTestHelper.RunWithFinal(source);

        var mapperTree = SourceGeneratorTestHelper.GetSingleTree(result, "CsvMapper.g.cs");

        Assert.Contains("System.TimeOnly.ParseExact(", mapperTree.ToString());

        var errors = SourceGeneratorTestHelper.GetCompilationErrors(final);
        Assert.Empty(errors);

        logger.LogInformation("TimeOnly.ParseExact emitted, compilation errors: {ErrorCount}", errors.Count);
    }

    [Fact]
    public void Skips_emit_for_DateOnly_without_DateTimeFormat()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            using System;
            using Sdp.Attributes;
            namespace Test;

            [StaticDataRecord("File", "Sheet")]
            public sealed partial record Foo(int Id, DateOnly Date);
            """;

        var result = SourceGeneratorTestHelper.Run(source);

        Assert.Contains(result.Diagnostics, d => d.Id == "SDP0004" && d.Severity == DiagnosticSeverity.Error);

        var mapperTree = SourceGeneratorTestHelper.GetSingleTree(result, "CsvMapper.g.cs");
        Assert.Contains("NotSupportedException", mapperTree.ToString());

        logger.LogInformation("NotSupportedException fallback emitted: {FilePath}", mapperTree.FilePath);
    }

    [Fact]
    public void Emits_enum_element_helper_for_collection_of_enum()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            using System.Collections.Immutable;
            using Sdp.Attributes;
            namespace Test;

            public enum Color { Red, Green, Blue }

            [StaticDataRecord("File", "Sheet")]
            public sealed partial record Foo(
                int Id,
                [Length(2)] ImmutableArray<Color> Colors);
            """;

        var (result, final) = SourceGeneratorTestHelper.RunWithFinal(source);

        var mapperTree = SourceGeneratorTestHelper.GetSingleTree(result, "CsvMapper.g.cs");

        var code = mapperTree.ToString();
        Assert.Contains("__MapElementEnum_1_Color", code);
        Assert.DoesNotContain("NotSupportedException", code);

        var errors = SourceGeneratorTestHelper.GetCompilationErrors(final);
        Assert.Empty(errors);

        logger.LogInformation("__MapElementEnum_1_Color emitted, compilation errors: {ErrorCount}", errors.Count);
    }

    [Fact]
    public void Emits_element_range_validation_for_collection_with_Range()
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
                [Length(2)][Range(1, 100)] ImmutableArray<int> Scores);
            """;

        var (result, final) = SourceGeneratorTestHelper.RunWithFinal(source);

        var mapperTree = SourceGeneratorTestHelper.GetSingleTree(result, "CsvMapper.g.cs");

        var code = mapperTree.ToString();
        Assert.Contains("__ValidateRange_Scores", code);
        Assert.DoesNotContain("NotSupportedException", code);

        var errors = SourceGeneratorTestHelper.GetCompilationErrors(final);
        Assert.Empty(errors);

        logger.LogInformation("__ValidateRange_Scores emitted, compilation errors: {ErrorCount}", errors.Count);
    }

    [Fact]
    public void Reports_SDP0005_when_CountRange_used_with_Length()
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
                [Length(4)][CountRange(3, 5)] ImmutableArray<int> Values);
            """;

        var result = SourceGeneratorTestHelper.Run(source);

        var diagnostic = Assert.Single(result.Diagnostics, d => d.Id == "SDP0005");
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);

        logger.LogInformation("Diagnostic {Id} reported with severity {Severity}.", diagnostic.Id, diagnostic.Severity);
    }

    [Fact]
    public void Reports_SDP0007_when_CountRange_without_SingleColumnCollection()
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
                [CountRange(1, 3)] ImmutableArray<int> Values);
            """;

        var result = SourceGeneratorTestHelper.Run(source);

        var diagnostic = Assert.Single(result.Diagnostics, d => d.Id == "SDP0007");
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);

        logger.LogInformation("Diagnostic {Id} reported with severity {Severity}.", diagnostic.Id, diagnostic.Severity);
    }

    [Fact]
    public void Does_not_report_CountRange_diagnostics_with_SingleColumnCollection()
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
                [SingleColumnCollection(",")][CountRange(1, 3)] ImmutableArray<int> Values);
            """;

        var result = SourceGeneratorTestHelper.Run(source);

        Assert.DoesNotContain(result.Diagnostics, d => d.Id == "SDP0005");
        Assert.DoesNotContain(result.Diagnostics, d => d.Id == "SDP0007");

        logger.LogInformation(
            "Reported diagnostics: [{Ids}]",
            string.Join(", ", result.Diagnostics.Select(d => d.Id).Distinct()));
    }

    private TestOutputLogger<CsvMapperGeneratorAdditionalScalarTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Information);
        if (factory.CreateLogger<CsvMapperGeneratorAdditionalScalarTests>()
            is not TestOutputLogger<CsvMapperGeneratorAdditionalScalarTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
