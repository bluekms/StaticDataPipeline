using Microsoft.Extensions.Logging;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.SourceGeneratorTests;

// 구조적으로 잘못된 ViewSet 에 대해 SG 가 SDP03xx 진단뿐 아니라 런타임에 던지는 Build 를 방출하는지 검증한다.
// 사용자가 진단을 억제(NoWarn 등)해도 런타임에 실패가 보장되어야 한다.
public class ViewSetRuntimeGuardTests(ITestOutputHelper testOutputHelper)
{
    // language=C#
    private const string Header = """
        using Microsoft.Extensions.Logging;
        using Sdp.Attributes;
        using Sdp.Manager;
        using Sdp.Table;
        using Sdp.View;
        using System.Collections.Immutable;

        namespace Test;

        [StaticDataRecord("F", "S")]
        public sealed partial record Rec(int Id);

        public sealed partial class RecTable(ImmutableArray<Rec> records)
            : StaticDataTable<Rec>(records);

        """;

    [Fact]
    public void Emits_runtime_throw_for_nullable_member()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            public sealed partial class SomeView(GenManager.TableSet tables)
                : StaticDataView<GenManager.TableSet>(tables);

            public partial class GenManager(ILogger logger)
                : StaticDataManager<GenManager.TableSet, GenManager.ViewSet>(logger)
            {
                public sealed partial record TableSet(RecTable? Recs);
                public sealed partial record ViewSet(SomeView? V);
            }
            """;

        var generated = GeneratedViewSet(source);
        Assert.Contains("ViewSetBuildHelper.NullableViewMemberError(", generated);
        Assert.Contains("throw new global::System.AggregateException", generated);
        Assert.DoesNotContain("NotSupportedException", generated);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void Emits_not_supported_shell_for_non_view_member()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            public partial class GenManager(ILogger logger)
                : StaticDataManager<GenManager.TableSet, GenManager.ViewSet>(logger)
            {
                public sealed partial record TableSet(RecTable? Recs);
                public sealed partial record ViewSet(int V);
            }
            """;

        var generated = GeneratedViewSet(source);
        Assert.Contains("ViewSetBuildHelper.CannotEmitError(", generated);
        Assert.DoesNotContain("ViewSetBuildHelper.Build<", generated);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void Emits_runtime_throw_for_missing_tableset_constructor()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            public sealed partial class SomeView : StaticDataView<GenManager.TableSet>
            {
                public SomeView() : base(null!) { }
            }

            public partial class GenManager(ILogger logger)
                : StaticDataManager<GenManager.TableSet, GenManager.ViewSet>(logger)
            {
                public sealed partial record TableSet(RecTable? Recs);
                public sealed partial record ViewSet(SomeView V);
            }
            """;

        var generated = GeneratedViewSet(source);
        Assert.Contains("ViewSetBuildHelper.ViewConstructorNotFoundError(", generated);
        Assert.Contains("throw new global::System.AggregateException", generated);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void Valid_viewset_emits_real_build_not_runtime_throw()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            public sealed partial class SomeView(GenManager.TableSet tables)
                : StaticDataView<GenManager.TableSet>(tables);

            public partial class GenManager(ILogger logger)
                : StaticDataManager<GenManager.TableSet, GenManager.ViewSet>(logger)
            {
                public sealed partial record TableSet(RecTable? Recs);
                public sealed partial record ViewSet(SomeView V);
            }
            """;

        var generated = GeneratedViewSet(source);
        Assert.Contains("ViewSetBuildHelper.Build<", generated);
        Assert.DoesNotContain("NullableViewMemberError(", generated);
        Assert.DoesNotContain("ViewConstructorNotFoundError(", generated);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void Emits_not_supported_shell_when_viewset_has_no_primary_constructor()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            public partial class GenManager(ILogger logger)
                : StaticDataManager<GenManager.TableSet, GenManager.ViewSet>(logger)
            {
                public sealed partial record TableSet(RecTable? Recs);
                public sealed partial record ViewSet { }
            }
            """;

        var result = SourceGeneratorTestHelper.Run(Header + source);
        var diagnostics = result.Results.SelectMany(r => r.Diagnostics).ToList();
        Assert.Contains(diagnostics, d => d.Id == "SDP0006");

        var generated = SourceGeneratorTestHelper.GetSingleTree(result, "ViewSetBuilder.g.cs").ToString();
        Assert.Contains("ViewSetBuildHelper.CannotEmitError(", generated);
        Assert.DoesNotContain("ViewSetBuildHelper.Build<", generated);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void Emits_target_mismatch_error_without_constructor_error_when_view_targets_other_tableset()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            public partial class OtherManager(ILogger logger)
                : StaticDataManager<OtherManager.TableSet>(logger)
            {
                public sealed partial record TableSet(RecTable? Recs);
            }

            public sealed partial class SomeView(OtherManager.TableSet tables)
                : StaticDataView<OtherManager.TableSet>(tables);

            public partial class GenManager(ILogger logger)
                : StaticDataManager<GenManager.TableSet, GenManager.ViewSet>(logger)
            {
                public sealed partial record TableSet(RecTable? Recs);
                public sealed partial record ViewSet(SomeView V);
            }
            """;

        var generated = GeneratedViewSet(source);
        Assert.Contains("ViewSetBuildHelper.ViewTargetsDifferentTableSetError(", generated);
        Assert.DoesNotContain("ViewConstructorNotFoundError(", generated);
        Assert.Empty(logger.Logs);
    }

    private static string GeneratedViewSet(string source)
    {
        var result = SourceGeneratorTestHelper.Run(Header + source);
        var tree = SourceGeneratorTestHelper.GetSingleTree(result, "ViewSetBuilder.g.cs");

        return tree.ToString();
    }

    private TestOutputLogger<ViewSetRuntimeGuardTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<ViewSetRuntimeGuardTests>()
            is not TestOutputLogger<ViewSetRuntimeGuardTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
