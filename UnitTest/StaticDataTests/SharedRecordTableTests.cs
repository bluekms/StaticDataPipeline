using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using Sdp.Attributes;
using Sdp.Manager;
using Sdp.Table;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.StaticDataTests;

public class SharedRecordTableTests(ITestOutputHelper testOutputHelper)
{
    private const string ItemCsv =
        """
        Id,Name
        1,Alpha
        2,Beta
        3,Gamma
        """;

    [StaticDataRecord("Item", "Main")]
    private record Item(int Id, string Name);

    private sealed class PrimaryItemTable : StaticDataTable<PrimaryItemTable, Item>
    {
        private readonly UniqueIndex<Item, int> byId;

        public PrimaryItemTable(ImmutableList<Item> records)
            : base(records)
        {
            byId = new(records, x => x.Id);
        }

        public Item Get(int id) => byId.Get(id);
    }

    private sealed class SecondaryItemTable : StaticDataTable<SecondaryItemTable, Item>
    {
        private readonly UniqueIndex<Item, string> byName;

        public SecondaryItemTable(ImmutableList<Item> records)
            : base(records)
        {
            byName = new(records, x => x.Name);
        }

        public Item Get(string name) => byName.Get(name);
    }

    private sealed class StaticData(ILogger logger)
        : StaticDataManager<StaticData.TableSet>(logger)
    {
        public sealed record TableSet(
            PrimaryItemTable Primary,
            SecondaryItemTable Secondary);

        public PrimaryItemTable PrimaryTable => Current.Primary;
        public SecondaryItemTable SecondaryTable => Current.Secondary;
    }

    [Fact]
    public async Task SameRecordType_DifferentTables_CoexistInTableSet()
    {
        var dir = CreateTempDir();
        try
        {
            WriteCsv(dir, "Item.Main.csv", ItemCsv);

            var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
            if (factory.CreateLogger<SharedRecordTableTests>() is not TestOutputLogger<SharedRecordTableTests> logger)
            {
                throw new InvalidOperationException("Logger creation failed.");
            }

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
