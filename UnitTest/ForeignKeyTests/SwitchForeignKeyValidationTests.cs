using System.Collections.Immutable;
using Sdp.Attributes;
using Sdp.Csv;
using Sdp.Manager;
using Sdp.Table;

namespace UnitTest.ForeignKeyTests;

public class SwitchForeignKeyValidationTests
{
    private enum RewardType
    {
        Item,
        Character,
        Gold,
        None,
    }

    private const string ValidQuestCsv =
        """
        QuestId,RewardType,RewardId
        1,Item,101
        2,Character,5
        3,Gold,1
        """;

    private const string ErrorQuestCsv =
        """
        QuestId,RewardType,RewardId
        1,Item,101
        2,Character,999
        3,Gold,1
        """;

    private const string NoConditionQuestCsv =
        """
        QuestId,RewardType,RewardId
        1,None,0
        """;

    // RewardId=5는 CharacterTable에 존재하지만 ItemTable에는 없음
    // SwitchFK는 RewardType=Item일 때 ItemTable만 검사해야 하므로 실패해야 함
    private const string CrossTableQuestCsv =
        """
        QuestId,RewardType,RewardId
        1,Item,5
        """;

    private const string ItemCsv =
        """
        Id,Name
        101,전설의 검
        102,용의 방패
        """;

    private const string CharacterCsv =
        """
        Id,Name
        5,기사
        6,마법사
        """;

    private const string CurrencyCsv =
        """
        Id,Name
        1,골드
        2,다이아
        """;

    private record Quest(
        int QuestId,
        RewardType RewardType,
        [SwitchForeignKey("RewardType", "Item",      "Item",      "Id")]
        [SwitchForeignKey("RewardType", "Character", "Character", "Id")]
        [SwitchForeignKey("RewardType", "Gold",      "Currency",  "Id")]
        int RewardId);

    private record Item(int Id, string Name);
    private record Character(int Id, string Name);
    private record Currency(int Id, string Name);

    private sealed class QuestTable(ImmutableList<Quest> records)
        : StaticDataTable<QuestTable, Quest>(records);

    private sealed class ItemTable(ImmutableList<Item> records)
        : StaticDataTable<ItemTable, Item>(records);

    private sealed class CharacterTable(ImmutableList<Character> records)
        : StaticDataTable<CharacterTable, Character>(records);

    private sealed class CurrencyTable(ImmutableList<Currency> records)
        : StaticDataTable<CurrencyTable, Currency>(records);

    private sealed class StaticData : StaticDataManager<StaticData.TableSet>
    {
        public sealed record TableSet(
            QuestTable? Quest,
            ItemTable? Item,
            CharacterTable? Character,
            CurrencyTable? Currency);

        public QuestTable QuestTable => Current.Quest!;

        public void Load()
            => Load(new TableSet(
                new(CsvLoader.Parse<Quest>(ValidQuestCsv)),
                new(CsvLoader.Parse<Item>(ItemCsv)),
                new(CsvLoader.Parse<Character>(CharacterCsv)),
                new(CsvLoader.Parse<Currency>(CurrencyCsv))));

        public void LoadWithErrorQuest()
            => Load(new TableSet(
                new(CsvLoader.Parse<Quest>(ErrorQuestCsv)),
                new(CsvLoader.Parse<Item>(ItemCsv)),
                new(CsvLoader.Parse<Character>(CharacterCsv)),
                new(CsvLoader.Parse<Currency>(CurrencyCsv))));

        public void LoadWithNoConditionQuest()
            => Load(new TableSet(
                new(CsvLoader.Parse<Quest>(NoConditionQuestCsv)),
                new(CsvLoader.Parse<Item>(ItemCsv)),
                new(CsvLoader.Parse<Character>(CharacterCsv)),
                new(CsvLoader.Parse<Currency>(CurrencyCsv))));

        public void LoadWithCrossTableQuest()
            => Load(new TableSet(
                new(CsvLoader.Parse<Quest>(CrossTableQuestCsv)),
                new(CsvLoader.Parse<Item>(ItemCsv)),
                new(CsvLoader.Parse<Character>(CharacterCsv)),
                new(CsvLoader.Parse<Currency>(CurrencyCsv))));
    }

    [Fact]
    public void Load_ValidData_SucceedsWithoutException()
    {
        var staticData = new StaticData();

        staticData.Load();

        Assert.Equal(3, staticData.QuestTable.Records.Count);
    }

