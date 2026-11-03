using System.Globalization;
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
        using System.Collections.Frozen;
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
            : StaticDataTable<TargetRec>(records);

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
                : StaticDataTable<ConsumerRec>(records);

            public partial class GenManager(ILogger logger)
                : StaticDataManager<GenManager.TableSet>(logger)
            {
                public sealed partial record TableSet(TargetTable? Target, ConsumerTable? Consumer);
            }
            """;

        AssertReportedAsError(source, "SDP0204");
        Assert.Empty(logger.Logs);
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
                : StaticDataTable<ConsumerRec>(records);

            public partial class GenManager(ILogger logger)
                : StaticDataManager<GenManager.TableSet>(logger)
            {
                public sealed partial record TableSet(TargetTable? Target, ConsumerTable? Consumer);
            }
            """;

        AssertReportedAsError(source, "SDP0205");
        Assert.Empty(logger.Logs);
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
                : StaticDataTable<ConsumerRec>(records);

            public partial class GenManager(ILogger logger)
                : StaticDataManager<GenManager.TableSet>(logger)
            {
                public sealed partial record TableSet(TargetTable? Target, ConsumerTable? Consumer);
            }
            """;

        AssertReportedAsError(source, "SDP0206");
        Assert.Empty(logger.Logs);
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
                : StaticDataTable<ConsumerRec>(records);

            public partial class GenManager(ILogger logger)
                : StaticDataManager<GenManager.TableSet>(logger)
            {
                public sealed partial record TableSet(TargetTable? Target, ConsumerTable? Consumer);
            }
            """;

        AssertReportedAsError(source, "SDP0207");
        Assert.Empty(logger.Logs);
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
                : StaticDataTable<ConsumerRec>(records);

            public partial class GenManager(ILogger logger)
                : StaticDataManager<GenManager.TableSet>(logger)
            {
                public sealed partial record TableSet(TargetTable? Target, ConsumerTable? Consumer);
            }
            """;

        AssertReportedAsError(source, "SDP0208");
        Assert.Empty(logger.Logs);
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
                : StaticDataTable<ConsumerRec>(records);

            public partial class GenManager(ILogger logger)
                : StaticDataManager<GenManager.TableSet>(logger)
            {
                public sealed partial record TableSet(TargetTable? Target, ConsumerTable? Consumer);
            }
            """;

        AssertReportedAsError(source, "SDP0209");
        Assert.Empty(logger.Logs);
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
                : StaticDataTable<ConsumerRec>(records);

            public partial class GenManager(ILogger logger)
                : StaticDataManager<GenManager.TableSet>(logger)
            {
                public sealed partial record TableSet(TargetTable? Target, ConsumerTable? Consumer);
            }
            """;

        AssertReportedAsError(source, "SDP0210");
        Assert.Empty(logger.Logs);
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
                : StaticDataTable<ConsumerRec>(records);

            public partial class GenManager(ILogger logger)
                : StaticDataManager<GenManager.TableSet>(logger)
            {
                public sealed partial record TableSet(TargetTable? Target, ConsumerTable? Consumer);
            }
            """;

        AssertReportedAsError(source, "SDP0211");
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void Reports_SDP0225_when_SwitchForeignKey_condition_values_are_equivalent()
    {
        var logger = CreateLogger();

        // "Item"과 "0"은 표기만 다른 같은 enum 값 — 두 번째 branch는 도달 불가.
        // language=C#
        const string source =
            """
            [StaticDataRecord("Consumer", "S")]
            public sealed partial record ConsumerRec(
                int Id,
                RewardKind Kind,
                [SwitchForeignKey("Kind", "Item", "Target", "Id")]
                [SwitchForeignKey("Kind", "0", "Target", "Id")]
                int RefId);

            public sealed partial class ConsumerTable(ImmutableArray<ConsumerRec> records)
                : StaticDataTable<ConsumerRec>(records);

            public partial class GenManager(ILogger logger)
                : StaticDataManager<GenManager.TableSet>(logger)
            {
                public sealed partial record TableSet(TargetTable? Target, ConsumerTable? Consumer);
            }
            """;

        AssertReportedAsError(source, "SDP0225");
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void Reports_SDP0226_when_ForeignKey_target_is_duplicated()
    {
        var logger = CreateLogger();

        // language=C#
        const string source =
            """
            [StaticDataRecord("Consumer", "S")]
            public sealed partial record ConsumerRec(
                int Id,
                [ForeignKey("Target", "Id")]
                [ForeignKey("Target", "Id")]
                int RefId);

            public sealed partial class ConsumerTable(ImmutableArray<ConsumerRec> records)
                : StaticDataTable<ConsumerRec>(records);

            public partial class GenManager(ILogger logger)
                : StaticDataManager<GenManager.TableSet>(logger)
            {
                public sealed partial record TableSet(TargetTable? Target, ConsumerTable? Consumer);
            }
            """;

        AssertReportedAsError(source, "SDP0226");
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void Reports_SDP0227_when_SwitchForeignKey_condition_column_is_a_record()
    {
        var logger = CreateLogger();

        // language=C#
        const string source =
            """
            public sealed partial record KindId(int Value);

            [StaticDataRecord("Consumer", "S")]
            public sealed partial record ConsumerRec(
                int Id,
                KindId Kind,
                [SwitchForeignKey("Kind", "1", "Target", "Id")] int RefId);

            public sealed partial class ConsumerTable(ImmutableArray<ConsumerRec> records)
                : StaticDataTable<ConsumerRec>(records);

            public partial class GenManager(ILogger logger)
                : StaticDataManager<GenManager.TableSet>(logger)
            {
                public sealed partial record TableSet(TargetTable? Target, ConsumerTable? Consumer);
            }
            """;

        AssertReportedAsError(source, "SDP0227");
        Assert.Empty(logger.Logs);
    }

    [Theory]
    [InlineData("decimal Kind", "abc")]
    [InlineData("double Kind", "NaN")]
    [InlineData("float Kind", "1,5x")]
    [InlineData("[DateTimeFormat(\"yyyy-MM-dd\")] System.DateTime Kind", "2024-13-45")]
    [InlineData("[TimeSpanFormat(\"hh\\\\:mm\")] System.TimeSpan Kind", "1:30:00")]
    public void Reports_SDP0228_when_SwitchForeignKey_condition_value_is_not_parsable(string conditionParameter, string conditionValue)
    {
        var logger = CreateLogger();

        // language=C#
        var source =
            $$"""
            [StaticDataRecord("Consumer", "S")]
            public sealed partial record ConsumerRec(
                int Id,
                {{conditionParameter}},
                [SwitchForeignKey("Kind", "{{conditionValue}}", "Target", "Id")] int RefId);

            public sealed partial class ConsumerTable(ImmutableArray<ConsumerRec> records)
                : StaticDataTable<ConsumerRec>(records);

            public partial class GenManager(ILogger logger)
                : StaticDataManager<GenManager.TableSet>(logger)
            {
                public sealed partial record TableSet(TargetTable? Target, ConsumerTable? Consumer);
            }
            """;

        AssertReportedAsError(source, "SDP0228");
    }

    [Fact]
    public void Reports_SDP0225_when_decimal_condition_values_differ_only_in_notation()
    {
        var logger = CreateLogger();

        // language=C#
        const string source =
            """
            [StaticDataRecord("Consumer", "S")]
            public sealed partial record ConsumerRec(
                int Id,
                decimal Tier,
                [SwitchForeignKey("Tier", "1.0", "Target", "Id")]
                [SwitchForeignKey("Tier", "1", "Target", "Id")] int RefId);

            public sealed partial class ConsumerTable(ImmutableArray<ConsumerRec> records)
                : StaticDataTable<ConsumerRec>(records);

            public partial class GenManager(ILogger logger)
                : StaticDataManager<GenManager.TableSet>(logger)
            {
                public sealed partial record TableSet(TargetTable? Target, ConsumerTable? Consumer);
            }
            """;

        AssertReportedAsError(source, "SDP0225");
        Assert.Empty(logger.Logs);
    }

    [Theory]
    [InlineData("[SingleColumnCollection(\",\")] ImmutableArray<string> Refs")]
    [InlineData("[Length(2)] ImmutableArray<TargetRec> Refs")]
    [InlineData("[Length(2)] FrozenDictionary<int, TargetRec> Refs")]
    public void Reports_SDP0211_when_collection_element_type_mismatches_target(string parameterDeclaration)
    {
        var logger = CreateLogger();

        // language=C#
        var source =
            $$"""
            [StaticDataRecord("Consumer", "S")]
            public sealed partial record ConsumerRec(
                int Id,
                [ForeignKey("Target", "Id")] {{parameterDeclaration}});

            public sealed partial class ConsumerTable(ImmutableArray<ConsumerRec> records)
                : StaticDataTable<ConsumerRec>(records);

            public partial class GenManager(ILogger logger)
                : StaticDataManager<GenManager.TableSet>(logger)
            {
                public sealed partial record TableSet(TargetTable? Target, ConsumerTable? Consumer);
            }
            """;

        AssertReportedAsError("using System.Collections.Frozen;\n" + source, "SDP0211");
    }

    [Theory]
    [InlineData("[SingleColumnCollection(\",\")] ImmutableArray<int> Refs")]
    [InlineData("[Length(2)] [NullString(\"\")] ImmutableArray<int?> Refs")]
    [InlineData("[Length(2)] FrozenSet<int> Refs")]
    public void Does_not_report_when_collection_element_type_matches_target(string parameterDeclaration)
    {
        var logger = CreateLogger();

        // language=C#
        var source =
            $$"""
            [StaticDataRecord("Consumer", "S")]
            public sealed partial record ConsumerRec(
                int Id,
                [ForeignKey("Target", "Id")] {{parameterDeclaration}});

            public sealed partial class ConsumerTable(ImmutableArray<ConsumerRec> records)
                : StaticDataTable<ConsumerRec>(records);

            public partial class GenManager(ILogger logger)
                : StaticDataManager<GenManager.TableSet>(logger)
            {
                public sealed partial record TableSet(TargetTable? Target, ConsumerTable? Consumer);
            }
            """;

        var diagnostics = RunDiagnostics("using System.Collections.Frozen;\n" + source);
        Assert.Empty(diagnostics);
    }

    // language=C#
    private const string BrandingTargets =
        """
        public enum ConsumerKind
        {
            Item,
            Currency,
        }

        [StaticDataRecord("Item", "S")]
        public sealed partial record ItemRec([Key] ItemRec.ItemId Id, string Name)
        {
            public readonly record struct ItemId(int Value);
        }

        public sealed partial class ItemTable(ImmutableArray<ItemRec> records)
            : StaticDataTable<ItemRec>(records);

        [StaticDataRecord("Currency", "S")]
        public sealed partial record CurrencyRec([Key] CurrencyRec.CurrencyId Id, string Name)
        {
            public readonly record struct CurrencyId(int Value);
        }

        public sealed partial class CurrencyTable(ImmutableArray<CurrencyRec> records)
            : StaticDataTable<CurrencyRec>(records);

        [StaticDataRecord("Npc", "S")]
        public sealed partial record NpcRec([Key] NpcRec.NpcId Id, string Name)
        {
            public readonly record struct NpcId([NullString("")] int? Value);
        }

        public sealed partial class NpcTable(ImmutableArray<NpcRec> records)
            : StaticDataTable<NpcRec>(records);

        """;

    private static string BuildBrandingSource(string consumerParameters)
    {
        // language=C#
        const string template =
            """
            [StaticDataRecord("Consumer", "S")]
            public sealed partial record ConsumerRec(
                int Id,
                {{CONSUMER_PARAMETERS}});

            public sealed partial class ConsumerTable(ImmutableArray<ConsumerRec> records)
                : StaticDataTable<ConsumerRec>(records);

            public partial class GenManager(ILogger logger)
                : StaticDataManager<GenManager.TableSet>(logger)
            {
                public sealed partial record TableSet(
                    TargetTable? Target,
                    ItemTable? Item,
                    CurrencyTable? Currency,
                    NpcTable? Npc,
                    ConsumerTable? Consumer);
            }
            """;

        return BrandingTargets + template.Replace("{{CONSUMER_PARAMETERS}}", consumerParameters, StringComparison.Ordinal);
    }

    [Fact]
    public void Allows_primitive_column_to_reference_branding_key()
    {
        var logger = CreateLogger();

        var source = BuildBrandingSource("[ForeignKey(\"Item\", \"Id\")] int ItemId");

        var diagnostics = RunDiagnostics(source);
        Assert.Empty(diagnostics);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void Allows_primitive_column_with_two_branding_targets()
    {
        var logger = CreateLogger();

        var source = BuildBrandingSource("[ForeignKey(\"Item\", \"Id\")] [ForeignKey(\"Currency\", \"Id\")] int RewardId");

        var diagnostics = RunDiagnostics(source);
        Assert.Empty(diagnostics);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void Allows_primitive_collection_element_to_reference_branding_key()
    {
        var logger = CreateLogger();

        var source = BuildBrandingSource("[ForeignKey(\"Item\", \"Id\")] [Length(2)] FrozenSet<int> ItemIds");

        var diagnostics = RunDiagnostics(source);
        Assert.Empty(diagnostics);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void Allows_nullable_primitive_column_to_reference_branding_key()
    {
        var logger = CreateLogger();

        var source = BuildBrandingSource("[ForeignKey(\"Item\", \"Id\")] [NullString(\"\")] int? ItemId");

        var diagnostics = RunDiagnostics(source);
        Assert.Empty(diagnostics);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void Allows_primitive_switch_column_over_two_branding_targets()
    {
        var logger = CreateLogger();

        // language=C#
        const string consumerParameters =
            """
            ConsumerKind Kind,
            [SwitchForeignKey("Kind", "Item", "Item", "Id")]
            [SwitchForeignKey("Kind", "Currency", "Currency", "Id")]
            int TargetId
            """;

        var source = BuildBrandingSource(consumerParameters);

        var diagnostics = RunDiagnostics(source);
        Assert.Empty(diagnostics);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void Reports_SDP0211_when_primitive_column_references_branding_key_with_nullable_value()
    {
        var logger = CreateLogger();

        var source = BuildBrandingSource("[ForeignKey(\"Npc\", \"Id\")] int NpcId");

        AssertReportedAsError(source, "SDP0211");
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void Reports_SDP0211_with_branding_value_type_when_branding_key_value_is_nullable()
    {
        var logger = CreateLogger();

        var source = BuildBrandingSource("[ForeignKey(\"Npc\", \"Id\")] int NpcId");

        var diagnostics = RunDiagnostics(source);
        var diagnostic = Assert.Single(diagnostics, d => d.Id == "SDP0211");
        Assert.Contains("NpcId(int? Value)", diagnostic.GetMessage(CultureInfo.InvariantCulture));
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void Reports_SDP0211_when_branding_column_references_primitive_key()
    {
        var logger = CreateLogger();

        var source = BuildBrandingSource("[ForeignKey(\"Target\", \"Id\")] ItemRec.ItemId RefId");

        AssertReportedAsError(source, "SDP0211");
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void Reports_SDP0211_when_branding_column_references_different_branding_key()
    {
        var logger = CreateLogger();

        var source = BuildBrandingSource("[ForeignKey(\"Currency\", \"Id\")] ItemRec.ItemId RefId");

        AssertReportedAsError(source, "SDP0211");
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void Reports_SDP0211_when_primitive_column_type_differs_from_branding_value()
    {
        var logger = CreateLogger();

        var source = BuildBrandingSource("[ForeignKey(\"Item\", \"Id\")] long ItemId");

        AssertReportedAsError(source, "SDP0211");
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void Allows_branding_collection_element_to_reference_same_branding_key()
    {
        var logger = CreateLogger();

        var source = BuildBrandingSource("[ForeignKey(\"Item\", \"Id\")] [Length(2)] FrozenSet<ItemRec.ItemId> ItemIds");

        var diagnostics = RunDiagnostics(source);
        Assert.Empty(diagnostics);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void Allows_branding_array_element_to_reference_same_branding_key()
    {
        var logger = CreateLogger();

        var source = BuildBrandingSource("[ForeignKey(\"Item\", \"Id\")] [Length(2)] ImmutableArray<ItemRec.ItemId> ItemIds");

        var diagnostics = RunDiagnostics(source);
        Assert.Empty(diagnostics);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void Reports_SDP0211_when_branding_collection_element_references_other_branding_key()
    {
        var logger = CreateLogger();

        var source = BuildBrandingSource("[ForeignKey(\"Currency\", \"Id\")] [Length(2)] FrozenSet<ItemRec.ItemId> ItemIds");

        AssertReportedAsError(source, "SDP0211");
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void Reports_SDP0211_when_branding_collection_element_references_primitive_key()
    {
        var logger = CreateLogger();

        var source = BuildBrandingSource("[ForeignKey(\"Target\", \"Id\")] [Length(2)] FrozenSet<ItemRec.ItemId> ItemIds");

        AssertReportedAsError(source, "SDP0211");
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void Allows_branding_collection_element_with_switch_foreign_key()
    {
        var logger = CreateLogger();

        // language=C#
        const string source =
            """
            public enum PickupKind
            {
                Item,
                Season1Item,
            }

            [StaticDataRecord("Item", "S")]
            public sealed partial record ItemRec([Key] ItemRec.ItemId Id, string Name)
            {
                public readonly record struct ItemId(int Value);
            }

            public sealed partial class ItemTable(ImmutableArray<ItemRec> records)
                : StaticDataTable<ItemRec>(records);

            [StaticDataRecord("Season1Item", "S")]
            public sealed partial record Season1ItemRec([Key] ItemRec.ItemId Id, string Name);

            public sealed partial class Season1ItemTable(ImmutableArray<Season1ItemRec> records)
                : StaticDataTable<Season1ItemRec>(records);

            [StaticDataRecord("Consumer", "S")]
            public sealed partial record ConsumerRec(
                int Id,
                PickupKind Kind,
                [SwitchForeignKey("Kind", "Item", "Item", "Id")]
                [SwitchForeignKey("Kind", "Season1Item", "Season1Item", "Id")]
                [Length(2)]
                FrozenSet<ItemRec.ItemId> PickupItemIds);

            public sealed partial class ConsumerTable(ImmutableArray<ConsumerRec> records)
                : StaticDataTable<ConsumerRec>(records);

            public partial class GenManager(ILogger logger)
                : StaticDataManager<GenManager.TableSet>(logger)
            {
                public sealed partial record TableSet(
                    TargetTable? Target,
                    ItemTable? Item,
                    Season1ItemTable? Season1Item,
                    ConsumerTable? Consumer);
            }
            """;

        var diagnostics = RunDiagnostics(source);
        Assert.Empty(diagnostics);
        Assert.Empty(logger.Logs);
    }

    private TestOutputLogger<ForeignKeyDiagnosticsTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<ForeignKeyDiagnosticsTests>()
            is not TestOutputLogger<ForeignKeyDiagnosticsTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
