using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using Sdp.Attributes;
using Sdp.Manager;
using Sdp.Table;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.StaticDataTests;

public partial class SharedRecordTableTests(ITestOutputHelper testOutputHelper)
{
    private const string ItemCsv =
        """
        Id,Name
        1,Alpha
        2,Beta
        3,Gamma
        """;

    [StaticDataRecord("Item", "Main")]
    private partial record ItemRecord(int Id, string Name);

    private sealed partial class PrimaryItemTable : StaticDataTable<PrimaryItemTable, ItemRecord>
    {
        private readonly UniqueIndex<ItemRecord, int> byId;

        public PrimaryItemTable(ImmutableArray<ItemRecord> records)
            : base(records)
        {
            byId = new(records, x => x.Id);
        }

        public ItemRecord Get(int id) => byId.Get(id);
    }

    private sealed partial class SecondaryItemTable : StaticDataTable<SecondaryItemTable, ItemRecord>
    {
        private readonly UniqueIndex<ItemRecord, string> byName;

        public SecondaryItemTable(ImmutableArray<ItemRecord> records)
            : base(records)
        {
            byName = new(records, x => x.Name);
        }

        public ItemRecord Get(string name) => byName.Get(name);
    }

    private sealed partial class StaticData(ILogger logger)
        : StaticDataManager<StaticData.TableSet>(logger)
    {
        public sealed partial record TableSet(
            PrimaryItemTable Primary,
            SecondaryItemTable Secondary);

        public PrimaryItemTable PrimaryTable => Current.Primary;
        public SecondaryItemTable SecondaryTable => Current.Secondary;
    }

    [Fact]
    public async Task SameRecordType_DifferentTables_CoexistInTableSet()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<SharedRecordTableTests>() is not TestOutputLogger<SharedRecordTableTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        var dir = CreateTempDir();
        try
        {
            WriteCsv(dir, "Item.Main.csv", ItemCsv);

            var staticData = new StaticData(logger);
            await staticData.LoadAsync(dir);

            Assert.Equal("Beta", staticData.PrimaryTable.Get(2).Name);
            Assert.Equal(3, staticData.SecondaryTable.Get("Gamma").Id);
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