    [Fact]
    public void Load_SwitchFkViolation_ThrowsWhenConditionMatchesButValueMissing()
    {
        var staticData = new StaticData();

        var ex = Assert.Throws<AggregateException>(staticData.LoadWithErrorQuest);

        Assert.Single(ex.InnerExceptions);
        Assert.Contains("999", ex.InnerExceptions[0].Message);
    }

    [Fact]
    public void Load_SwitchFk_UnmatchedCondition_SkipsValidation()
    {
        var staticData = new StaticData();

        staticData.LoadWithNoConditionQuest();

        var quest = Assert.Single(staticData.QuestTable.Records);
        Assert.Equal(RewardType.None, quest.RewardType);
    }

    [Fact]
    public void Load_SwitchFk_OnlyChecksMatchedTable_ThrowsWhenValueExistsOnlyInOtherConditionTable()
    {
        // RewardId=5는 CharacterTable에 있지만 ItemTable에는 없음
        // RewardType=Item이므로 ItemTable만 검사 → 실패해야 함 (OR 검사가 아님을 증명)
        var staticData = new StaticData();

        var ex = Assert.Throws<AggregateException>(staticData.LoadWithCrossTableQuest);

        Assert.Single(ex.InnerExceptions);
        Assert.Contains("5", ex.InnerExceptions[0].Message);
        Assert.Contains("when RewardType=Item", ex.InnerExceptions[0].Message);
    }
}

public class SwitchForeignKeyConfigurationErrorTests
{
    // conditionColumnName이 Record에 없는 경우
    private const string BadConditionQuestCsv =
        """
        Id,RewardId
        1,10
        """;

    private const string TargetCsv =
        """
        Id
        10
        """;

    private record BadConditionQuest(
        int Id,
        [SwitchForeignKey("NonExistentColumn", "Item", "Target", "Id")]
        int RewardId);

    private record Target(int Id);

    private sealed class BadConditionQuestTable(ImmutableList<BadConditionQuest> records)
        : StaticDataTable<BadConditionQuestTable, BadConditionQuest>(records);

    private sealed class TargetTable(ImmutableList<Target> records)
        : StaticDataTable<TargetTable, Target>(records);

    private sealed class ConditionColumnStaticData : StaticDataManager<ConditionColumnStaticData.TableSet>
    {
        public sealed record TableSet(BadConditionQuestTable? Quest, TargetTable? Target);

        public void Load()
            => Load(new TableSet(
                new(CsvLoader.Parse<BadConditionQuest>(BadConditionQuestCsv)),
                new(CsvLoader.Parse<Target>(TargetCsv))));
    }

    // tableSetName이 TableSet에 없는 경우
    private enum RewardType
    {
        Item,
    }

    private const string BadTargetQuestCsv =
        """
        Id,RewardType,RewardId
        1,Item,1
        """;

    private record BadTargetQuest(
        int Id,
        RewardType RewardType,
        [SwitchForeignKey("RewardType", "Item", "NonExistentTable", "Id")]
        int RewardId);

    private sealed class BadTargetQuestTable(ImmutableList<BadTargetQuest> records)
        : StaticDataTable<BadTargetQuestTable, BadTargetQuest>(records);

    private sealed class TargetTableStaticData : StaticDataManager<TargetTableStaticData.TableSet>
    {
        public sealed record TableSet(BadTargetQuestTable? Quest);

        public void Load()
            => Load(new TableSet(new(CsvLoader.Parse<BadTargetQuest>(BadTargetQuestCsv))));
    }

    [Fact]
    public void Load_ConditionColumnNotFound_ThrowsAggregateException()
    {
        var staticData = new ConditionColumnStaticData();

        var ex = Assert.Throws<AggregateException>(staticData.Load);

        Assert.Single(ex.InnerExceptions);
        Assert.Contains("NonExistentColumn", ex.InnerExceptions[0].Message);
    }

    [Fact]
    public void Load_TargetTableNotFound_ThrowsAggregateException()
    {
        var staticData = new TargetTableStaticData();

        var ex = Assert.Throws<AggregateException>(staticData.Load);

        Assert.Single(ex.InnerExceptions);
        Assert.Contains("NonExistentTable", ex.InnerExceptions[0].Message);
    }
}
