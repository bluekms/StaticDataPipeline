using Microsoft.Extensions.Logging;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.SourceGeneratorTests;

public class CsvMapperGeneratorPartialTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void Reports_SDP0001_when_record_is_not_partial()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            using Sdp.Attributes;
            namespace Test;

            [StaticDataRecord("File", "Sheet")]
            public sealed record Foo(int Id);
            """;

        var result = SourceGeneratorTestHelper.Run(source);
        var diagnostics = result.Results.SelectMany(r => r.Diagnostics).ToList();

        Assert.Contains(diagnostics, d => d.Id == "SDP0001");

        logger.LogInformation(
            "Reported diagnostics: [{Ids}]",
            string.Join(", ", diagnostics.Select(d => d.Id).Distinct()));
    }

    [Fact]
    public void Does_not_report_when_record_is_partial()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            using Sdp.Attributes;
            namespace Test;

            [StaticDataRecord("File", "Sheet")]
            public sealed partial record Foo(int Id);
            """;

        var result = SourceGeneratorTestHelper.Run(source);
        var diagnostics = result.Results.SelectMany(r => r.Diagnostics).ToList();

        Assert.DoesNotContain(diagnostics, d => d.Id == "SDP0001");
        Assert.DoesNotContain(diagnostics, d => d.Id == "SDP0002");

        logger.LogInformation(
            "Reported diagnostics: [{Ids}]",
            string.Join(", ", diagnostics.Select(d => d.Id).Distinct()));
    }

    [Fact]
    public void Reports_SDP0002_when_containing_type_is_not_partial()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            using Sdp.Attributes;
            namespace Test;

            public static class Outer
            {
                [StaticDataRecord("File", "Sheet")]
                public sealed partial record Foo(int Id);
            }
            """;

        var result = SourceGeneratorTestHelper.Run(source);
        var diagnostics = result.Results.SelectMany(r => r.Diagnostics).ToList();

        Assert.Contains(diagnostics, d => d.Id == "SDP0002");

        logger.LogInformation(
            "Reported diagnostics: [{Ids}]",
            string.Join(", ", diagnostics.Select(d => d.Id).Distinct()));
    }

    private TestOutputLogger<CsvMapperGeneratorPartialTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Information);
        if (factory.CreateLogger<CsvMapperGeneratorPartialTests>()
            is not TestOutputLogger<CsvMapperGeneratorPartialTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
