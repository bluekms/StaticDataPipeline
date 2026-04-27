using System.Collections.Immutable;
using Sdp.Attributes;
using Sdp.Manager;
using Sdp.Table;

namespace UnitTest.StaticDataTests;

public class SharedCsvTableTests
{
    private const string UnitCsv =
        """
        Id,Name,Hp,Attack,Description
        1,Goblin,100,10,Small green creature
        2,Dragon,1000,100,Ancient terror
        """;

    [StaticDataRecord("Unit", "Main")]
    private record UnitStat(int Id, int Hp, int Attack);

    [StaticDataRecord("Unit", "Main")]
    private record UnitProfile(int Id, string Name, string Description);

    private sealed class UnitStatTable(ImmutableList<UnitStat> records)
        : StaticDataTable<UnitStatTable, UnitStat>(records);

    private sealed class UnitProfileTable(ImmutableList<UnitProfile> records)
        : StaticDataTable<UnitProfileTable, UnitProfile>(records);

    private sealed class StaticData : StaticDataManager<StaticData.TableSet>
    {
        public sealed record TableSet(
            UnitStatTable Stats,
            UnitProfileTable Profiles);

        public UnitStatTable StatTable => Current.Stats;
        public UnitProfileTable ProfileTable => Current.Profiles;
    }

    [Fact]
    public async Task DifferentRecords_SameCsv_PickDifferentColumns()
    {
        var dir = CreateTempDir();
        try
        {
            WriteCsv(dir, "Unit.Main.csv", UnitCsv);

            var staticData = new StaticData();
            await staticData.LoadAsync(dir);

            Assert.Equal(2, staticData.StatTable.Records.Count);
            Assert.Equal(100, staticData.StatTable.Records[0].Hp);
            Assert.Equal(10, staticData.StatTable.Records[0].Attack);

            Assert.Equal(2, staticData.ProfileTable.Records.Count);
            Assert.Equal("Goblin", staticData.ProfileTable.Records[0].Name);
            Assert.Equal("Small green creature", staticData.ProfileTable.Records[0].Description);
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
