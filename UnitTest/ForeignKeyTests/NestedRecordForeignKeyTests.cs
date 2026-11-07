using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Sdp.Attributes;
using Sdp.Manager;
using Sdp.Table;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.ForeignKeyTests;

public partial class NestedRecordForeignKeyTests(ITestOutputHelper testOutputHelper)
{
    private const string ItemCsv =
        """
        Id,Name
        101,Sword
        102,Shield
        """;

    private const string CurrencyCsv =
        """
        Id
        1
        """;

    private const string MonsterHeader =
        "Id,FirstKill.ItemId,FirstKill.Count,Bonus.First.ItemId,Bonus.First.Count,Bonus.Second.ItemId,Bonus.Second.Count,"
        + "Drops[0].ItemId,Drops[0].Rate,Drops[1].ItemId,Drops[1].Rate,"
        + "Objectives[0].Kind,Objectives[0].TargetId,Objectives[1].Kind,Objectives[1].TargetId,"
        + "Lootbox[0].Slot,Lootbox[0].ItemId,Lootbox[1].Slot,Lootbox[1].ItemId";

    private const string ValidNodeCsv =
        """
        Id,Links[0].TargetId,Links[0].Weight,Links[1].TargetId,Links[1].Weight
        1,2,10,,0
        2,1,10,,0
        """;

    private enum RewardKind
    {
        Item,
        Currency,
    }

    [StaticDataRecord("Item", "Sheet1")]
    private partial record ItemRecord([Key] int Id, string Name);

    [StaticDataRecord("Currency", "Sheet1")]
    private partial record CurrencyRecord([Key] int Id);

    private sealed record Reward([ForeignKey("Item", "Id")] int ItemId, int Count);

    private sealed record RewardPair(Reward First, Reward Second);

    private sealed record Drop(
        [ForeignKey("Item", "Id")] int ItemId,
        int Rate,
        [Ignore] int LegacyItemId);

    private sealed record Objective(
        RewardKind Kind,
        [SwitchForeignKey(nameof(Kind), "Item", "Item", "Id")]
        [SwitchForeignKey(nameof(Kind), "Currency", "Currency", "Id")]
        int TargetId);

    private sealed record LootSlot([Key] int Slot, [ForeignKey("Item", "Id")] int ItemId);

    [StaticDataRecord("Monster", "Sheet1")]
    private partial record MonsterRecord(
        [Key] int Id,
        Reward FirstKill,
        RewardPair Bonus,
        [Length(2)] ImmutableArray<Drop> Drops,
        [Length(2)] FrozenSet<Objective> Objectives,
        [Length(2)] FrozenDictionary<int, LootSlot> Lootbox,
        [Ignore] int LegacyItemId);

    private sealed record Link([NullString("")] [ForeignKey("Node", "Id")] int? TargetId, int Weight);

    [StaticDataRecord("Node", "Sheet1")]
    private partial record NodeRecord([Key] int Id, [Length(2)] ImmutableArray<Link> Links);

    private sealed partial class ItemTable : StaticDataTable<ItemRecord>;

    private sealed partial class CurrencyTable : StaticDataTable<CurrencyRecord>;

    private sealed partial class MonsterTable : StaticDataTable<MonsterRecord>;

    private sealed partial class NodeTable : StaticDataTable<NodeRecord>;

    private sealed partial class NestedStaticData(ILogger logger)
        : StaticDataManager<NestedStaticData.TableSet>(logger)
    {
        public sealed partial record TableSet(
            ItemTable? Item,
            CurrencyTable? Currency,
            MonsterTable? Monster,
            NodeTable? Node);
    }

    private sealed record MonsterRow(
        int Id,
        int FirstKillItemId = 101,
        int BonusSecondItemId = 102,
        int SecondDropItemId = 102,
        int CurrencyObjectiveTargetId = 1,
        int SecondLootItemId = 102)
    {
        public string ToCsvLine()
        {
            return string.Create(
                CultureInfo.InvariantCulture,
                $"{Id},{FirstKillItemId},1,101,1,{BonusSecondItemId},1,101,50,{SecondDropItemId},50,Item,101,Currency,{CurrencyObjectiveTargetId},5,101,7,{SecondLootItemId}");
        }
    }

