using System.Collections.Frozen;
using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using SchemaInfoScanner;
using Sdp.Attributes;
using Sdp.Manager;
using Sdp.Table;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.ForeignKeyTests;

public partial class ForeignKeyBrandingCollectionElementTests(ITestOutputHelper testOutputHelper)
{
    private TestOutputLogger<ForeignKeyBrandingCollectionElementTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<ForeignKeyBrandingCollectionElementTests>()
            is not TestOutputLogger<ForeignKeyBrandingCollectionElementTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }

    [StaticDataRecord("Item", "Sheet1")]
    private partial record ItemRecord([Key] ItemRecord.ItemId Id, string Name)
    {
        public readonly record struct ItemId(int Value);
    }

    [StaticDataRecord("Gacha", "Sheet1")]
    private partial record GachaRecord(
        int Id,
        [Length(2)] [ForeignKey("Item", "Id")] FrozenSet<ItemRecord.ItemId> PickupItemIds);

    private sealed partial class ItemTable(ImmutableArray<ItemRecord> records)
        : StaticDataTable<ItemRecord>(records);

    private sealed partial class GachaTable(ImmutableArray<GachaRecord> records)
        : StaticDataTable<GachaRecord>(records);

    private sealed partial class BrandingCollectionElementStaticData(ILogger logger)
        : StaticDataManager<BrandingCollectionElementStaticData.TableSet>(logger)
    {
        public sealed partial record TableSet(ItemTable? Item, GachaTable? Gacha);

        public GachaTable GachaTable => Current.Gacha!;
    }

    private const string ItemCsv =
        """
        Id,Name
        10001,Sword
        10002,Shield
        """;

    private const string ValidGachaCsv =
        """
        Id,PickupItemIds[0],PickupItemIds[1]
        1,10001,10002
        """;

    private const string MissingGachaCsv =
        """
        Id,PickupItemIds[0],PickupItemIds[1]
        1,10001,19999
        """;

    // language=C#
    private const string GachaRecordSource = """
        public sealed partial record ItemRecord([Key] ItemRecord.ItemId Id, string Name)
        {
            public readonly record struct ItemId(int Value);
        }

        [StaticDataRecord("Gacha", "Sheet1")]
        public sealed partial record GachaRecord(
            int Id,
            [Length(2)] [ForeignKey("Item", "Id")] FrozenSet<ItemRecord.ItemId> PickupItemIds);
        """;

    private static CsvTestDirectory CreateDirectory(string gachaCsv)
    {
        var dir = new CsvTestDirectory();
        dir.Write("Item.Sheet1.csv", ItemCsv);
        dir.Write("Gacha.Sheet1.csv", gachaCsv);

        return dir;
    }

    [Fact]
    public async Task Load_BrandingCollectionElementToBrandingKey_Succeeds()
    {
        var logger = CreateLogger();

        using var dir = CreateDirectory(ValidGachaCsv);

        var staticData = new BrandingCollectionElementStaticData(logger);
        await staticData.LoadAsync(dir.Path);

        var gacha = Assert.Single(staticData.GachaTable.Records);
        var pickupItemIds = gacha.PickupItemIds
            .Select(id => id.Value)
            .Order()
            .ToList();
        Assert.Equal([10001, 10002], pickupItemIds);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public async Task Load_BrandingCollectionElementMissingFromTarget_ThrowsAggregateException()
    {
        var logger = CreateLogger();

        using var dir = CreateDirectory(MissingGachaCsv);

        var staticData = new BrandingCollectionElementStaticData(logger);

        var ex = await Assert.ThrowsAsync<AggregateException>(() => staticData.LoadAsync(dir.Path));

        var inner = Assert.Single(ex.InnerExceptions);
        Assert.Contains("PickupItemIds", inner.Message, StringComparison.Ordinal);
        Assert.Contains("19999", inner.Message, StringComparison.Ordinal);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public async Task Load_CsvHeadersFromRecordFlattener_Succeeds()
    {
        var logger = CreateLogger();
        var parseResult = SimpleCordParser.Parse(GachaRecordSource, logger);
        var gachaSchema = Assert.Single(parseResult.RawRecordSchemata);
        var headers = RecordFlattener.Flatten(gachaSchema, parseResult.RecordSchemaCatalog, logger);
        var headerLine = string.Join(",", headers);
        Assert.Equal("Id,PickupItemIds[0],PickupItemIds[1]", headerLine);

        using var dir = CreateDirectory(headerLine + "\n1,10001,10002");

        var staticData = new BrandingCollectionElementStaticData(logger);
        await staticData.LoadAsync(dir.Path);

        var gacha = Assert.Single(staticData.GachaTable.Records);
        Assert.Equal(2, gacha.PickupItemIds.Count);
        Assert.Empty(logger.Logs);
    }
}
