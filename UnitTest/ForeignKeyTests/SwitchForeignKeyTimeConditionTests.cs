using Microsoft.Extensions.Logging;
using Sdp.Attributes;
using Sdp.Manager;
using Sdp.Table;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.ForeignKeyTests;

public partial class SwitchForeignKeyTimeConditionTests(ITestOutputHelper testOutputHelper)
{
    private const string EventCsv =
        """
        Id,Start,TargetId
        1,2024-01-02T00:00:00+09:00,101
        2,2024-01-01T15:00:00Z,101
        3,2024-07-01T00:00:00+09:00,5
        """;

    private const string SaleCsv =
        """
        Id,Start,TargetId
        1,2024-01-02 00:00,101
        2,2024-07-01 00:00,5
        """;

    private const string ItemCsv =
        """
        Id,Name
        101,Sword
        """;

    private const string CharacterCsv =
        """
        Id,Name
        5,Knight
        """;

    [StaticDataRecord("Event", "Sheet1")]
    private partial record EventRecord(
        int Id,
        [DateTimeFormat("yyyy-MM-ddTHH:mm:ssK")] DateTime Start,
        [SwitchForeignKey("Start", "2024-01-02T00:00:00+09:00", "Item", "Id")]
        [SwitchForeignKey("Start", "2024-07-01T00:00:00+09:00", "Character", "Id")]
        int TargetId);

    [StaticDataRecord("Sale", "Sheet1")]
    private partial record SaleRecord(
        int Id,
        [DateTimeFormat("yyyy-MM-dd HH:mm")] DateTimeOffset Start,
        [SwitchForeignKey("Start", "2024-01-02 00:00", "Item", "Id")]
        [SwitchForeignKey("Start", "2024-07-01 00:00", "Character", "Id")]
        int TargetId);

    [StaticDataRecord("Item", "Sheet1")]
    private partial record ItemRecord(int Id, string Name);

    [StaticDataRecord("Character", "Sheet1")]
    private partial record CharacterRecord(int Id, string Name);

    private sealed partial class EventTable : StaticDataTable<EventRecord>;

    private sealed partial class SaleTable : StaticDataTable<SaleRecord>;

    private sealed partial class ItemTable : StaticDataTable<ItemRecord>;

    private sealed partial class CharacterTable : StaticDataTable<CharacterRecord>;

    private sealed partial class StaticData(ILogger logger)
        : StaticDataManager<StaticData.TableSet>(logger)
    {
        public sealed partial record TableSet(
            EventTable? Event,
            SaleTable? Sale,
            ItemTable? Item,
            CharacterTable? Character);
    }

    [Fact]
    public async Task Load_TimeZoneDependentConditions_MatchCellsParsedOnSameMachine()
    {
        var logger = CreateLogger();
        using var dir = new CsvTestDirectory();
        dir.Write("Event.Sheet1.csv", EventCsv);
        dir.Write("Sale.Sheet1.csv", SaleCsv);
        dir.Write("Item.Sheet1.csv", ItemCsv);
        dir.Write("Character.Sheet1.csv", CharacterCsv);

        var staticData = new StaticData(logger);
        await staticData.LoadAsync(dir.Path);

        Assert.Equal(3, staticData.Current.Event!.Records.Length);
        Assert.Equal(2, staticData.Current.Sale!.Records.Length);
        Assert.Empty(logger.Logs);
    }

    private TestOutputLogger<SwitchForeignKeyTimeConditionTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<SwitchForeignKeyTimeConditionTests>() is not TestOutputLogger<SwitchForeignKeyTimeConditionTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
