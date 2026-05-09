using System.Collections.Immutable;
using Sdp.Table;

namespace UnitTest.StaticDataTests;

public class StaticDataTableTests
{
    private sealed record ItemRecord(int Id, string Name);

    private sealed class ItemTable(ImmutableList<ItemRecord> records)
        : StaticDataTable<ItemTable, ItemRecord>(records);

    private sealed class IndexedItemTable : StaticDataTable<IndexedItemTable, ItemRecord>
    {
        private readonly UniqueIndex<ItemRecord, int> byId;

        public IndexedItemTable(ImmutableList<ItemRecord> records)
            : base(records)
        {
            byId = new(records, x => x.Id);
        }

        public ItemRecord Get(int id) => byId.Get(id);
    }

    private static ImmutableList<ItemRecord> SampleRecords()
        => ImmutableList.Create(
            new ItemRecord(1, "Alpha"),
            new ItemRecord(2, "Beta"),
            new ItemRecord(3, "Gamma"));

    [Fact]
    public void Records_ReturnsAllRecordsInInsertionOrder()
    {
        var table = new ItemTable(SampleRecords());

        Assert.Equal(3, table.Records.Count);
        Assert.Equal(1, table.Records[0].Id);
        Assert.Equal(2, table.Records[1].Id);
        Assert.Equal(3, table.Records[2].Id);
    }

    [Fact]
    public void BaseTable_DoesNotEnforcePrimaryKeyIndex()
    {
        var duplicated = ImmutableList.Create(
            new ItemRecord(1, "Alpha"),
            new ItemRecord(1, "Duplicate"));

        var table = new ItemTable(duplicated);

        Assert.Equal(2, table.Records.Count);
    }

    [Fact]
    public void Subclass_WithUniqueIndex_ProvidesLookup()
    {
        var table = new IndexedItemTable(SampleRecords());

        var record = table.Get(2);

        Assert.Equal("Beta", record.Name);
    }

    [Fact]
    public void Subclass_WithUniqueIndex_DuplicateKey_ThrowsInvalidOperationException()
    {
        var duplicated = ImmutableList.Create(
            new ItemRecord(1, "Alpha"),
            new ItemRecord(1, "Duplicate"));

        Assert.Throws<InvalidOperationException>(() => new IndexedItemTable(duplicated));
    }
}
