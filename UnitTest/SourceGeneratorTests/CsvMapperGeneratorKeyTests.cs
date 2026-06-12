using Microsoft.Extensions.Logging;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.SourceGeneratorTests;

public class CsvMapperGeneratorKeyTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void Reports_SDP0003_when_multiple_Key_attributes()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            using Sdp.Attributes;
            namespace Test;

            [StaticDataRecord("File", "Sheet")]
            public sealed partial record Foo(
                [Key] int Id,
                [Key] string Name);
            """;

        var result = SourceGeneratorTestHelper.Run(source);
        var diagnostics = result.Results.SelectMany(r => r.Diagnostics).ToList();

        Assert.Contains(diagnostics, d => d.Id == "SDP0003");

        logger.LogInformation(
            "Reported diagnostics: [{Ids}]",
            string.Join(", ", diagnostics.Select(d => d.Id).Distinct()));
    }

    [Fact]
    public void Does_not_report_when_single_Key()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            using Sdp.Attributes;
            namespace Test;

            [StaticDataRecord("File", "Sheet")]
            public sealed partial record Foo(
                [Key] int Id,
                string Name);
            """;

        var result = SourceGeneratorTestHelper.Run(source);
        var diagnostics = result.Results.SelectMany(r => r.Diagnostics).ToList();

        Assert.DoesNotContain(diagnostics, d => d.Id == "SDP0003");

        logger.LogInformation(
            "Reported diagnostics: [{Ids}]",
            string.Join(", ", diagnostics.Select(d => d.Id).Distinct()));
    }

    [Fact]
    public void Does_not_report_when_no_Key()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            using Sdp.Attributes;
            namespace Test;

            [StaticDataRecord("File", "Sheet")]
            public sealed partial record Foo(int Id, string Name);
            """;

        var result = SourceGeneratorTestHelper.Run(source);
        var diagnostics = result.Results.SelectMany(r => r.Diagnostics).ToList();

        Assert.DoesNotContain(diagnostics, d => d.Id == "SDP0003");

        logger.LogInformation(
            "Reported diagnostics: [{Ids}]",
            string.Join(", ", diagnostics.Select(d => d.Id).Distinct()));
    }

    private TestOutputLogger<CsvMapperGeneratorKeyTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Information);
        if (factory.CreateLogger<CsvMapperGeneratorKeyTests>()
            is not TestOutputLogger<CsvMapperGeneratorKeyTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
