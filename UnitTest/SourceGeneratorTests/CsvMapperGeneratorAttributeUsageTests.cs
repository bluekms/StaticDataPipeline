using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.SourceGeneratorTests;

public class CsvMapperGeneratorAttributeUsageTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void Reports_SDP0014_when_Length_is_zero()
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
                [Length(0)] ImmutableArray<int> Grades);
            """;

        var result = SourceGeneratorTestHelper.Run(source);

        var diagnostic = Assert.Single(result.Diagnostics, d => d.Id == "SDP0014");
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);

        logger.LogInformation("Diagnostic {Id} reported: {Message}", diagnostic.Id, diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Reports_SDP0015_when_Range_minimum_exceeds_maximum()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            using Sdp.Attributes;
            namespace Test;

            [StaticDataRecord("File", "Sheet")]
            public sealed partial record Foo(
                int Id,
                [Range(10, 0)] int Score);
            """;

        var result = SourceGeneratorTestHelper.Run(source);

        var diagnostic = Assert.Single(result.Diagnostics, d => d.Id == "SDP0015");
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);

        logger.LogInformation("Diagnostic {Id} reported: {Message}", diagnostic.Id, diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Reports_SDP0015_when_CountRange_minimum_exceeds_maximum()
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
                [SingleColumnCollection(",")][CountRange(3, 1)] ImmutableArray<int> Tags);
            """;

        var result = SourceGeneratorTestHelper.Run(source);

        var diagnostic = Assert.Single(result.Diagnostics, d => d.Id == "SDP0015");
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);

        logger.LogInformation("Diagnostic {Id} reported: {Message}", diagnostic.Id, diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Reports_SDP0019_when_CountRange_bound_is_negative()
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
                [SingleColumnCollection(",")][CountRange(-3, -1)] ImmutableArray<int> Tags);
            """;

        var result = SourceGeneratorTestHelper.Run(source);

        var diagnostic = Assert.Single(result.Diagnostics, d => d.Id == "SDP0019");
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);

        logger.LogInformation("Diagnostic {Id} reported: {Message}", diagnostic.Id, diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Reports_SDP0016_when_SingleColumnCollection_on_scalar()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            using Sdp.Attributes;
            namespace Test;

            [StaticDataRecord("File", "Sheet")]
            public sealed partial record Foo(
                int Id,
                [SingleColumnCollection(",")] string Tags);
            """;

        var result = SourceGeneratorTestHelper.Run(source);

        var diagnostic = Assert.Single(result.Diagnostics, d => d.Id == "SDP0016");
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("SingleColumnCollection", diagnostic.GetMessage(CultureInfo.InvariantCulture));

        logger.LogInformation("Diagnostic {Id} reported: {Message}", diagnostic.Id, diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Reports_SDP0016_when_NullString_on_non_nullable_scalar()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            using Sdp.Attributes;
            namespace Test;

            [StaticDataRecord("File", "Sheet")]
            public sealed partial record Foo(
                int Id,
                [NullString("-")] int Score);
            """;

        var result = SourceGeneratorTestHelper.Run(source);

        var diagnostic = Assert.Single(result.Diagnostics, d => d.Id == "SDP0016");
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("NullString", diagnostic.GetMessage(CultureInfo.InvariantCulture));

        logger.LogInformation("Diagnostic {Id} reported: {Message}", diagnostic.Id, diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Reports_SDP0016_when_RegularExpression_on_non_string_parameter()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            using Sdp.Attributes;
            namespace Test;

            [StaticDataRecord("File", "Sheet")]
            public sealed partial record Foo(
                int Id,
                [RegularExpression(@"^\d+$")] int Score);
            """;

        var result = SourceGeneratorTestHelper.Run(source);

        var diagnostic = Assert.Single(result.Diagnostics, d => d.Id == "SDP0016");
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("RegularExpression", diagnostic.GetMessage(CultureInfo.InvariantCulture));

        logger.LogInformation("Diagnostic {Id} reported: {Message}", diagnostic.Id, diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Reports_SDP0016_when_DateTimeFormat_on_non_date_parameter()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            using Sdp.Attributes;
            namespace Test;

            [StaticDataRecord("File", "Sheet")]
            public sealed partial record Foo(
                int Id,
                [DateTimeFormat("yyyy")] int Year);
            """;

        var result = SourceGeneratorTestHelper.Run(source);

        var diagnostic = Assert.Single(result.Diagnostics, d => d.Id == "SDP0016");
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("DateTimeFormat", diagnostic.GetMessage(CultureInfo.InvariantCulture));

        logger.LogInformation("Diagnostic {Id} reported: {Message}", diagnostic.Id, diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Reports_SDP0010_for_nested_record_range_bound_out_of_type_range()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            using Sdp.Attributes;
            namespace Test;

            public sealed record Inner([Range(0, 300)] byte Value);

            [StaticDataRecord("File", "Sheet")]
            public sealed partial record Foo(int Id, Inner Pos);
            """;

        var result = SourceGeneratorTestHelper.Run(source);

        var diagnostic = Assert.Single(result.Diagnostics, d => d.Id == "SDP0010");
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("Pos.Value", diagnostic.GetMessage(CultureInfo.InvariantCulture));

        var mapperTree = SourceGeneratorTestHelper.GetSingleTree(result, "CsvMapper.g.cs");
        Assert.Contains("NotSupportedException", mapperTree.ToString());

        logger.LogInformation("Diagnostic {Id} reported: {Message}", diagnostic.Id, diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Reports_SDP0008_for_nested_nullable_collection()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            using System.Collections.Frozen;
            using Sdp.Attributes;
            namespace Test;

            public sealed record Inner(int Id, FrozenSet<int>? Tags);

            [StaticDataRecord("File", "Sheet")]
            public sealed partial record Foo(int Id, Inner Data);
            """;

        var result = SourceGeneratorTestHelper.Run(source);

        var diagnostic = Assert.Single(result.Diagnostics, d => d.Id == "SDP0008");
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("Data.Tags", diagnostic.GetMessage(CultureInfo.InvariantCulture));

        logger.LogInformation("Diagnostic {Id} reported: {Message}", diagnostic.Id, diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Emits_default_for_Ignore_parameter()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            using Sdp.Attributes;
            namespace Test;

            [StaticDataRecord("File", "Sheet")]
            public sealed partial record Foo(int Id, [Ignore] int Cached, string Name);
            """;

        var (result, final) = SourceGeneratorTestHelper.RunWithFinal(source);

        var mapperTree = SourceGeneratorTestHelper.GetSingleTree(result, "CsvMapper.g.cs");

        var code = mapperTree.ToString();
        Assert.Contains("Cached: default!", code);
        Assert.DoesNotContain("headers[\"Cached\"]", code);

        var errors = SourceGeneratorTestHelper.GetCompilationErrors(final);
        Assert.Empty(errors);

        logger.LogInformation("Ignored parameter emitted as default!, compilation errors: {ErrorCount}", errors.Count);
    }

    private TestOutputLogger<CsvMapperGeneratorAttributeUsageTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Information);
        if (factory.CreateLogger<CsvMapperGeneratorAttributeUsageTests>()
            is not TestOutputLogger<CsvMapperGeneratorAttributeUsageTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
