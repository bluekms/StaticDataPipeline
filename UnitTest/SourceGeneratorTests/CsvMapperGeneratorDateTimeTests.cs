using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.SourceGeneratorTests;

public class CsvMapperGeneratorDateTimeTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void Emits_ParseExact_for_DateTime_with_DateTimeFormat()
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
                [DateTimeFormat("yyyy-MM-dd")] DateTime D);
            """;

        var (result, final) = SourceGeneratorTestHelper.RunWithFinal(source);

        var mapperTree = SourceGeneratorTestHelper.GetSingleTree(result, "CsvMapper.g.cs");

        var code = mapperTree.ToString();
        Assert.Contains("global::System.DateTime.ParseExact(", code);
        Assert.Contains("\"yyyy-MM-dd\"", code);

        var errors = SourceGeneratorTestHelper.GetCompilationErrors(final);
        Assert.Empty(errors);

        logger.LogInformation("DateTime.ParseExact emitted, compilation errors: {ErrorCount}", errors.Count);
    }

    [Fact]
    public void Emits_ParseExact_for_DateTimeOffset_with_DateTimeFormat()
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
                [DateTimeFormat("yyyy-MM-ddTHH:mm:sszzz")] DateTimeOffset D);
            """;

        var (result, final) = SourceGeneratorTestHelper.RunWithFinal(source);

        var mapperTree = SourceGeneratorTestHelper.GetSingleTree(result, "CsvMapper.g.cs");

        var code = mapperTree.ToString();
        Assert.Contains("global::System.DateTimeOffset.ParseExact(", code);

        var errors = SourceGeneratorTestHelper.GetCompilationErrors(final);
        Assert.Empty(errors);

        logger.LogInformation("DateTimeOffset.ParseExact emitted, compilation errors: {ErrorCount}", errors.Count);
    }

    [Fact]
    public void Emits_ParseExact_for_TimeSpan_with_TimeSpanFormat()
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
                [TimeSpanFormat(@"hh\:mm")] TimeSpan T);
            """;

        var (result, final) = SourceGeneratorTestHelper.RunWithFinal(source);

        var mapperTree = SourceGeneratorTestHelper.GetSingleTree(result, "CsvMapper.g.cs");

        var code = mapperTree.ToString();
        Assert.Contains("global::System.TimeSpan.ParseExact(", code);

        var errors = SourceGeneratorTestHelper.GetCompilationErrors(final);
        Assert.Empty(errors);

        logger.LogInformation("TimeSpan.ParseExact emitted, compilation errors: {ErrorCount}", errors.Count);
    }

    [Fact]
    public void Skips_emit_for_DateTime_without_DateTimeFormat()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            using System;
            using Sdp.Attributes;
            namespace Test;

            [StaticDataRecord("File", "Sheet")]
            public sealed partial record Foo(int Id, DateTime D);
            """;

        var result = SourceGeneratorTestHelper.Run(source);

        Assert.Contains(result.Diagnostics, d => d.Id == "SDP0004" && d.Severity == DiagnosticSeverity.Error);

        var mapperTree = SourceGeneratorTestHelper.GetSingleTree(result, "CsvMapper.g.cs");
        Assert.Contains("NotSupportedException", mapperTree.ToString());

        logger.LogInformation("NotSupportedException fallback emitted: {FilePath}", mapperTree.FilePath);
    }

    [Fact]
    public void Skips_emit_for_TimeSpan_without_TimeSpanFormat()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            using System;
            using Sdp.Attributes;
            namespace Test;

            [StaticDataRecord("File", "Sheet")]
            public sealed partial record Foo(int Id, TimeSpan T);
            """;

        var result = SourceGeneratorTestHelper.Run(source);

        Assert.Contains(result.Diagnostics, d => d.Id == "SDP0004" && d.Severity == DiagnosticSeverity.Error);

        var mapperTree = SourceGeneratorTestHelper.GetSingleTree(result, "CsvMapper.g.cs");
        Assert.Contains("NotSupportedException", mapperTree.ToString());

        logger.LogInformation("NotSupportedException fallback emitted: {FilePath}", mapperTree.FilePath);
    }

    private TestOutputLogger<CsvMapperGeneratorDateTimeTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Information);
        if (factory.CreateLogger<CsvMapperGeneratorDateTimeTests>()
            is not TestOutputLogger<CsvMapperGeneratorDateTimeTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
