using Microsoft.CodeAnalysis;
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
            : StaticDataTable<RecTable, Rec>(records);

        """;

    [Fact]
    public void Reports_SDP0305_when_viewset_member_is_not_a_view()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            public sealed partial class SomeView(GenManager.TableSet tables)
                : StaticDataView<SomeView, GenManager.TableSet>(tables);

            public partial class GenManager(ILogger logger)
                : StaticDataManager<GenManager.TableSet, GenManager.ViewSet>(logger)
            {
                public sealed partial record TableSet(RecTable? Recs);
                public sealed partial record ViewSet(int V);
            }
            """;

        var diagnostics = SourceGeneratorTestHelper.Run(Header + source).Results.SelectMany(r => r.Diagnostics).ToList();

        Assert.Contains(diagnostics, d => d.Id == "SDP0305" && d.Severity == DiagnosticSeverity.Error);

        logger.LogInformation(
            "Reported diagnostics: [{Ids}]",
            string.Join(", ", diagnostics.Select(d => d.Id).Distinct()));
    }

    [Fact]
    public void Reports_SDP0302_when_viewset_member_is_nullable()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            public sealed partial class SomeView(GenManager.TableSet tables)
                : StaticDataView<SomeView, GenManager.TableSet>(tables);

            public partial class GenManager(ILogger logger)
                : StaticDataManager<GenManager.TableSet, GenManager.ViewSet>(logger)
            {
                public sealed partial record TableSet(RecTable? Recs);
                public sealed partial record ViewSet(SomeView? V);
            }
            """;

        var diagnostics = SourceGeneratorTestHelper.Run(Header + source).Results.SelectMany(r => r.Diagnostics).ToList();

        Assert.Contains(diagnostics, d => d.Id == "SDP0302" && d.Severity == DiagnosticSeverity.Error);

        logger.LogInformation(
            "Reported diagnostics: [{Ids}]",
            string.Join(", ", diagnostics.Select(d => d.Id).Distinct()));
    }

    [Fact]
    public void Reports_SDP0303_when_view_is_not_partial()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            public sealed class SomeView : StaticDataView<SomeView, GenManager.TableSet>
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

        Assert.Contains(diagnostics, d => d.Id == "SDP0303" && d.Severity == DiagnosticSeverity.Error);

        logger.LogInformation(
            "Reported diagnostics: [{Ids}]",
            string.Join(", ", diagnostics.Select(d => d.Id).Distinct()));
    }

    [Fact]
    public void Reports_SDP0304_when_view_lacks_tableset_constructor()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            public sealed partial class SomeView : StaticDataView<SomeView, GenManager.TableSet>
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

        Assert.Contains(diagnostics, d => d.Id == "SDP0304" && d.Severity == DiagnosticSeverity.Error);

        logger.LogInformation(
            "Reported diagnostics: [{Ids}]",
            string.Join(", ", diagnostics.Select(d => d.Id).Distinct()));
    }

    [Fact]
    public void Reports_SDP0307_when_viewset_is_shared_by_managers_with_different_tablesets()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            public sealed partial class SomeView(ManagerA.TableSet tables)
                : StaticDataView<SomeView, ManagerA.TableSet>(tables);

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

        logger.LogInformation(
            "Reported diagnostics: [{Ids}]",
            string.Join(", ", diagnostics.Select(d => d.Id).Distinct()));
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
                : StaticDataView<SomeView, OtherManager.TableSet>(tables);

            public partial class GenManager(ILogger logger)
                : StaticDataManager<GenManager.TableSet, GenManager.ViewSet>(logger)
            {
                public sealed partial record TableSet(RecTable? Recs);
                public sealed partial record ViewSet(SomeView V);
            }
            """;

        var diagnostics = SourceGeneratorTestHelper.Run(Header + source).Results.SelectMany(r => r.Diagnostics).ToList();

        Assert.Contains(diagnostics, d => d.Id == "SDP0308" && d.Severity == DiagnosticSeverity.Error);

        logger.LogInformation(
            "Reported diagnostics: [{Ids}]",
            string.Join(", ", diagnostics.Select(d => d.Id).Distinct()));
    }

    [Fact]
    public void Does_not_report_for_valid_viewset()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            public sealed partial class SomeView(GenManager.TableSet tables)
                : StaticDataView<SomeView, GenManager.TableSet>(tables);

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

        logger.LogInformation(
            "Reported diagnostics: [{Ids}]",
            string.Join(", ", diagnostics.Select(d => d.Id).Distinct()));
    }

    private TestOutputLogger<ViewSetDiagnosticsTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Information);
        if (factory.CreateLogger<ViewSetDiagnosticsTests>()
            is not TestOutputLogger<ViewSetDiagnosticsTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
