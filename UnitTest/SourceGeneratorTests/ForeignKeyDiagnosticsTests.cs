using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.SourceGeneratorTests;

// FK/SwitchFK 오설정은 런타임에서 검사가 생성되지 않아 "조용히 건너뛰는" 것처럼 보이지만,
// 실제로는 아래 진단들이 컴파일타임 Error 로 빌드를 실패시켜 출하를 막는다.
// 이 테스트는 (1) 각 진단이 방출되는지, (2) 심각도가 Error 인지(= Warning 으로 격하되어 조용히
// 통과되지 않는지)를 잠가, SG 리팩터로 진단이 사라지면 곧바로 실패하도록 한다.
public class ForeignKeyDiagnosticsTests(ITestOutputHelper testOutputHelper)
{
    // language=C#
    private const string Header =
        """
        using Microsoft.Extensions.Logging;
        using Sdp.Attributes;
        using Sdp.Manager;
        using Sdp.Table;
        using System.Collections.Immutable;

        namespace Test;

        public enum RewardKind
        {
            Item,
            Character,
        }

        [StaticDataRecord("Target", "S")]
        public sealed partial record TargetRec(
            int Id,
            [SingleColumnCollection(",")] ImmutableArray<string> Tags);

        public sealed partial class TargetTable(ImmutableArray<TargetRec> records)
            : StaticDataTable<TargetTable, TargetRec>(records);

        """;

    private static List<Diagnostic> RunDiagnostics(string consumerSource)
        => SourceGeneratorTestHelper.Run(Header + consumerSource)
            .Results
            .SelectMany(r => r.Diagnostics)
            .ToList();

