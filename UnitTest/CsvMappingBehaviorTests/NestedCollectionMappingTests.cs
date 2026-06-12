using System.Collections.Frozen;
using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using Sdp.Attributes;
using Sdp.Csv;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.CsvMappingBehaviorTests;

public partial class NestedCollectionMappingTests(ITestOutputHelper testOutputHelper)
{
    public sealed record Stats(int Id, [Length(2)] ImmutableArray<int> Values);

    [StaticDataRecord("NestedCollection", "Inner")]
    public sealed partial record HolderRecord(int Id, Stats Data);

    [Fact]
    public void NestedRecord_WithFixedLengthCollection_Maps()
    {
        var logger = CreateLogger();

        var csv = "Id,Data.Id,Data.Values[0],Data.Values[1]\n1,7,10,20";

        var record = Assert.Single(CsvLoader.Parse(csv, HolderRecord.MapFromCsvRow));

        Assert.Equal(1, record.Id);
        Assert.Equal(7, record.Data.Id);
        Assert.Equal(2, record.Data.Values.Length);
        Assert.Equal(10, record.Data.Values[0]);
        Assert.Equal(20, record.Data.Values[1]);
        logger.LogInformation(
            "Nested collection mapped: Data.Id={DataId}, Values=[{V0}, {V1}]",
            record.Data.Id,
            record.Data.Values[0],
            record.Data.Values[1]);
    }

    public sealed record Item(int Id, string Name);

    [StaticDataRecord("NestedCollection", "RecordSet")]
    public sealed partial record SetHolderRecord([Length(2)] FrozenSet<Item> Items);

    [Fact]
    public void FrozenSet_OfRecords_Maps()
    {
        var logger = CreateLogger();

        var csv = "Items[0].Id,Items[0].Name,Items[1].Id,Items[1].Name\n1,First,2,Second";

        var record = Assert.Single(CsvLoader.Parse(csv, SetHolderRecord.MapFromCsvRow));

        Assert.Equal(2, record.Items.Count);
        Assert.Contains(new Item(1, "First"), (IEnumerable<Item>)record.Items);
        Assert.Contains(new Item(2, "Second"), (IEnumerable<Item>)record.Items);
        logger.LogInformation("Record set mapped with {Count} elements", record.Items.Count);
    }

    private TestOutputLogger<NestedCollectionMappingTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Information);
        if (factory.CreateLogger<NestedCollectionMappingTests>()
            is not TestOutputLogger<NestedCollectionMappingTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
