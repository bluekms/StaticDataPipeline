using System.Collections.Immutable;
using Sdp.Attributes;
using Sdp.Manager;
using Sdp.Table;

namespace UnitTest.StaticDataTests;

public class FilteredRecordTableTests
{
    private const string BuffCsv =
        """
        Id,Name,IsNormal
        1,Haste,true
        2,Poison,false
        3,Shield,true
        4,Curse,false
        """;

    [StaticDataRecord("Buff", "Main")]
    private record Buff(int Id, string Name, bool IsNormal);

    private sealed class NormalBuffTable(ImmutableList<Buff> records)
        : StaticDataTable<NormalBuffTable, Buff>(records.Where(x => x.IsNormal).ToImmutableList());

    private sealed class AbnormalBuffTable(ImmutableList<Buff> records)
        : StaticDataTable<AbnormalBuffTable, Buff>(records.Where(x => !x.IsNormal).ToImmutableList());

    private sealed class StaticData : StaticDataManager<StaticData.TableSet>
    {
        public sealed record TableSet(
            NormalBuffTable Normal,
            AbnormalBuffTable Abnormal);

        public NormalBuffTable NormalTable => Current.Normal;
        public AbnormalBuffTable AbnormalTable => Current.Abnormal;
    }

    [Fact]
    public async Task SameRecord_TwoTables_FilterIntoSubsets()
    {
        var dir = CreateTempDir();
        try
        {
            WriteCsv(dir, "Buff.Main.csv", BuffCsv);

            var staticData = new StaticData();
            await staticData.LoadAsync(dir);

            Assert.Equal(2, staticData.NormalTable.Records.Count);
            Assert.All(staticData.NormalTable.Records, x => Assert.True(x.IsNormal));

            Assert.Equal(2, staticData.AbnormalTable.Records.Count);
            Assert.All(staticData.AbnormalTable.Records, x => Assert.False(x.IsNormal));
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
