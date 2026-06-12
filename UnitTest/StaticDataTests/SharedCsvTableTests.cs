using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using Sdp.Attributes;
using Sdp.Manager;
using Sdp.Table;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.StaticDataTests;

public partial class SharedCsvTableTests(ITestOutputHelper testOutputHelper)
{
    private const string UnitCsv =
        """
        Id,Name,Hp,Attack,Description
        1,Goblin,100,10,Small green creature
        2,Dragon,1000,100,Ancient terror
        """;

    [StaticDataRecord("Unit", "Main")]
    private partial record UnitStatRecord(int Id, int Hp, int Attack);

    [StaticDataRecord("Unit", "Main")]
    private partial record UnitProfileRecord(int Id, string Name, string Description);

    private sealed partial class UnitStatTable(ImmutableArray<UnitStatRecord> records)
        : StaticDataTable<UnitStatTable, UnitStatRecord>(records);

    private sealed partial class UnitProfileTable(ImmutableArray<UnitProfileRecord> records)
        : StaticDataTable<UnitProfileTable, UnitProfileRecord>(records);

    private sealed partial class StaticData(ILogger logger)
        : StaticDataManager<StaticData.TableSet>(logger)
    {
        public sealed partial record TableSet(
            UnitStatTable Stats,
            UnitProfileTable Profiles);

        public UnitStatTable StatTable => Current.Stats;
        public UnitProfileTable ProfileTable => Current.Profiles;
    }

    [Fact]
    public async Task DifferentRecords_SameCsv_PickDifferentColumns()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<SharedCsvTableTests>() is not TestOutputLogger<SharedCsvTableTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        var dir = CreateTempDir();
        try
        {
            WriteCsv(dir, "Unit.Main.csv", UnitCsv);

            var staticData = new StaticData(logger);
            await staticData.LoadAsync(dir);

            Assert.Equal(2, staticData.StatTable.Records.Length);
            Assert.Equal(100, staticData.StatTable.Records[0].Hp);
            Assert.Equal(10, staticData.StatTable.Records[0].Attack);

            Assert.Equal(2, staticData.ProfileTable.Records.Length);
            Assert.Equal("Goblin", staticData.ProfileTable.Records[0].Name);
            Assert.Equal("Small green creature", staticData.ProfileTable.Records[0].Description);
            Assert.Empty(logger.Logs);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    private static string CreateTempDir()
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(path);
        return path;
    }

    private static void WriteCsv(string dir, string fileName, string content)
        => File.WriteAllText(Path.Combine(dir, fileName), content);
}
