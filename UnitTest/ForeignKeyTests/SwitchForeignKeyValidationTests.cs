using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using Sdp.Attributes;
using Sdp.Manager;
using Sdp.Table;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.ForeignKeyTests;

public class SwitchForeignKeyValidationTests(ITestOutputHelper testOutputHelper)
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

    [StaticDataRecord("Quest", "Sheet1")]
    private record QuestRecord(
        int QuestId,
        RewardType RewardType,
        [SwitchForeignKey("RewardType", "Item",      "Item",      "Id")]
        [SwitchForeignKey("RewardType", "Character", "Character", "Id")]
        [SwitchForeignKey("RewardType", "Gold",      "Currency",  "Id")]
        int RewardId);

    [StaticDataRecord("Item", "Sheet1")]
    private record ItemRecord(int Id, string Name);

    [StaticDataRecord("Character", "Sheet1")]
    private record CharacterRecord(int Id, string Name);

    [StaticDataRecord("Currency", "Sheet1")]
    private record CurrencyRecord(int Id, string Name);

    private sealed class QuestTable(ImmutableList<QuestRecord> records)
        : StaticDataTable<QuestTable, QuestRecord>(records);

    private sealed class ItemTable(ImmutableList<ItemRecord> records)
        : StaticDataTable<ItemTable, ItemRecord>(records);

    private sealed class CharacterTable(ImmutableList<CharacterRecord> records)
        : StaticDataTable<CharacterTable, CharacterRecord>(records);

    private sealed class CurrencyTable(ImmutableList<CurrencyRecord> records)
        : StaticDataTable<CurrencyTable, CurrencyRecord>(records);

    private sealed class StaticData(ILogger logger)
        : StaticDataManager<StaticData.TableSet>(logger)
    {
        public sealed record TableSet(
            QuestTable? Quest,
            ItemTable? Item,
            CharacterTable? Character,
            CurrencyTable? Currency);

        public QuestTable QuestTable => Current.Quest!;
    }

    private static void WriteFixedCsvs(CsvTestDirectory dir)
    {
        dir.Write("Item.Sheet1.csv", ItemCsv);
        dir.Write("Character.Sheet1.csv", CharacterCsv);
        dir.Write("Currency.Sheet1.csv", CurrencyCsv);
    }

    [Fact]
    public async Task Load_ValidData_SucceedsWithoutException()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<SwitchForeignKeyValidationTests>() is not TestOutputLogger<SwitchForeignKeyValidationTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        using var dir = new CsvTestDirectory();
        dir.Write("Quest.Sheet1.csv", ValidQuestCsv);
        WriteFixedCsvs(dir);

        var staticData = new StaticData(logger);
        await staticData.LoadAsync(dir.Path);

        Assert.Equal(3, staticData.QuestTable.Records.Count);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public async Task Load_SwitchFkViolation_ThrowsWhenConditionMatchesButValueMissing()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<SwitchForeignKeyValidationTests>() is not TestOutputLogger<SwitchForeignKeyValidationTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        using var dir = new CsvTestDirectory();
        dir.Write("Quest.Sheet1.csv", ErrorQuestCsv);
        WriteFixedCsvs(dir);

        var staticData = new StaticData(logger);

        var ex = await Assert.ThrowsAsync<AggregateException>(() => staticData.LoadAsync(dir.Path));

        Assert.Single(ex.InnerExceptions);
        Assert.Contains("999", ex.InnerExceptions[0].Message);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public async Task Load_SwitchFk_UnmatchedCondition_SkipsValidation()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<SwitchForeignKeyValidationTests>() is not TestOutputLogger<SwitchForeignKeyValidationTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        using var dir = new CsvTestDirectory();
        dir.Write("Quest.Sheet1.csv", NoConditionQuestCsv);
        WriteFixedCsvs(dir);

        var staticData = new StaticData(logger);
        await staticData.LoadAsync(dir.Path);

        var quest = Assert.Single(staticData.QuestTable.Records);
        Assert.Equal(RewardType.None, quest.RewardType);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public async Task Load_SwitchFk_OnlyChecksMatchedTable_ThrowsWhenValueExistsOnlyInOtherConditionTable()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<SwitchForeignKeyValidationTests>() is not TestOutputLogger<SwitchForeignKeyValidationTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // RewardId=5는 CharacterTable에 있지만 ItemTable에는 없음
        // RewardType=Item이므로 ItemTable만 검사 → 실패해야 함 (OR 검사가 아님을 증명)
        using var dir = new CsvTestDirectory();
        dir.Write("Quest.Sheet1.csv", CrossTableQuestCsv);
        WriteFixedCsvs(dir);

        var staticData = new StaticData(logger);

        var ex = await Assert.ThrowsAsync<AggregateException>(() => staticData.LoadAsync(dir.Path));

        Assert.Single(ex.InnerExceptions);
        Assert.Contains("5", ex.InnerExceptions[0].Message);
        Assert.Contains("when RewardType=Item", ex.InnerExceptions[0].Message);
        Assert.Empty(logger.Logs);
    }
}

