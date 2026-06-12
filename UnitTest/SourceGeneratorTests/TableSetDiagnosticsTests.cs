using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.SourceGeneratorTests;

// 매니저 선언 형태 오류는 조용히 생성이 누락되면 미구현 추상 멤버(CS0534)만 남으므로,
// 전용 진단(SDP0218/SDP0219)이 Error 로 보고되는지 잠근다.
public class TableSetDiagnosticsTests(ITestOutputHelper testOutputHelper)
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
    public void Reports_SDP0218_when_manager_passes_TableSet_type_parameter_through()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            public partial class MidManager<TTableSet>(ILogger logger)
                : StaticDataManager<TTableSet>(logger)
                where TTableSet : class;
            """;

        var diagnostics = SourceGeneratorTestHelper.Run(Header + source).Results.SelectMany(r => r.Diagnostics).ToList();

        Assert.Contains(diagnostics, d => d.Id == "SDP0218" && d.Severity == DiagnosticSeverity.Error);

        logger.LogInformation(
            "Reported diagnostics: [{Ids}]",
            string.Join(", ", diagnostics.Select(d => d.Id).Distinct()));
    }

    [Fact]
    public void Reports_SDP0218_when_manager_passes_ViewSet_type_parameter_through()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            public partial class GenManager(ILogger logger)
                : StaticDataManager<GenManager.TableSet>(logger)
            {
                public sealed partial record TableSet(RecTable? Recs);
            }

            public partial class MidViewManager<TViewSet>(ILogger logger)
                : StaticDataManager<GenManager.TableSet, TViewSet>(logger)
                where TViewSet : class;
            """;

        var diagnostics = SourceGeneratorTestHelper.Run(Header + source).Results.SelectMany(r => r.Diagnostics).ToList();

        Assert.Contains(diagnostics, d => d.Id == "SDP0218" && d.Severity == DiagnosticSeverity.Error);

        logger.LogInformation(
            "Reported diagnostics: [{Ids}]",
            string.Join(", ", diagnostics.Select(d => d.Id).Distinct()));
    }

    [Fact]
    public void Reports_SDP0219_when_manager_is_not_partial()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            public sealed class GenManager(ILogger logger)
                : StaticDataManager<GenManager.TableSet>(logger)
            {
                public sealed partial record TableSet(RecTable? Recs);
            }
            """;

        var diagnostics = SourceGeneratorTestHelper.Run(Header + source).Results.SelectMany(r => r.Diagnostics).ToList();

        Assert.Contains(diagnostics, d => d.Id == "SDP0219" && d.Severity == DiagnosticSeverity.Error);

        logger.LogInformation(
            "Reported diagnostics: [{Ids}]",
            string.Join(", ", diagnostics.Select(d => d.Id).Distinct()));
    }

    [Fact]
    public void Does_not_report_SDP0219_when_manager_is_partial()
    {
        var logger = CreateLogger();

        // language=C#
        const string source = """
            public sealed partial class GenManager(ILogger logger)
                : StaticDataManager<GenManager.TableSet>(logger)
            {
                public sealed partial record TableSet(RecTable? Recs);
            }
            """;

        var diagnostics = SourceGeneratorTestHelper.Run(Header + source).Results.SelectMany(r => r.Diagnostics).ToList();

        Assert.DoesNotContain(diagnostics, d => d.Id == "SDP0219");

        logger.LogInformation(
            "Reported diagnostics: [{Ids}]",
            string.Join(", ", diagnostics.Select(d => d.Id).Distinct()));
    }

    private TestOutputLogger<TableSetDiagnosticsTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Information);
        if (factory.CreateLogger<TableSetDiagnosticsTests>()
            is not TestOutputLogger<TableSetDiagnosticsTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
