using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.SourceGeneratorTests;

public class CsvMapperGeneratorCollectionTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void Emits_array_helper_for_ImmutableArray_of_string()
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
                [Length(3)] ImmutableArray<string> Departments);
            """;

        var (result, final) = SourceGeneratorTestHelper.RunWithFinal(source);

        var mapperTree = SourceGeneratorTestHelper.GetSingleTree(result, "CsvMapper.g.cs");

        var code = mapperTree.ToString();
        Assert.Contains("__MapArray_Departments(headers, values, string.Empty)", code);
        Assert.Contains("ImmutableArray.CreateBuilder<string>(3)", code);
        Assert.Contains("\"Departments[0]\"", code);
        Assert.Contains("\"Departments[1]\"", code);
        Assert.Contains("\"Departments[2]\"", code);
        Assert.Contains("MoveToImmutable", code);

        var errors = SourceGeneratorTestHelper.GetCompilationErrors(final);
        Assert.Empty(errors);

        logger.LogInformation("__MapArray_Departments emitted, compilation errors: {ErrorCount}", errors.Count);
    }

    [Fact]
    public void Emits_array_helper_for_ImmutableArray_of_int()
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
                [Length(2)] ImmutableArray<int> Grades);
            """;

        var (result, final) = SourceGeneratorTestHelper.RunWithFinal(source);

        var mapperTree = SourceGeneratorTestHelper.GetSingleTree(result, "CsvMapper.g.cs");

        var code = mapperTree.ToString();
        Assert.Contains("ImmutableArray.CreateBuilder<int>(2)", code);
        Assert.Contains("\"Grades[0]\"", code);
        Assert.Contains("int.Parse(values[headers[__k0]]", code);

        var errors = SourceGeneratorTestHelper.GetCompilationErrors(final);
        Assert.Empty(errors);

        logger.LogInformation("Array helper for int emitted, compilation errors: {ErrorCount}", errors.Count);
    }

    [Fact]
    public void Emits_set_helper_for_FrozenSet_of_int()
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
                [Length(2)] FrozenSet<int> Grades);
            """;

        var (result, final) = SourceGeneratorTestHelper.RunWithFinal(source);

        var mapperTree = SourceGeneratorTestHelper.GetSingleTree(result, "CsvMapper.g.cs");

        var code = mapperTree.ToString();
        Assert.Contains("__MapSet_Grades(headers, values, string.Empty)", code);
        Assert.Contains("HashSet<int>(2)", code);
        Assert.Contains("Duplicate value {__e0} in set 'Grades'", code);
        Assert.Contains("ToFrozenSet(set)", code);

        var errors = SourceGeneratorTestHelper.GetCompilationErrors(final);
        Assert.Empty(errors);

        logger.LogInformation("__MapSet_Grades emitted, compilation errors: {ErrorCount}", errors.Count);
    }

    [Fact]
    public void Emits_dict_helper_for_FrozenDictionary_of_keyed_record()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            using System.Collections.Frozen;
            using Sdp.Attributes;
            namespace Test;

            public sealed partial record Item(
                [Key] int Id,
                string Name);

            [StaticDataRecord("File", "Sheet")]
            public sealed partial record Foo(
                int Id,
                [Length(2)] FrozenDictionary<int, Item> Items);
            """;

        var (result, final) = SourceGeneratorTestHelper.RunWithFinal(source);

        var mapperTree = SourceGeneratorTestHelper.GetSingleTree(result, "CsvMapper.g.cs");

        var code = mapperTree.ToString();
        Assert.Contains("__MapDict_Items(headers, values, string.Empty)", code);
        Assert.Contains("\"Items[0]\"", code);
        Assert.Contains("__MapNested_1_Item(headers, values, __k0)", code);
        Assert.Contains("__MapNested_1_Item(headers, values, __k1)", code);
        Assert.Contains("dict.TryAdd(__v0.Id, __v0)", code);
        Assert.Contains("dict.TryAdd(__v1.Id, __v1)", code);
        Assert.Contains("Duplicate key {__v0.Id} in dict 'Items'", code);
        Assert.Contains("FrozenDictionary.ToFrozenDictionary(dict)", code);

        var errors = SourceGeneratorTestHelper.GetCompilationErrors(final);
        Assert.Empty(errors);

        logger.LogInformation("__MapDict_Items emitted, compilation errors: {ErrorCount}", errors.Count);
    }

    [Fact]
    public void Skips_emit_for_FrozenDictionary_when_value_record_lacks_Key()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            using System.Collections.Frozen;
            using Sdp.Attributes;
            namespace Test;

            public sealed partial record Item(int Id, string Name);

            [StaticDataRecord("File", "Sheet")]
            public sealed partial record Foo(
                int Id,
                [Length(2)] FrozenDictionary<int, Item> Items);
            """;

        var result = SourceGeneratorTestHelper.Run(source);

        Assert.Contains(result.Diagnostics, d => d.Id == "SDP0017" && d.Severity == DiagnosticSeverity.Error);

        var mapperTree = SourceGeneratorTestHelper.GetSingleTree(result, "CsvMapper.g.cs");
        Assert.Contains("NotSupportedException", mapperTree.ToString());

        logger.LogInformation("NotSupportedException fallback emitted: {FilePath}", mapperTree.FilePath);
    }

    [Fact]
    public void Skips_emit_for_FrozenDictionary_when_key_type_mismatches()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            using System.Collections.Frozen;
            using Sdp.Attributes;
            namespace Test;

            public sealed partial record Item(
                [Key] string Id,
                string Name);

            [StaticDataRecord("File", "Sheet")]
            public sealed partial record Foo(
                int Id,
                [Length(2)] FrozenDictionary<int, Item> Items);
            """;

        var result = SourceGeneratorTestHelper.Run(source);

        Assert.Contains(result.Diagnostics, d => d.Id == "SDP0018" && d.Severity == DiagnosticSeverity.Error);

        var mapperTree = SourceGeneratorTestHelper.GetSingleTree(result, "CsvMapper.g.cs");
        Assert.Contains("NotSupportedException", mapperTree.ToString());

        logger.LogInformation("NotSupportedException fallback emitted: {FilePath}", mapperTree.FilePath);
    }

    [Fact]
    public void Emits_collection_nested_inside_record()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            using System.Collections.Immutable;
            using Sdp.Attributes;
            namespace Test;

            public sealed partial record Inner(
                int X,
                [Length(2)] ImmutableArray<int> Tags);

            [StaticDataRecord("File", "Sheet")]
            public sealed partial record Foo(int Id, Inner Inner);
            """;

        var (result, final) = SourceGeneratorTestHelper.RunWithFinal(source);

        var mapperTree = SourceGeneratorTestHelper.GetSingleTree(result, "CsvMapper.g.cs");
        var code = mapperTree.ToString();

        // nested record 안의 고정 길이 컬렉션도 basePath 기반으로 emit 된다.
        Assert.DoesNotContain("NotSupportedException", code);
        Assert.Contains("__MapArray_", code);
        Assert.Contains("basePath + \".\"", code);

        var errors = SourceGeneratorTestHelper.GetCompilationErrors(final);
        Assert.Empty(errors);

        logger.LogInformation("Nested collection emitted via basePath, compilation errors: {ErrorCount}", errors.Count);
    }

    private TestOutputLogger<CsvMapperGeneratorCollectionTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Information);
        if (factory.CreateLogger<CsvMapperGeneratorCollectionTests>()
            is not TestOutputLogger<CsvMapperGeneratorCollectionTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