    private static void AssertReportedAsError(string consumerSource, string diagnosticId)
    {
        var diagnostics = RunDiagnostics(consumerSource);

        Assert.Contains(diagnostics, d => d.Id == diagnosticId && d.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void Reports_SDP0204_when_ForeignKey_and_SwitchForeignKey_coexist()
    {
        var logger = CreateLogger();

        // language=C#
        const string source =
            """
            [StaticDataRecord("Consumer", "S")]
            public sealed partial record ConsumerRec(
                int Id,
                RewardKind Kind,
                [ForeignKey("Target", "Id")]
                [SwitchForeignKey("Kind", "Item", "Target", "Id")]
                int RefId);

            public sealed partial class ConsumerTable(ImmutableArray<ConsumerRec> records)
                : StaticDataTable<ConsumerTable, ConsumerRec>(records);

            public partial class GenManager(ILogger logger)
                : StaticDataManager<GenManager.TableSet>(logger)
            {
                public sealed partial record TableSet(TargetTable? Target, ConsumerTable? Consumer);
            }
            """;

        AssertReportedAsError(source, "SDP0204");

        logger.LogInformation("SDP0204 reported as Error.");
    }

    [Fact]
    public void Reports_SDP0205_when_ForeignKey_target_table_not_found()
    {
        var logger = CreateLogger();

        // language=C#
        const string source =
            """
            [StaticDataRecord("Consumer", "S")]
            public sealed partial record ConsumerRec(
                int Id,
                [ForeignKey("NonExistentTarget", "Id")] int RefId);

            public sealed partial class ConsumerTable(ImmutableArray<ConsumerRec> records)
                : StaticDataTable<ConsumerTable, ConsumerRec>(records);

            public partial class GenManager(ILogger logger)
                : StaticDataManager<GenManager.TableSet>(logger)
            {
                public sealed partial record TableSet(TargetTable? Target, ConsumerTable? Consumer);
            }
            """;

        AssertReportedAsError(source, "SDP0205");

        logger.LogInformation("SDP0205 reported as Error.");
    }

    [Fact]
    public void Reports_SDP0206_when_ForeignKey_target_column_not_found()
    {
        var logger = CreateLogger();

        // language=C#
        const string source =
            """
            [StaticDataRecord("Consumer", "S")]
            public sealed partial record ConsumerRec(
                int Id,
                [ForeignKey("Target", "NoSuchColumn")] int RefId);

            public sealed partial class ConsumerTable(ImmutableArray<ConsumerRec> records)
                : StaticDataTable<ConsumerTable, ConsumerRec>(records);

            public partial class GenManager(ILogger logger)
                : StaticDataManager<GenManager.TableSet>(logger)
            {
                public sealed partial record TableSet(TargetTable? Target, ConsumerTable? Consumer);
            }
            """;

        AssertReportedAsError(source, "SDP0206");

        logger.LogInformation("SDP0206 reported as Error.");
    }

    [Fact]
    public void Reports_SDP0207_when_ForeignKey_target_is_SingleColumnCollection()
    {
        var logger = CreateLogger();

        // language=C#
        const string source =
            """
            [StaticDataRecord("Consumer", "S")]
            public sealed partial record ConsumerRec(
                int Id,
                [ForeignKey("Target", "Tags")] string RefId);

            public sealed partial class ConsumerTable(ImmutableArray<ConsumerRec> records)
                : StaticDataTable<ConsumerTable, ConsumerRec>(records);

            public partial class GenManager(ILogger logger)
                : StaticDataManager<GenManager.TableSet>(logger)
            {
                public sealed partial record TableSet(TargetTable? Target, ConsumerTable? Consumer);
            }
            """;

        AssertReportedAsError(source, "SDP0207");

        logger.LogInformation("SDP0207 reported as Error.");
    }

    [Fact]
    public void Reports_SDP0208_when_SwitchForeignKey_condition_value_is_duplicated()
    {
        var logger = CreateLogger();

        // language=C#
        const string source =
            """
            [StaticDataRecord("Consumer", "S")]
            public sealed partial record ConsumerRec(
                int Id,
                RewardKind Kind,
                [SwitchForeignKey("Kind", "Item", "Target", "Id")]
                [SwitchForeignKey("Kind", "Item", "Target", "Id")]
                int RefId);

            public sealed partial class ConsumerTable(ImmutableArray<ConsumerRec> records)
                : StaticDataTable<ConsumerTable, ConsumerRec>(records);

            public partial class GenManager(ILogger logger)
                : StaticDataManager<GenManager.TableSet>(logger)
            {
                public sealed partial record TableSet(TargetTable? Target, ConsumerTable? Consumer);
            }
            """;

        AssertReportedAsError(source, "SDP0208");

        logger.LogInformation("SDP0208 reported as Error.");
    }

    [Fact]
    public void Reports_SDP0209_when_SwitchForeignKey_condition_column_not_found()
    {
        var logger = CreateLogger();

        // language=C#
        const string source =
            """
            [StaticDataRecord("Consumer", "S")]
            public sealed partial record ConsumerRec(
                int Id,
                [SwitchForeignKey("NoSuchColumn", "Item", "Target", "Id")] int RefId);

            public sealed partial class ConsumerTable(ImmutableArray<ConsumerRec> records)
                : StaticDataTable<ConsumerTable, ConsumerRec>(records);

            public partial class GenManager(ILogger logger)
                : StaticDataManager<GenManager.TableSet>(logger)
            {
                public sealed partial record TableSet(TargetTable? Target, ConsumerTable? Consumer);
            }
            """;

        AssertReportedAsError(source, "SDP0209");

        logger.LogInformation("SDP0209 reported as Error.");
    }

    [Fact]
    public void Reports_SDP0210_when_SwitchForeignKey_condition_columns_differ()
    {
        var logger = CreateLogger();

        // language=C#
        const string source =
            """
            [StaticDataRecord("Consumer", "S")]
            public sealed partial record ConsumerRec(
                int Id,
                RewardKind KindA,
                RewardKind KindB,
                [SwitchForeignKey("KindA", "Item", "Target", "Id")]
                [SwitchForeignKey("KindB", "Item", "Target", "Id")]
                int RefId);

            public sealed partial class ConsumerTable(ImmutableArray<ConsumerRec> records)
                : StaticDataTable<ConsumerTable, ConsumerRec>(records);

            public partial class GenManager(ILogger logger)
                : StaticDataManager<GenManager.TableSet>(logger)
            {
                public sealed partial record TableSet(TargetTable? Target, ConsumerTable? Consumer);
            }
            """;

        AssertReportedAsError(source, "SDP0210");

        logger.LogInformation("SDP0210 reported as Error.");
    }

    [Fact]
    public void Reports_SDP0211_when_ForeignKey_column_type_mismatches_target()
    {
        var logger = CreateLogger();

        // language=C#
        const string source =
            """
            [StaticDataRecord("Consumer", "S")]
            public sealed partial record ConsumerRec(
                int Id,
                [ForeignKey("Target", "Id")] string RefId);

            public sealed partial class ConsumerTable(ImmutableArray<ConsumerRec> records)
                : StaticDataTable<ConsumerTable, ConsumerRec>(records);

            public partial class GenManager(ILogger logger)
                : StaticDataManager<GenManager.TableSet>(logger)
            {
                public sealed partial record TableSet(TargetTable? Target, ConsumerTable? Consumer);
            }
            """;

        AssertReportedAsError(source, "SDP0211");

        logger.LogInformation("SDP0211 reported as Error.");
    }

    private TestOutputLogger<ForeignKeyDiagnosticsTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Information);
        if (factory.CreateLogger<ForeignKeyDiagnosticsTests>()
            is not TestOutputLogger<ForeignKeyDiagnosticsTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
