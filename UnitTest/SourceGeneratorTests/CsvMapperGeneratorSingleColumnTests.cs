using Microsoft.Extensions.Logging;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.SourceGeneratorTests;

public class CsvMapperGeneratorSingleColumnTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void Emits_single_column_array_helper_for_ImmutableArray_with_separator()
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
                [SingleColumnCollection(",")] ImmutableArray<int> Tags);
            """;

        var (result, final) = SourceGeneratorTestHelper.RunWithFinal(source);

        var mapperTree = SourceGeneratorTestHelper.GetSingleTree(result, "CsvMapper.g.cs");

        var code = mapperTree.ToString();
        Assert.Contains("__MapSingleArray_Tags(headers, values, string.Empty)", code);
        Assert.Contains(".Split(\",\")", code);
        Assert.Contains("parts[i].Trim()", code);
        Assert.Contains("MoveToImmutable", code);

        var errors = SourceGeneratorTestHelper.GetCompilationErrors(final);
        Assert.Empty(errors);

        logger.LogInformation("__MapSingleArray_Tags emitted, compilation errors: {ErrorCount}", errors.Count);
    }

    [Fact]
    public void Emits_single_column_set_helper_with_count_range_check()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            using System.Collections.Frozen;
            using Sdp.Attributes;
            namespace Test;

            [StaticDataRecord("File", "Sheet")]
            public sealed partial record Foo(
                int Id,
                [SingleColumnCollection("|")][CountRange(1, 5)] FrozenSet<string> Labels);
            """;

        var (result, final) = SourceGeneratorTestHelper.RunWithFinal(source);

        var mapperTree = SourceGeneratorTestHelper.GetSingleTree(result, "CsvMapper.g.cs");

        var code = mapperTree.ToString();
        Assert.Contains("__MapSingleSet_Labels(headers, values, string.Empty)", code);
        Assert.Contains(".Split(\"|\")", code);
        Assert.Contains("set.Count < 1", code);
        Assert.Contains("set.Count > 5", code);
        Assert.Contains("Duplicate value {__e} in set 'Labels'", code);

        var errors = SourceGeneratorTestHelper.GetCompilationErrors(final);
        Assert.Empty(errors);

        logger.LogInformation("__MapSingleSet_Labels emitted, compilation errors: {ErrorCount}", errors.Count);
    }

    private TestOutputLogger<CsvMapperGeneratorSingleColumnTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Information);
        if (factory.CreateLogger<CsvMapperGeneratorSingleColumnTests>()
            is not TestOutputLogger<CsvMapperGeneratorSingleColumnTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