public class SwitchForeignKeyConfigurationErrorTests(ITestOutputHelper testOutputHelper)
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

    [StaticDataRecord("BadConditionQuest", "Sheet1")]
    private record BadConditionQuestRecord(
        int Id,
        [SwitchForeignKey("NonExistentColumn", "Item", "Target", "Id")]
        int RewardId);

    [StaticDataRecord("Target", "Sheet1")]
    private record TargetRecord(int Id);

    private sealed class BadConditionQuestTable(ImmutableList<BadConditionQuestRecord> records)
        : StaticDataTable<BadConditionQuestTable, BadConditionQuestRecord>(records);

    private sealed class TargetTable(ImmutableList<TargetRecord> records)
        : StaticDataTable<TargetTable, TargetRecord>(records);

    private sealed class ConditionColumnStaticData(ILogger logger)
        : StaticDataManager<ConditionColumnStaticData.TableSet>(logger)
    {
        public sealed record TableSet(BadConditionQuestTable? Quest, TargetTable? Target);
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

    [StaticDataRecord("BadTargetQuest", "Sheet1")]
    private record BadTargetQuestRecord(
        int Id,
        RewardType RewardType,
        [SwitchForeignKey("RewardType", "Item", "NonExistentTable", "Id")]
        int RewardId);

    private sealed class BadTargetQuestTable(ImmutableList<BadTargetQuestRecord> records)
        : StaticDataTable<BadTargetQuestTable, BadTargetQuestRecord>(records);

    private sealed class TargetTableStaticData(ILogger logger)
        : StaticDataManager<TargetTableStaticData.TableSet>(logger)
    {
        public sealed record TableSet(BadTargetQuestTable? Quest);
    }

    [Fact]
    public async Task Load_ConditionColumnNotFound_ThrowsAggregateException()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<SwitchForeignKeyConfigurationErrorTests>() is not TestOutputLogger<SwitchForeignKeyConfigurationErrorTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        using var dir = new CsvTestDirectory();
        dir.Write("BadConditionQuest.Sheet1.csv", BadConditionQuestCsv);
        dir.Write("Target.Sheet1.csv", TargetCsv);

        var staticData = new ConditionColumnStaticData(logger);

        var ex = await Assert.ThrowsAsync<AggregateException>(() => staticData.LoadAsync(dir.Path));

        Assert.Single(ex.InnerExceptions);
        Assert.Contains("NonExistentColumn", ex.InnerExceptions[0].Message);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public async Task Load_TargetTableNotFound_ThrowsAggregateException()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<SwitchForeignKeyConfigurationErrorTests>() is not TestOutputLogger<SwitchForeignKeyConfigurationErrorTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        using var dir = new CsvTestDirectory();
        dir.Write("BadTargetQuest.Sheet1.csv", BadTargetQuestCsv);

        var staticData = new TargetTableStaticData(logger);

        var ex = await Assert.ThrowsAsync<AggregateException>(() => staticData.LoadAsync(dir.Path));

        Assert.Single(ex.InnerExceptions);
        Assert.Contains("NonExistentTable", ex.InnerExceptions[0].Message);
        Assert.Empty(logger.Logs);
    }
}
