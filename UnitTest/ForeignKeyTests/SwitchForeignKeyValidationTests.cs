using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using Sdp.Attributes;
using Sdp.Manager;
using Sdp.Table;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.ForeignKeyTests;

public partial class SwitchForeignKeyValidationTests(ITestOutputHelper testOutputHelper)
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

    private const string MultipleNoConditionQuestCsv =
        """
        QuestId,RewardType,RewardId
        1,None,0
        2,None,0
        3,None,0
        """;

    private const string MixedMatchedAndUnmatchedQuestCsv =
        """
        QuestId,RewardType,RewardId
        1,Item,101
        2,None,0
        3,Character,5
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
    private partial record QuestRecord(
        int QuestId,
        RewardType RewardType,
        [SwitchForeignKey("RewardType", "Item",      "Item",      "Id")]
        [SwitchForeignKey("RewardType", "Character", "Character", "Id")]
        [SwitchForeignKey("RewardType", "Gold",      "Currency",  "Id")]
        int RewardId);

    [StaticDataRecord("Item", "Sheet1")]
    private partial record ItemRecord(int Id, string Name);

    [StaticDataRecord("Character", "Sheet1")]
    private partial record CharacterRecord(int Id, string Name);

    [StaticDataRecord("Currency", "Sheet1")]
    private partial record CurrencyRecord(int Id, string Name);

    private sealed partial class QuestTable(ImmutableArray<QuestRecord> records)
        : StaticDataTable<QuestTable, QuestRecord>(records);

    private sealed partial class ItemTable(ImmutableArray<ItemRecord> records)
        : StaticDataTable<ItemTable, ItemRecord>(records);

    private sealed partial class CharacterTable(ImmutableArray<CharacterRecord> records)
        : StaticDataTable<CharacterTable, CharacterRecord>(records);

    private sealed partial class CurrencyTable(ImmutableArray<CurrencyRecord> records)
        : StaticDataTable<CurrencyTable, CurrencyRecord>(records);

    private sealed partial class StaticData(ILogger logger)
        : StaticDataManager<StaticData.TableSet>(logger)
    {
        public sealed partial record TableSet(
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

        Assert.Equal(3, staticData.QuestTable.Records.Length);
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
    public async Task Load_SwitchFk_UnmatchedCondition_ThrowsAggregateException()
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

        var ex = await Assert.ThrowsAsync<AggregateException>(() => staticData.LoadAsync(dir.Path));

        var inner = Assert.Single(ex.InnerExceptions);
        Assert.Contains("QuestRecord", inner.Message);
        Assert.Contains("RewardId", inner.Message);
        Assert.Contains("RewardType=None", inner.Message);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public async Task Load_SwitchFk_MultipleUnmatchedRows_AggregatesAllErrors()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<SwitchForeignKeyValidationTests>() is not TestOutputLogger<SwitchForeignKeyValidationTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        using var dir = new CsvTestDirectory();
        dir.Write("Quest.Sheet1.csv", MultipleNoConditionQuestCsv);
        WriteFixedCsvs(dir);

        var staticData = new StaticData(logger);

        var ex = await Assert.ThrowsAsync<AggregateException>(() => staticData.LoadAsync(dir.Path));

        Assert.Equal(3, ex.InnerExceptions.Count);
        Assert.All(ex.InnerExceptions, inner => Assert.Contains("RewardType=None", inner.Message));
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public async Task Load_SwitchFk_MixedMatchedAndUnmatched_ReportsOnlyUnmatched()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<SwitchForeignKeyValidationTests>() is not TestOutputLogger<SwitchForeignKeyValidationTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        using var dir = new CsvTestDirectory();
        dir.Write("Quest.Sheet1.csv", MixedMatchedAndUnmatchedQuestCsv);
        WriteFixedCsvs(dir);

        var staticData = new StaticData(logger);

        var ex = await Assert.ThrowsAsync<AggregateException>(() => staticData.LoadAsync(dir.Path));

        var inner = Assert.Single(ex.InnerExceptions);
        Assert.Contains("RewardType=None", inner.Message);
        Assert.DoesNotContain("RewardType=Item", inner.Message);
        Assert.DoesNotContain("RewardType=Character", inner.Message);
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

public partial class SwitchForeignKeyConfigurationErrorTests(ITestOutputHelper testOutputHelper)
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

#pragma warning disable SDP0209 // SwitchForeignKey condition column not found — 의도된 invalid 구조. 런타임 거부 검증.
    [StaticDataRecord("BadConditionQuest", "Sheet1")]
    private partial record BadConditionQuestRecord(
        int Id,
        [SwitchForeignKey("NonExistentColumn", "Item", "Target", "Id")]
        int RewardId);
#pragma warning restore SDP0209

    [StaticDataRecord("Target", "Sheet1")]
    private partial record TargetRecord(int Id);

    private sealed partial class BadConditionQuestTable(ImmutableArray<BadConditionQuestRecord> records)
        : StaticDataTable<BadConditionQuestTable, BadConditionQuestRecord>(records);

    private sealed partial class TargetTable(ImmutableArray<TargetRecord> records)
        : StaticDataTable<TargetTable, TargetRecord>(records);

    private sealed partial class ConditionColumnStaticData(ILogger logger)
        : StaticDataManager<ConditionColumnStaticData.TableSet>(logger)
    {
        public sealed partial record TableSet(BadConditionQuestTable? Quest, TargetTable? Target);

        public BadConditionQuestTable QuestTable => Current.Quest!;
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

#pragma warning disable SDP0205 // FK target TableSet member not found — 의도된 invalid 구조. 런타임 거부 검증.
    [StaticDataRecord("BadTargetQuest", "Sheet1")]
    private partial record BadTargetQuestRecord(
        int Id,
        RewardType RewardType,
        [SwitchForeignKey("RewardType", "Item", "NonExistentTable", "Id")]
        int RewardId);
#pragma warning restore SDP0205

    private sealed partial class BadTargetQuestTable(ImmutableArray<BadTargetQuestRecord> records)
        : StaticDataTable<BadTargetQuestTable, BadTargetQuestRecord>(records);

    private sealed partial class TargetTableStaticData(ILogger logger)
        : StaticDataManager<TargetTableStaticData.TableSet>(logger)
    {
        public sealed partial record TableSet(BadTargetQuestTable? Quest);

        public BadTargetQuestTable QuestTable => Current.Quest!;
    }

    // 이 클래스의 invalid 구조(NonExistentColumn, NonExistentTable) 거부는
    // SG 컴파일타임 진단(SDP0205/SDP0209)으로 검증된다. record 정의는 #pragma disable 로 남긴다.
    // 런타임에서는 SG-emit ValidateForeignKeys 가 해당 SwitchForeignKey 분기를 만들지 않으므로
    // FK 검사가 일어나지 않는다. 아래 테스트가 그 동작을 검증한다.
    [Fact]
    public async Task Load_SwitchFkConditionColumnNotFound_SkipsFkValidation()
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
        await staticData.LoadAsync(dir.Path);

        Assert.Single(staticData.QuestTable.Records);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public async Task Load_SwitchFkTargetTableNotFound_SkipsFkValidation()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<SwitchForeignKeyConfigurationErrorTests>() is not TestOutputLogger<SwitchForeignKeyConfigurationErrorTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        using var dir = new CsvTestDirectory();
        dir.Write("BadTargetQuest.Sheet1.csv", BadTargetQuestCsv);

        var staticData = new TargetTableStaticData(logger);
        await staticData.LoadAsync(dir.Path);

        Assert.Single(staticData.QuestTable.Records);
        Assert.Empty(logger.Logs);
    }
}
