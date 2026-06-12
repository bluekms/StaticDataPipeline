using Microsoft.Extensions.Logging;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.SourceGeneratorTests;

public class CsvMapperGeneratorScalarTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void Emits_mapper_for_int_and_string_record()
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

        var mapperTree = SourceGeneratorTestHelper.GetSingleTree(result, "CsvMapper.g.cs");

        var code = mapperTree.ToString();
        Assert.Contains("MapFromCsvRow", code);
        Assert.Contains("global::Test.Foo", code);
        Assert.Contains("int.Parse(values[headers[\"Id\"]]", code);
        Assert.Contains("values[headers[\"Name\"]]", code);

        logger.LogInformation("Mapper generated ({Length} chars): {FilePath}", code.Length, mapperTree.FilePath);
    }

    [Fact]
    public void Emits_mapper_for_all_supported_scalar_kinds()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            using System;
            using Sdp.Attributes;
            namespace Test;

            [StaticDataRecord("File", "Sheet")]
            public sealed partial record Foo(
                bool B,
                byte U8,
                sbyte S8,
                short S16,
                ushort U16,
                int I,
                uint U,
                long L,
                ulong UL,
                float F,
                double D,
                decimal M,
                string Name,
                Guid Id);
            """;

        var (result, final) = SourceGeneratorTestHelper.RunWithFinal(source);

        SourceGeneratorTestHelper.GetSingleTree(result, "CsvMapper.g.cs");

        var errors = SourceGeneratorTestHelper.GetCompilationErrors(final);

        Assert.Empty(errors);

        logger.LogInformation(
            "Generated trees: {Count}, compilation errors: {ErrorCount}",
            result.GeneratedTrees.Length,
            errors.Count);
    }

    [Fact]
    public void Honors_ColumnName_attribute_for_header_lookup()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            using Sdp.Attributes;
            namespace Test;

            [StaticDataRecord("File", "Sheet")]
            public sealed partial record Foo(
                int Id,
                [ColumnName("이름")] string Name);
            """;

        var result = SourceGeneratorTestHelper.Run(source);

        var mapperTree = SourceGeneratorTestHelper.GetSingleTree(result, "CsvMapper.g.cs");

        var code = mapperTree.ToString();
        Assert.Contains("headers[\"이름\"]", code);
        Assert.DoesNotContain("headers[\"Name\"]", code);

        logger.LogInformation("Mapper uses ColumnName header '이름' instead of 'Name': {FilePath}", mapperTree.FilePath);
    }

    private TestOutputLogger<CsvMapperGeneratorScalarTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Information);
        if (factory.CreateLogger<CsvMapperGeneratorScalarTests>()
            is not TestOutputLogger<CsvMapperGeneratorScalarTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