    [Fact]
    public async Task Load_AllNestedReferencesExist_Succeeds()
    {
        var logger = CreateLogger();

        using var dir = CreateDirectory([new MonsterRow(1)], ValidNodeCsv);

        var staticData = new NestedStaticData(logger);
        await staticData.LoadAsync(dir.Path);

        var monster = Assert.Single(staticData.Current.Monster!.Records);
        Assert.Equal(102, monster.Drops[1].ItemId);
        Assert.Equal(102, monster.Lootbox[7].ItemId);
        Assert.Equal(0, monster.LegacyItemId);
        Assert.Equal(0, monster.Drops[0].LegacyItemId);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public async Task Load_RecordInRecordPointsToMissingKey_ReportsMemberPath()
    {
        var logger = CreateLogger();

        using var dir = CreateDirectory([new MonsterRow(1, FirstKillItemId: 999)], ValidNodeCsv);

        var inner = await LoadAndGetSingleError(dir, logger);

        Assert.Contains("MonsterRecord[Id=1].FirstKill.ItemId(999)", inner.Message, StringComparison.Ordinal);
        Assert.Contains("Item.Id", inner.Message, StringComparison.Ordinal);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public async Task Load_SameNestedRecordOnSecondPathPointsToMissingKey_ReportsThatPath()
    {
        var logger = CreateLogger();

        using var dir = CreateDirectory([new MonsterRow(1, BonusSecondItemId: 999)], ValidNodeCsv);

        var inner = await LoadAndGetSingleError(dir, logger);

        Assert.Contains("MonsterRecord[Id=1].Bonus.Second.ItemId(999)", inner.Message, StringComparison.Ordinal);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public async Task Load_ArrayElementPointsToMissingKey_ReportsElementIndex()
    {
        var logger = CreateLogger();

        using var dir = CreateDirectory([new MonsterRow(1, SecondDropItemId: 999)], ValidNodeCsv);

        var inner = await LoadAndGetSingleError(dir, logger);

        Assert.Contains("MonsterRecord[Id=1].Drops[1].ItemId(999)", inner.Message, StringComparison.Ordinal);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public async Task Load_SetElementSwitchTargetMissing_ReportsPathWithoutIndexAndCondition()
    {
        var logger = CreateLogger();

        using var dir = CreateDirectory([new MonsterRow(1, CurrencyObjectiveTargetId: 999)], ValidNodeCsv);

        var inner = await LoadAndGetSingleError(dir, logger);

        Assert.Contains("MonsterRecord[Id=1].Objectives.TargetId(999)", inner.Message, StringComparison.Ordinal);
        Assert.Contains("Currency.Id", inner.Message, StringComparison.Ordinal);
        Assert.Contains("Kind=Currency", inner.Message, StringComparison.Ordinal);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public async Task Load_MapValuePointsToMissingKey_ReportsMapKey()
    {
        var logger = CreateLogger();

        using var dir = CreateDirectory([new MonsterRow(1, SecondLootItemId: 999)], ValidNodeCsv);

        var inner = await LoadAndGetSingleError(dir, logger);

        Assert.Contains("MonsterRecord[Id=1].Lootbox[Slot=7].ItemId(999)", inner.Message, StringComparison.Ordinal);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public async Task Load_NestedReferenceToOwnTable_ChecksAgainstOwnRecords()
    {
        var logger = CreateLogger();

        const string nodeCsv =
            """
            Id,Links[0].TargetId,Links[0].Weight,Links[1].TargetId,Links[1].Weight
            1,2,10,,0
            2,9,10,,0
            """;
        using var dir = CreateDirectory([new MonsterRow(1)], nodeCsv);

        var inner = await LoadAndGetSingleError(dir, logger);

        Assert.Contains("NodeRecord[Id=2].Links[0].TargetId(9)", inner.Message, StringComparison.Ordinal);
        Assert.Contains("Node.Id", inner.Message, StringComparison.Ordinal);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public async Task Load_TargetDisabled_ReportsEachNestedParameterOnce()
    {
        var logger = CreateLogger();

        using var dir = CreateDirectory([new MonsterRow(1), new MonsterRow(2)], ValidNodeCsv);

        var staticData = new NestedStaticData(logger);

        var ex = await Assert.ThrowsAsync<AggregateException>(
            () => staticData.LoadAsync(dir.Path, new List<string> { "Item" }));

        var messages = ex.InnerExceptions
            .Select(inner => inner.Message)
            .ToList();
        string[] expectedSources =
        [
            "MonsterRecord.FirstKill.ItemId",
            "MonsterRecord.Bonus.First.ItemId",
            "MonsterRecord.Bonus.Second.ItemId",
            "MonsterRecord.Drops.ItemId",
            "MonsterRecord.Objectives.TargetId",
            "MonsterRecord.Lootbox.ItemId",
        ];
        Assert.Equal(expectedSources.Length, messages.Count);
        foreach (var expectedSource in expectedSources)
        {
            Assert.Single(messages, message => message.Contains(expectedSource, StringComparison.Ordinal));
        }

        Assert.Empty(logger.Logs);
    }

    private static CsvTestDirectory CreateDirectory(List<MonsterRow> monsters, string nodeCsv)
    {
        var monsterLines = monsters
            .Select(monster => monster.ToCsvLine())
            .ToList();
        var monsterCsv = MonsterHeader + "\n" + string.Join("\n", monsterLines);

        var dir = new CsvTestDirectory();
        dir.Write("Item.Sheet1.csv", ItemCsv);
        dir.Write("Currency.Sheet1.csv", CurrencyCsv);
        dir.Write("Monster.Sheet1.csv", monsterCsv);
        dir.Write("Node.Sheet1.csv", nodeCsv);

        return dir;
    }

    private static async Task<Exception> LoadAndGetSingleError(CsvTestDirectory dir, ILogger logger)
    {
        var staticData = new NestedStaticData(logger);

        var ex = await Assert.ThrowsAsync<AggregateException>(() => staticData.LoadAsync(dir.Path));

        return Assert.Single(ex.InnerExceptions);
    }

    private TestOutputLogger<NestedRecordForeignKeyTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        var created = factory.CreateLogger<NestedRecordForeignKeyTests>();
        if (created is not TestOutputLogger<NestedRecordForeignKeyTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
