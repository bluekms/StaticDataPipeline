using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using Sdp.Attributes;
using Sdp.Manager;
using Sdp.Table;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.StaticDataTests;

public partial class FilteredRecordTableTests(ITestOutputHelper testOutputHelper)
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
    private partial record BuffRecord(int Id, string Name, bool IsNormal);

    private sealed partial class NormalBuffTable(ImmutableArray<BuffRecord> records)
        : StaticDataTable<NormalBuffTable, BuffRecord>(records.Where(x => x.IsNormal).ToImmutableArray());

    private sealed partial class AbnormalBuffTable(ImmutableArray<BuffRecord> records)
        : StaticDataTable<AbnormalBuffTable, BuffRecord>(records.Where(x => !x.IsNormal).ToImmutableArray());

    private sealed partial class StaticData(ILogger logger)
        : StaticDataManager<StaticData.TableSet>(logger)
    {
        public sealed partial record TableSet(
            NormalBuffTable Normal,
            AbnormalBuffTable Abnormal);

        public NormalBuffTable NormalTable => Current.Normal;
        public AbnormalBuffTable AbnormalTable => Current.Abnormal;
    }

    [Fact]
    public async Task SameRecord_TwoTables_FilterIntoSubsets()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<FilteredRecordTableTests>() is not TestOutputLogger<FilteredRecordTableTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        var dir = CreateTempDir();
        try
        {
            WriteCsv(dir, "Buff.Main.csv", BuffCsv);

            var staticData = new StaticData(logger);
            await staticData.LoadAsync(dir);

            Assert.Equal(2, staticData.NormalTable.Records.Length);
            Assert.All(staticData.NormalTable.Records, x => Assert.True(x.IsNormal));

            Assert.Equal(2, staticData.AbnormalTable.Records.Length);
            Assert.All(staticData.AbnormalTable.Records, x => Assert.False(x.IsNormal));
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
