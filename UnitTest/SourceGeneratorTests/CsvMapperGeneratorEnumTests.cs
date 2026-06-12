using Microsoft.Extensions.Logging;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.SourceGeneratorTests;

public class CsvMapperGeneratorEnumTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void Emits_switch_for_enum_member()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            using Sdp.Attributes;
            namespace Test;

            public enum Kind { Alpha, Beta, Gamma }

            [StaticDataRecord("File", "Sheet")]
            public sealed partial record Foo(int Id, Kind K);
            """;

        var (result, final) = SourceGeneratorTestHelper.RunWithFinal(source);

        var mapperTree = SourceGeneratorTestHelper.GetSingleTree(result, "CsvMapper.g.cs");

        var code = mapperTree.ToString();
        Assert.Contains("__MapColumn_K", code);
        Assert.Contains("case \"Alpha\":", code);
        Assert.Contains("case \"Beta\":", code);
        Assert.Contains("case \"Gamma\":", code);
        Assert.Contains("return global::Test.Kind.Alpha;", code);

        // 멤버명뿐 아니라 underlying 숫자값으로도 매핑된다.
        Assert.Contains("case \"0\":", code);
        Assert.Contains("not a defined member of global::Test.Kind", code);

        var errors = SourceGeneratorTestHelper.GetCompilationErrors(final);
        Assert.Empty(errors);

        logger.LogInformation("Enum switch for Kind emitted, compilation errors: {ErrorCount}", errors.Count);
    }

    [Fact]
    public void Key_enum_default_supports_comma_combination()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            using Sdp.Attributes;
            namespace Test;

            public enum Flag { None = 0, A = 1, B = 2 }

            [StaticDataRecord("File", "Sheet")]
            public sealed partial record Foo([Key] Flag K, string Name);
            """;

        var (result, final) = SourceGeneratorTestHelper.RunWithFinal(source);

        var mapperTree = SourceGeneratorTestHelper.GetSingleTree(result, "CsvMapper.g.cs");

        var code = mapperTree.ToString();
        Assert.Contains("value.Split(',')", code);
        Assert.Contains("__acc |=", code);
        Assert.Contains("(global::Test.Flag)__acc", code);

        var errors = SourceGeneratorTestHelper.GetCompilationErrors(final);
        Assert.Empty(errors);

        logger.LogInformation("Comma combination mapping emitted, compilation errors: {ErrorCount}", errors.Count);
    }

    [Fact]
    public void Emits_null_branch_for_nullable_enum_with_NullString()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            using Sdp.Attributes;
            namespace Test;

            public enum Kind { Alpha, Beta }

            [StaticDataRecord("File", "Sheet")]
            public sealed partial record Foo(
                int Id,
                [NullString("?")] Kind? K);
            """;

        var (result, final) = SourceGeneratorTestHelper.RunWithFinal(source);

        var mapperTree = SourceGeneratorTestHelper.GetSingleTree(result, "CsvMapper.g.cs");

        var code = mapperTree.ToString();
        Assert.Contains("== \"?\"", code);
        Assert.Contains("private static global::Test.Kind? __MapNullable_K(string value)", code);
        Assert.Contains("__MapColumn_K", code);

        var errors = SourceGeneratorTestHelper.GetCompilationErrors(final);
        Assert.Empty(errors);

        logger.LogInformation("Null branch for nullable enum emitted, compilation errors: {ErrorCount}", errors.Count);
    }

    [Fact]
    public void Key_long_flags_enum_with_sign_bit_member_compiles()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            using Sdp.Attributes;
            namespace Test;

            [System.Flags]
            public enum Flags : long { None = 0, High = long.MinValue }

            [StaticDataRecord("File", "Sheet")]
            public sealed partial record Foo([Key] Flags F, string Name);
            """;

        var (result, final) = SourceGeneratorTestHelper.RunWithFinal(source);

        var mapperTree = SourceGeneratorTestHelper.GetSingleTree(result, "CsvMapper.g.cs");

        Assert.Contains("unchecked((long)", mapperTree.ToString());

        var errors = SourceGeneratorTestHelper.GetCompilationErrors(final);
        Assert.Empty(errors);

        logger.LogInformation("unchecked long cast emitted, compilation errors: {ErrorCount}", errors.Count);
    }

    private TestOutputLogger<CsvMapperGeneratorEnumTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Information);
        if (factory.CreateLogger<CsvMapperGeneratorEnumTests>()
            is not TestOutputLogger<CsvMapperGeneratorEnumTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
