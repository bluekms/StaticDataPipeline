using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.SourceGeneratorTests;

public class ViewSetDiagnosticsTests(ITestOutputHelper testOutputHelper)
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
    public void Reports_SDP0305_when_viewset_member_is_not_a_view()
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
                public sealed partial record ViewSet(int V);
            }
            """;

        var diagnostics = SourceGeneratorTestHelper.Run(Header + source).Results.SelectMany(r => r.Diagnostics).ToList();
        Assert.Contains(diagnostics, d => d.Id == "SDP0305" && d.Severity == DiagnosticSeverity.Error);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void Reports_SDP0302_when_viewset_member_is_nullable()
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

        var diagnostics = SourceGeneratorTestHelper.Run(Header + source).Results.SelectMany(r => r.Diagnostics).ToList();
        Assert.Contains(diagnostics, d => d.Id == "SDP0302" && d.Severity == DiagnosticSeverity.Error);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void Reports_SDP0303_when_view_is_not_partial()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            public sealed class SomeView : StaticDataView<GenManager.TableSet>
            {
                public SomeView(GenManager.TableSet tables) : base(tables) { }
            }

            public partial class GenManager(ILogger logger)
                : StaticDataManager<GenManager.TableSet, GenManager.ViewSet>(logger)
            {
                public sealed partial record TableSet(RecTable? Recs);
                public sealed partial record ViewSet(SomeView V);
            }
            """;

        var diagnostics = SourceGeneratorTestHelper.Run(Header + source).Results.SelectMany(r => r.Diagnostics).ToList();
        var diagnostic = Assert.Single(diagnostics, d => d.Id == "SDP0303");
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void Reports_SDP0002_once_when_view_containing_type_is_not_partial()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            public static class Outer
            {
                public sealed partial class SomeView(GenManager.TableSet tables)
                    : StaticDataView<GenManager.TableSet>(tables);
            }

            public partial class GenManager(ILogger logger)
                : StaticDataManager<GenManager.TableSet, GenManager.ViewSet>(logger)
            {
                public sealed partial record TableSet(RecTable? Recs);
                public sealed partial record ViewSet(Outer.SomeView V);
            }
            """;

        var diagnostics = SourceGeneratorTestHelper.Run(Header + source).Results.SelectMany(r => r.Diagnostics).ToList();
        var diagnostic = Assert.Single(diagnostics, d => d.Id == "SDP0002");
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void Reports_SDP0304_when_view_lacks_tableset_constructor()
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

        var diagnostics = SourceGeneratorTestHelper.Run(Header + source).Results.SelectMany(r => r.Diagnostics).ToList();
        var diagnostic = Assert.Single(diagnostics, d => d.Id == "SDP0304");
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void Reports_SDP0303_and_SDP0304_together_when_view_is_not_partial_and_lacks_constructor()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            public sealed class SomeView : StaticDataView<GenManager.TableSet>
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

        var diagnostics = SourceGeneratorTestHelper.Run(Header + source).Results.SelectMany(r => r.Diagnostics).ToList();
        Assert.Contains(diagnostics, d => d.Id == "SDP0303" && d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(diagnostics, d => d.Id == "SDP0304" && d.Severity == DiagnosticSeverity.Error);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void Reports_SDP0307_when_viewset_is_shared_by_managers_with_different_tablesets()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            public sealed partial class SomeView(ManagerA.TableSet tables)
                : StaticDataView<ManagerA.TableSet>(tables);

            public sealed partial record SharedViewSet(SomeView V);

            public partial class ManagerA(ILogger logger)
                : StaticDataManager<ManagerA.TableSet, SharedViewSet>(logger)
            {
                public sealed partial record TableSet(RecTable? Recs);
            }

            public partial class ManagerB(ILogger logger)
                : StaticDataManager<ManagerB.TableSet, SharedViewSet>(logger)
            {
                public sealed partial record TableSet(RecTable? Recs);
            }
            """;

        var diagnostics = SourceGeneratorTestHelper.Run(Header + source).Results.SelectMany(r => r.Diagnostics).ToList();
        Assert.Contains(diagnostics, d => d.Id == "SDP0307" && d.Severity == DiagnosticSeverity.Error);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void Reports_SDP0307_once_per_conflicting_tableset()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            public sealed partial record TableSetA(RecTable? Recs);
            public sealed partial record TableSetB(RecTable? Recs);
            public sealed partial record TableSetC(RecTable? Recs);

            public sealed partial class SomeView(TableSetA tables)
                : StaticDataView<TableSetA>(tables);

            public sealed partial record SharedViewSet(SomeView V);

            public partial class ManagerA(ILogger logger)
                : StaticDataManager<TableSetA, SharedViewSet>(logger);

            public partial class ManagerB(ILogger logger)
                : StaticDataManager<TableSetB, SharedViewSet>(logger);

            public partial class ManagerC(ILogger logger)
                : StaticDataManager<TableSetB, SharedViewSet>(logger);

            public partial class ManagerD(ILogger logger)
                : StaticDataManager<TableSetC, SharedViewSet>(logger);
            """;

        var diagnostics = SourceGeneratorTestHelper.Run(Header + source).Results.SelectMany(r => r.Diagnostics).ToList();
        Assert.Equal(2, diagnostics.Count(d => d.Id == "SDP0307"));
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void Reports_SDP0308_when_view_targets_a_different_tableset()
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

        var diagnostics = SourceGeneratorTestHelper.Run(Header + source).Results.SelectMany(r => r.Diagnostics).ToList();
        Assert.Contains(diagnostics, d => d.Id == "SDP0308" && d.Severity == DiagnosticSeverity.Error);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void Does_not_report_for_valid_viewset()
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

        var diagnostics = SourceGeneratorTestHelper.Run(Header + source).Results.SelectMany(r => r.Diagnostics).ToList();
        Assert.DoesNotContain(diagnostics, d => d.Id == "SDP0302");
        Assert.DoesNotContain(diagnostics, d => d.Id == "SDP0303");
        Assert.DoesNotContain(diagnostics, d => d.Id == "SDP0304");
        Assert.DoesNotContain(diagnostics, d => d.Id == "SDP0305");
        Assert.DoesNotContain(diagnostics, d => d.Id == "SDP0308");
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void Reports_SDP0309_and_emits_not_supported_shell_when_view_is_declared_in_other_assembly()
    {
        var logger = CreateLogger();

        // language=C#
        const string librarySource = """
            using Sdp.View;

            namespace External;

            public sealed record ExternalTableSet(int Dummy);

            public sealed partial class ExternalView(ExternalTableSet tables)
                : StaticDataView<ExternalTableSet>(tables);
            """;

        // language=C#
        const string source = """
            public partial class GenManager(ILogger logger)
                : StaticDataManager<GenManager.TableSet, GenManager.ViewSet>(logger)
            {
                public sealed partial record TableSet(RecTable? Recs);
                public sealed partial record ViewSet(External.ExternalView V);
            }
            """;

        var library = SourceGeneratorTestHelper.CompileLibrary(librarySource, "ExternalViews");
        var result = SourceGeneratorTestHelper.RunWithFinal(Header + source, LanguageVersion.Latest, [library]).Run;
        var diagnostics = result.Results.SelectMany(r => r.Diagnostics).ToList();
        Assert.Contains(diagnostics, d => d.Id == "SDP0309" && d.Severity == DiagnosticSeverity.Error);
        Assert.DoesNotContain(diagnostics, d => d.Id == "SDP0308");

        var generated = SourceGeneratorTestHelper.GetSingleTree(result, "ViewSetBuilder.g.cs").ToString();
        Assert.Contains("ViewSetBuildHelper.CannotEmitError(", generated);
        Assert.DoesNotContain("ViewNotPartialError", generated);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void Does_not_emit_bridge_when_viewset_is_not_partial()
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
                public sealed record ViewSet(SomeView V);
            }
            """;

        var result = SourceGeneratorTestHelper.Run(Header + source);
        var diagnostics = result.Results.SelectMany(r => r.Diagnostics).ToList();
        Assert.Contains(diagnostics, d => d.Id == "SDP0301" && d.Severity == DiagnosticSeverity.Error);
        Assert.DoesNotContain(result.GeneratedTrees, t => t.FilePath.EndsWith("ViewSetBuilder.g.cs", StringComparison.Ordinal));
        Assert.DoesNotContain(result.GeneratedTrees, t => t.FilePath.EndsWith("StaticDataManager.g.cs", StringComparison.Ordinal));
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void Reports_SDP0306_without_SDP0006_when_viewset_is_not_a_record()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            public partial class GenManager(ILogger logger)
                : StaticDataManager<GenManager.TableSet, GenManager.ViewSet>(logger)
            {
                public sealed partial record TableSet(RecTable? Recs);
                public sealed partial class ViewSet { }
            }
            """;

        var diagnostics = SourceGeneratorTestHelper.Run(Header + source).Results.SelectMany(r => r.Diagnostics).ToList();
        Assert.Contains(diagnostics, d => d.Id == "SDP0306" && d.Severity == DiagnosticSeverity.Error);
        Assert.DoesNotContain(diagnostics, d => d.Id == "SDP0006");
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void Reports_viewset_diagnostics_once_when_managers_share_viewset_and_tableset()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            public sealed partial record SharedTableSet(RecTable? Recs);

            public sealed partial class SomeView(SharedTableSet tables)
                : StaticDataView<SharedTableSet>(tables);

            public sealed partial record SharedViewSet(SomeView? V);

            public partial class ManagerA(ILogger logger)
                : StaticDataManager<SharedTableSet, SharedViewSet>(logger);

            public partial class ManagerB(ILogger logger)
                : StaticDataManager<SharedTableSet, SharedViewSet>(logger);
            """;

        var result = SourceGeneratorTestHelper.Run(Header + source);
        var diagnostics = result.Results.SelectMany(r => r.Diagnostics).ToList();
        Assert.Single(diagnostics, d => d.Id == "SDP0302");
        Assert.DoesNotContain(diagnostics, d => d.Id == "SDP0307");
        SourceGeneratorTestHelper.GetSingleTree(result, "ViewSetBuilder.g.cs");
        Assert.Empty(logger.Logs);
    }

    private TestOutputLogger<ViewSetDiagnosticsTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<ViewSetDiagnosticsTests>()
            is not TestOutputLogger<ViewSetDiagnosticsTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
