using Microsoft.Extensions.Logging;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.SourceGeneratorTests;

public class StaticDataTableGeneratorTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void Emits_records_constructor_when_table_has_no_user_constructor()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            using Sdp.Attributes;
            using Sdp.Table;
            namespace Test;

            [StaticDataRecord("File", "Sheet")]
            public sealed partial record Foo(int Id);

            public sealed partial class FooTable : StaticDataTable<FooTable, Foo>;
            """;

        var output = SourceGeneratorTestHelper.RunWithFinal(source);
        var tree = SourceGeneratorTestHelper.GetSingleTree(output.Run, "FooTable.TableFactory.g.cs");
        var generated = tree.ToString();

        Assert.Contains("public FooTable(", generated, StringComparison.Ordinal);
        Assert.Contains(": base(records)", generated, StringComparison.Ordinal);

        var errors = SourceGeneratorTestHelper.GetCompilationErrors(output.Final);
        Assert.Empty(errors);

        logger.LogInformation("Generated factory:\n{Generated}", generated);
    }

    [Fact]
    public void Does_not_emit_constructor_when_table_has_primary_constructor()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            using System.Collections.Immutable;
            using Sdp.Attributes;
            using Sdp.Table;
            namespace Test;

            [StaticDataRecord("File", "Sheet")]
            public sealed partial record Foo(int Id);

            public sealed partial class FooTable(ImmutableArray<Foo> records)
                : StaticDataTable<FooTable, Foo>(records);
            """;

        var output = SourceGeneratorTestHelper.RunWithFinal(source);
        var tree = SourceGeneratorTestHelper.GetSingleTree(output.Run, "FooTable.TableFactory.g.cs");
        var generated = tree.ToString();

        Assert.DoesNotContain("public FooTable(", generated, StringComparison.Ordinal);

        var errors = SourceGeneratorTestHelper.GetCompilationErrors(output.Final);
        Assert.Empty(errors);

        logger.LogInformation("Generated factory:\n{Generated}", generated);
    }

    [Fact]
    public void Does_not_emit_constructor_when_table_has_explicit_constructor()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            using System.Collections.Immutable;
            using Sdp.Attributes;
            using Sdp.Table;
            namespace Test;

            [StaticDataRecord("File", "Sheet")]
            public sealed partial record Foo(int Id);

            public sealed partial class FooTable : StaticDataTable<FooTable, Foo>
            {
                public FooTable(ImmutableArray<Foo> records)
                    : base(records)
                {
                }
            }
            """;

        var output = SourceGeneratorTestHelper.RunWithFinal(source);
        var tree = SourceGeneratorTestHelper.GetSingleTree(output.Run, "FooTable.TableFactory.g.cs");
        var generated = tree.ToString();

        Assert.DoesNotContain("public FooTable(", generated, StringComparison.Ordinal);

        var errors = SourceGeneratorTestHelper.GetCompilationErrors(output.Final);
        Assert.Empty(errors);

        logger.LogInformation("Generated factory:\n{Generated}", generated);
    }

    private TestOutputLogger<StaticDataTableGeneratorTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Information);
        if (factory.CreateLogger<StaticDataTableGeneratorTests>()
            is not TestOutputLogger<StaticDataTableGeneratorTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
