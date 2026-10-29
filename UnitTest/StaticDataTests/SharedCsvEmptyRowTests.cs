using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using Sdp.Attributes;
using Sdp.Manager;
using Sdp.Table;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.StaticDataTests;

public partial class SharedCsvEmptyRowTests(ITestOutputHelper testOutputHelper)
{
    private const string UnitCsv =
        """
        Id,Hp,Attack,ProfileId,Name,Description
        1,100,10,1,Goblin,Small green creature
        2,1000,100,2,Dragon,Ancient terror
        ,,,3,Slime,Jelly
        """;

    [StaticDataRecord("Unit", "Shared")]
    private partial record StatRecord(int Id, int Hp, int Attack);

    [StaticDataRecord("Unit", "Shared")]
    private partial record ProfileRecord(int ProfileId, string Name, string Description);

    private sealed partial class StatTable(ImmutableArray<StatRecord> records)
        : StaticDataTable<StatRecord>(records);

    private sealed partial class ProfileTable(ImmutableArray<ProfileRecord> records)
        : StaticDataTable<ProfileRecord>(records);

    private sealed partial class StaticData(ILogger logger)
        : StaticDataManager<StaticData.TableSet>(logger)
    {
        public sealed partial record TableSet(StatTable Stats, ProfileTable Profiles);
    }

    [Fact]
    public async Task RowEmptyForOneRecord_IsSkippedForThatRecordOnly()
    {
        var logger = CreateLogger();

        using var dir = new CsvTestDirectory();
        dir.Write("Unit.Shared.csv", UnitCsv);

        var staticData = new StaticData(logger);
        await staticData.LoadAsync(dir.Path);

        Assert.Equal([1, 2], staticData.Current.Stats.Records.Select(x => x.Id));
        Assert.Equal(["Goblin", "Dragon", "Slime"], staticData.Current.Profiles.Records.Select(x => x.Name));
        Assert.Empty(logger.Logs);
    }

    private TestOutputLogger<SharedCsvEmptyRowTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<SharedCsvEmptyRowTests>() is not TestOutputLogger<SharedCsvEmptyRowTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
