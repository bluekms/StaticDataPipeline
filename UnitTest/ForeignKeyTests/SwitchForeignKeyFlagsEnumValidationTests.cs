using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using Sdp.Attributes;
using Sdp.Manager;
using Sdp.Table;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.ForeignKeyTests;

public partial class SwitchForeignKeyFlagsEnumValidationTests(ITestOutputHelper testOutputHelper)
{
    [Flags]
    private enum Access
    {
        None = 0,
        Read = 1,
        Write = 2,
        Execute = 4,
        ReadWrite = Read | Write,
    }

    private const string ValidGrantCsv =
        """
        GrantId,Access,RefId
        1,Read,101
        2,ReadWrite,5
        3,3,5
        """;

    private const string UnmatchedConditionGrantCsv =
        """
        GrantId,Access,RefId
        1,Write,101
        """;

    private const string MissingFkValueGrantCsv =
        """
        GrantId,Access,RefId
        1,ReadWrite,999
        """;

    private const string ItemCsv =
        """
        Id,Name
        101,전설의 검
        """;

    private const string CharacterCsv =
        """
        Id,Name
        5,기사
        """;

    [StaticDataRecord("Grant", "Sheet1")]
    private partial record GrantRecord(
        int GrantId,
        Access Access,
        [SwitchForeignKey("Access", "Read",        "Item",      "Id")]
        [SwitchForeignKey("Access", "Read, Write", "Character", "Id")]
        int RefId);

    [StaticDataRecord("Item", "Sheet1")]
    private partial record ItemRecord(int Id, string Name);

    [StaticDataRecord("Character", "Sheet1")]
    private partial record CharacterRecord(int Id, string Name);

    private sealed partial class GrantTable(ImmutableArray<GrantRecord> records)
        : StaticDataTable<GrantRecord>(records);

    private sealed partial class ItemTable(ImmutableArray<ItemRecord> records)
        : StaticDataTable<ItemRecord>(records);

    private sealed partial class CharacterTable(ImmutableArray<CharacterRecord> records)
        : StaticDataTable<CharacterRecord>(records);

    private sealed partial class FlagsStaticData(ILogger logger)
        : StaticDataManager<FlagsStaticData.TableSet>(logger)
    {
        public sealed partial record TableSet(
            GrantTable? Grant,
            ItemTable? Item,
            CharacterTable? Character);

        public GrantTable GrantTable => Current.Grant!;
    }

    private static void WriteFixedCsvs(CsvTestDirectory dir)
    {
        dir.Write("Item.Sheet1.csv", ItemCsv);
        dir.Write("Character.Sheet1.csv", CharacterCsv);
    }

    [Fact]
    public async Task Load_FlagsCombinationConditionValue_MatchesNamedComboAndNumericCells()
    {
        var logger = CreateLogger();

        using var dir = new CsvTestDirectory();
        dir.Write("Grant.Sheet1.csv", ValidGrantCsv);
        WriteFixedCsvs(dir);

        var staticData = new FlagsStaticData(logger);
        await staticData.LoadAsync(dir.Path);

        Assert.Equal(3, staticData.GrantTable.Records.Length);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public async Task Load_FlagsUnmatchedCondition_ThrowsAggregateException()
    {
        var logger = CreateLogger();

        using var dir = new CsvTestDirectory();
        dir.Write("Grant.Sheet1.csv", UnmatchedConditionGrantCsv);
        WriteFixedCsvs(dir);

        var staticData = new FlagsStaticData(logger);
        var ex = await Assert.ThrowsAsync<AggregateException>(() => staticData.LoadAsync(dir.Path));
        var inner = Assert.Single(ex.InnerExceptions);
        Assert.Contains("Access=Write", inner.Message);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public async Task Load_FlagsMatchedBranch_MissingFkValue_Throws()
    {
        var logger = CreateLogger();

        using var dir = new CsvTestDirectory();
        dir.Write("Grant.Sheet1.csv", MissingFkValueGrantCsv);
        WriteFixedCsvs(dir);

        var staticData = new FlagsStaticData(logger);
        var ex = await Assert.ThrowsAsync<AggregateException>(() => staticData.LoadAsync(dir.Path));
        var inner = Assert.Single(ex.InnerExceptions);
        Assert.Contains("999", inner.Message);
        Assert.Empty(logger.Logs);
    }

    private TestOutputLogger<SwitchForeignKeyFlagsEnumValidationTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<SwitchForeignKeyFlagsEnumValidationTests>()
            is not TestOutputLogger<SwitchForeignKeyFlagsEnumValidationTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
