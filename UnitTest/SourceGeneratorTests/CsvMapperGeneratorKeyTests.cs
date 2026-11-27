using System.Globalization;
using Microsoft.Extensions.Logging;
using Sdp.SourceGenerator;
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
        var diagnostic = Assert.Single(diagnostics, d => d.Id == "SDP0003");
        var format = SdpDiagnostics.MultipleKeyAttributes.MessageFormat.ToString(CultureInfo.InvariantCulture);
        var expected = string.Format(CultureInfo.InvariantCulture, format, "Test.Foo", 2, "Id, Name");
        Assert.Equal(expected, diagnostic.GetMessage(CultureInfo.InvariantCulture));
        Assert.Empty(logger.Logs);
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
        Assert.Empty(logger.Logs);
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
        Assert.Empty(logger.Logs);
    }

    [Theory]
    [InlineData("[Key] [NullString(\"\")] int? Id")]
    [InlineData("[Key] [NullString(\"\")] string? Id")]
    public void Reports_SDP0022_when_Key_is_nullable(string keyParameter)
    {
        var logger = CreateLogger();

        // language=C#
        const string template = """
            using Sdp.Attributes;
            namespace Test;

            [StaticDataRecord("File", "Sheet")]
            public sealed partial record Foo(
                __KEY__,
                string Name);
            """;
        var source = template.Replace("__KEY__", keyParameter, StringComparison.Ordinal);

        var result = SourceGeneratorTestHelper.Run(source);
        var diagnostics = result.Results.SelectMany(r => r.Diagnostics).ToList();
        var diagnostic = Assert.Single(diagnostics, d => d.Id == "SDP0022");
        var format = SdpDiagnostics.KeyMustBeNonNullable.MessageFormat.ToString(CultureInfo.InvariantCulture);
        var expected = string.Format(CultureInfo.InvariantCulture, format, "Test.Foo", "Id");
        Assert.Equal(expected, diagnostic.GetMessage(CultureInfo.InvariantCulture));
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void Does_not_report_SDP0022_when_Key_is_non_nullable()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            using Sdp.Attributes;
            namespace Test;

            [StaticDataRecord("File", "Sheet")]
            public sealed partial record Foo(
                [Key] int Id,
                [NullString("")] string? Name);
            """;

        var result = SourceGeneratorTestHelper.Run(source);
        var diagnostics = result.Results.SelectMany(r => r.Diagnostics).ToList();
        Assert.DoesNotContain(diagnostics, d => d.Id == "SDP0022");
        Assert.Empty(logger.Logs);
    }

    [Theory]
    [InlineData("int", "int?", "SDP0022")]
    [InlineData("string", "string?", "SDP0022")]
    [InlineData("string", "int?", "SDP0018,SDP0022")]
    public void Nullable_Key_of_map_value_reports_SDP0018_only_when_types_differ_beyond_nullability(
        string mapKeyType,
        string keyParameterType,
        string expectedDiagnosticIds)
    {
        var logger = CreateLogger();

        // language=C#
        const string template = """
            using System.Collections.Frozen;
            using Sdp.Attributes;
            namespace Test;

            public sealed record Reward([Key] [NullString("")] __KEY_TYPE__ Id, string Name);

            [StaticDataRecord("File", "Sheet")]
            public sealed partial record Foo(
                int Id,
                [Length(2)] FrozenDictionary<__MAP_KEY_TYPE__, Reward> Rewards);
            """;
        var source = template
            .Replace("__KEY_TYPE__", keyParameterType, StringComparison.Ordinal)
            .Replace("__MAP_KEY_TYPE__", mapKeyType, StringComparison.Ordinal);

        var result = SourceGeneratorTestHelper.Run(source);
        var diagnosticIds = result.Results
            .SelectMany(r => r.Diagnostics)
            .Select(d => d.Id)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();
        Assert.Equal(expectedDiagnosticIds, string.Join(",", diagnosticIds));
        Assert.Empty(logger.Logs);
    }

    private TestOutputLogger<CsvMapperGeneratorKeyTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<CsvMapperGeneratorKeyTests>()
            is not TestOutputLogger<CsvMapperGeneratorKeyTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
