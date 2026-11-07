using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.SourceGeneratorTests;

public class NestedRecordForeignKeyGeneratorTests(ITestOutputHelper testOutputHelper)
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
            Currency,
        }

        [StaticDataRecord("Item", "S")]
        public sealed partial record ItemRec([Key] int Id, string Name);

        public sealed partial class ItemTable : StaticDataTable<ItemRec>;

        [StaticDataRecord("Currency", "S")]
        public sealed partial record CurrencyRec([Key] int Id, string Name);

        public sealed partial class CurrencyTable : StaticDataTable<CurrencyRec>;

        """;

    // language=C#
    private const string MonsterManager =
        """

        public sealed partial class MonsterTable : StaticDataTable<MonsterRec>;

        public partial class GenManager(ILogger logger)
            : StaticDataManager<GenManager.TableSet>(logger)
        {
            public sealed partial record TableSet(ItemTable? Item, CurrencyTable? Currency, MonsterTable? Monster);
        }
        """;

    [Fact]
    public void Nested_foreign_keys_compile_without_warnings()
    {
        var logger = CreateLogger();

        // language=C#
        const string source =
            """
            public sealed record Point(int X, int Y);

            public sealed record Reward([ForeignKey("Item", "Id")] int ItemId, int Count);

            public sealed record Spawn(Point Position, Reward Reward);

            public sealed record Drop([ForeignKey("Item", "Id")] int ItemId, int Rate);

            public sealed record Objective(
                RewardKind Kind,
                [SwitchForeignKey(nameof(Kind), "Item", "Item", "Id")]
                [SwitchForeignKey(nameof(Kind), "Currency", "Currency", "Id")]
                int TargetId);

            public sealed record LootSlot([Key] int Slot, [ForeignKey("Item", "Id")] int ItemId);

            [StaticDataRecord("Monster", "S")]
            public sealed partial record MonsterRec(
                [Key] int Id,
                Spawn Spawn,
                [Length(2)] ImmutableArray<Drop> Drops,
                [Length(2)] FrozenSet<Objective> Objectives,
                [Length(2)] FrozenDictionary<int, LootSlot> Lootbox);
            """;

        var output = SourceGeneratorTestHelper.RunWithFinal(Header + source + MonsterManager);
        var diagnostics = output.Run.Results.SelectMany(r => r.Diagnostics).ToList();
        Assert.Empty(diagnostics);

        var tree = SourceGeneratorTestHelper.GetSingleTree(output.Run, "TableSetLoader.g.cs");
        var generated = tree.ToString();
        Assert.Contains("var __s1 = __r.Spawn;", generated, StringComparison.Ordinal);
        Assert.Contains("var __s2 = __s1.Reward;", generated, StringComparison.Ordinal);
        Assert.Contains("for (var __i1 = 0; __i1 < __r.Drops.Length; __i1++)", generated, StringComparison.Ordinal);
        Assert.Contains("foreach (var __s1 in __r.Objectives)", generated, StringComparison.Ordinal);
        Assert.Contains("foreach (var __s1 in __r.Lootbox.Values)", generated, StringComparison.Ordinal);
        Assert.Contains("MonsterRec[Id={__r.Id}].Drops[{__i1}]", generated, StringComparison.Ordinal);
        Assert.Contains("MonsterRec[Id={__r.Id}].Lootbox[Slot={__s1.Slot}]", generated, StringComparison.Ordinal);
        Assert.Contains("__s1.Kind == ", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("__r.Position", generated, StringComparison.Ordinal);

        var warningsAndErrors = SourceGeneratorTestHelper.GetCompilationWarningsAndErrors(output.Final);
        Assert.Empty(warningsAndErrors);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void Reports_SDP0205_when_nested_foreign_key_target_table_not_found()
    {
        var logger = CreateLogger();

        // language=C#
        const string source =
            """
            public sealed record Drop([ForeignKey("Missing", "Id")] int ItemId, int Rate);

            [StaticDataRecord("Monster", "S")]
            public sealed partial record MonsterRec([Key] int Id, [Length(2)] ImmutableArray<Drop> Drops);
            """;

        var diagnostic = Assert.Single(RunDiagnostics(source), d => d.Id == "SDP0205");
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void Reports_nested_record_diagnostic_once_when_record_is_used_on_two_paths()
    {
        var logger = CreateLogger();

        // language=C#
        const string source =
            """
            public sealed record Reward([ForeignKey("Missing", "Id")] int ItemId, int Count);

            [StaticDataRecord("Monster", "S")]
            public sealed partial record MonsterRec(
                [Key] int Id,
                Reward FirstKill,
                [Length(2)] ImmutableArray<Reward> Rewards);
            """;

        Assert.Single(RunDiagnostics(source), d => d.Id == "SDP0205");
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void Reports_SDP0209_when_nested_switch_condition_column_is_on_outer_record()
    {
        var logger = CreateLogger();

        // language=C#
        const string source =
            """
            public sealed record Drop(
                [SwitchForeignKey("DropKind", "Item", "Item", "Id")]
                [SwitchForeignKey("DropKind", "Currency", "Currency", "Id")]
                int TargetId,
                int Rate);

            [StaticDataRecord("Monster", "S")]
            public sealed partial record MonsterRec(
                [Key] int Id,
                RewardKind DropKind,
                [Length(2)] ImmutableArray<Drop> Drops);
            """;

        var diagnostic = Assert.Single(RunDiagnostics(source), d => d.Id == "SDP0209");
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Empty(logger.Logs);
    }

    [Theory]
    [InlineData("""[ForeignKey("Item", "Id")]""", "[ForeignKey]")]
    [InlineData("""[SwitchForeignKey("Kind", "Item", "Item", "Id")]""", "[SwitchForeignKey]")]
    public void Reports_SDP0229_when_foreign_key_is_inside_type_branding_record(string attribute, string expectedName)
    {
        var logger = CreateLogger();

        // language=C#
        var source =
            $$"""
            public readonly record struct ItemId({{attribute}} int Value);

            public sealed record Drop(ItemId Item, int Rate);

            [StaticDataRecord("Monster", "S")]
            public sealed partial record MonsterRec(
                [Key] int Id,
                ItemId MainItem,
                [Length(2)] ImmutableArray<Drop> Drops,
                [Length(2)] FrozenSet<ItemId> PickupItems);
            """;

        var diagnostic = Assert.Single(RunDiagnostics(source), d => d.Id == "SDP0229");
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        var message = diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture);
        Assert.Contains("'ItemId'", message, StringComparison.Ordinal);
        Assert.Contains("'Value'", message, StringComparison.Ordinal);
        Assert.Contains(expectedName, message, StringComparison.Ordinal);
        Assert.Empty(logger.Logs);
    }

    [Theory]
    [InlineData("""[ForeignKey("Item", "Id")]""", "[ForeignKey]")]
    [InlineData("""[SwitchForeignKey("Kind", "Item", "Item", "Id")]""", "[SwitchForeignKey]")]
    public void Reports_SDP0230_when_foreign_key_is_on_ignored_parameter(string attribute, string expectedName)
    {
        var logger = CreateLogger();

        // language=C#
        var source =
            $$"""
            [StaticDataRecord("Monster", "S")]
            public sealed partial record MonsterRec(
                [Key] int Id,
                RewardKind Kind,
                [Ignore] {{attribute}} int LegacyItemId);
            """;

        var diagnostic = Assert.Single(RunDiagnostics(source), d => d.Id == "SDP0230");
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        var message = diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture);
        Assert.Contains("'MonsterRec'", message, StringComparison.Ordinal);
        Assert.Contains("'LegacyItemId'", message, StringComparison.Ordinal);
        Assert.Contains(expectedName, message, StringComparison.Ordinal);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void Reports_SDP0230_when_foreign_key_is_on_ignored_parameter_of_nested_record()
    {
        var logger = CreateLogger();

        // language=C#
        const string source =
            """
            public sealed record Drop(int Rate, [Ignore] [ForeignKey("Item", "Id")] int LegacyItemId);

            [StaticDataRecord("Monster", "S")]
            public sealed partial record MonsterRec([Key] int Id, [Length(2)] ImmutableArray<Drop> Drops);
            """;

        var diagnostic = Assert.Single(RunDiagnostics(source), d => d.Id == "SDP0230");
        var message = diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture);
        Assert.Contains("'Drop'", message, StringComparison.Ordinal);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void Allows_foreign_key_on_single_parameter_table_record()
    {
        var logger = CreateLogger();

        // language=C#
        const string source =
            """
            [StaticDataRecord("Pickup", "S")]
            public sealed partial record PickupRec([ForeignKey("Item", "Id")] int ItemId);

            public sealed partial class PickupTable : StaticDataTable<PickupRec>;

            public partial class GenManager(ILogger logger)
                : StaticDataManager<GenManager.TableSet>(logger)
            {
                public sealed partial record TableSet(ItemTable? Item, PickupTable? Pickup);
            }
            """;

        var diagnostics = SourceGeneratorTestHelper.Run(Header + source)
            .Results
            .SelectMany(r => r.Diagnostics)
            .ToList();
        Assert.Empty(diagnostics);
        Assert.Empty(logger.Logs);
    }

    private static List<Diagnostic> RunDiagnostics(string monsterSource)
        => SourceGeneratorTestHelper.Run(Header + monsterSource + MonsterManager)
            .Results
            .SelectMany(r => r.Diagnostics)
            .ToList();

    private TestOutputLogger<NestedRecordForeignKeyGeneratorTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        var created = factory.CreateLogger<NestedRecordForeignKeyGeneratorTests>();
        if (created is not TestOutputLogger<NestedRecordForeignKeyGeneratorTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
