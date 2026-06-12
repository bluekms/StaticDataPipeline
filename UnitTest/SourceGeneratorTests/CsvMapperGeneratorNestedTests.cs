using Microsoft.Extensions.Logging;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.SourceGeneratorTests;

public class CsvMapperGeneratorNestedTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void Emits_nested_helper_for_record_parameter()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            using Sdp.Attributes;
            namespace Test;

            public sealed partial record Address(
                [ColumnName("도시")] string City,
                string Detail);

            [StaticDataRecord("File", "Sheet")]
            public sealed partial record School(
                int Id,
                string Name,
                Address Address);
            """;

        var (result, final) = SourceGeneratorTestHelper.RunWithFinal(source);

        var mapperTree = SourceGeneratorTestHelper.GetSingleTree(result, "CsvMapper.g.cs");

        var code = mapperTree.ToString();
        Assert.Contains("__MapNested_1_Address(headers, values, \"Address\")", code);
        Assert.Contains("private static global::Test.Address __MapNested_1_Address(", code);
        Assert.Contains("basePath + \".\" + \"도시\"", code);
        Assert.Contains("basePath + \".\" + \"Detail\"", code);

        var errors = SourceGeneratorTestHelper.GetCompilationErrors(final);
        Assert.Empty(errors);

        logger.LogInformation("__MapNested_1_Address emitted, compilation errors: {ErrorCount}", errors.Count);
    }

    [Fact]
    public void Emits_one_helper_per_nested_record_type_even_with_multiple_uses()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            using Sdp.Attributes;
            namespace Test;

            public sealed partial record Address(string City, string Detail);

            [StaticDataRecord("File", "Sheet")]
            public sealed partial record School(
                int Id,
                Address Home,
                Address Office);
            """;

        var (result, final) = SourceGeneratorTestHelper.RunWithFinal(source);

        var mapperTree = SourceGeneratorTestHelper.GetSingleTree(result, "CsvMapper.g.cs");

        var code = mapperTree.ToString();

        var helperMatches = System.Text.RegularExpressions.Regex.Matches(
            code,
            @"private\s+static\s+global::Test\.Address\s+__MapNested_1_Address\(");
        Assert.Single(helperMatches);

        Assert.Contains("__MapNested_1_Address(headers, values, \"Home\")", code);
        Assert.Contains("__MapNested_1_Address(headers, values, \"Office\")", code);

        var errors = SourceGeneratorTestHelper.GetCompilationErrors(final);
        Assert.Empty(errors);

        logger.LogInformation("__MapNested_1_Address helper count: {HelperCount}", helperMatches.Count);
    }

    [Fact]
    public void Emits_recursive_nested_helpers_for_two_levels()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            using Sdp.Attributes;
            namespace Test;

            public sealed partial record Coords(double Lat, double Lng);

            public sealed partial record Address(string City, Coords Coords);

            [StaticDataRecord("File", "Sheet")]
            public sealed partial record School(int Id, Address Address);
            """;

        var (result, final) = SourceGeneratorTestHelper.RunWithFinal(source);

        var mapperTree = SourceGeneratorTestHelper.GetSingleTree(result, "CsvMapper.g.cs");

        var code = mapperTree.ToString();
        Assert.Contains("__MapNested_1_Address", code);
        Assert.Contains("__MapNested_2_Coords", code);
        Assert.Contains("__MapNested_2_Coords(headers, values, basePath + \".\" + \"Coords\")", code);

        var errors = SourceGeneratorTestHelper.GetCompilationErrors(final);
        Assert.Empty(errors);

        logger.LogInformation("Two-level nested helpers emitted, compilation errors: {ErrorCount}", errors.Count);
    }

    [Fact]
    public void Handles_nullable_NullString_inside_nested_record()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            using Sdp.Attributes;
            namespace Test;

            public sealed partial record ContactInfo(
                string Email,
                [NullString("")] string? Phone);

            [StaticDataRecord("File", "Sheet")]
            public sealed partial record Person(int Id, ContactInfo Contact);
            """;

        var (result, final) = SourceGeneratorTestHelper.RunWithFinal(source);

        var mapperTree = SourceGeneratorTestHelper.GetSingleTree(result, "CsvMapper.g.cs");

        var code = mapperTree.ToString();
        Assert.Contains("__MapNested_1_ContactInfo", code);
        Assert.Contains("private static string? __MapNullable_1_ContactInfo_Phone(string value)", code);

        var errors = SourceGeneratorTestHelper.GetCompilationErrors(final);
        Assert.Empty(errors);

        logger.LogInformation("__MapNested_1_ContactInfo emitted, compilation errors: {ErrorCount}", errors.Count);
    }

    private TestOutputLogger<CsvMapperGeneratorNestedTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Information);
        if (factory.CreateLogger<CsvMapperGeneratorNestedTests>()
            is not TestOutputLogger<CsvMapperGeneratorNestedTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
