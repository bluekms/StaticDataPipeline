using System.Collections.Immutable;
using Sdp.Table;

namespace UnitTest.StaticDataTests;

public class StaticDataTableTests
{
    private sealed record Item(int Id, string Name);

    private sealed class ItemTable(ImmutableList<Item> records)
        : StaticDataTable<ItemTable, Item, int>(records, r => r.Id);

    private static ItemTable CreateTable()
    {
        var records = ImmutableList.Create(
            new Item(1, "Alpha"),
            new Item(2, "Beta"),
            new Item(3, "Gamma"));

        return new ItemTable(records);
    }

    [Fact]
    public void Records_ReturnsAllRecordsInInsertionOrder()
    {
        var table = CreateTable();

        Assert.Equal(3, table.Records.Count);
        Assert.Equal(1, table.Records[0].Id);
        Assert.Equal(2, table.Records[1].Id);
        Assert.Equal(3, table.Records[2].Id);
    }

    [Fact]
    public void Get_ExistingKey_ReturnsRecord()
    {
        var table = CreateTable();

        var record = table.Get(2);

        Assert.Equal(2, record.Id);
        Assert.Equal("Beta", record.Name);
    }

    [Fact]
    public void Get_MissingKey_ThrowsKeyNotFoundException()
    {
        var table = CreateTable();

        Assert.Throws<KeyNotFoundException>(() => table.Get(99));
    }

    [Fact]
    public void TryGet_ExistingKey_ReturnsTrueAndRecord()
    {
        var table = CreateTable();

        var result = table.TryGet(1, out var record);

        Assert.True(result);
        Assert.NotNull(record);
        Assert.Equal("Alpha", record.Name);
    }

    [Fact]
    public void TryGet_MissingKey_ReturnsFalse()
    {
        var table = CreateTable();

        var result = table.TryGet(99, out var record);

        Assert.False(result);
        Assert.Null(record);
    }

    [Fact]
    public void Constructor_DuplicateKey_ThrowsInvalidOperationException()
    {
        var records = ImmutableList.Create(
            new Item(1, "Alpha"),
            new Item(1, "Duplicate"));

        Assert.Throws<InvalidOperationException>(() => new ItemTable(records));
    }

    [Fact]
    public void Constructor_NonPropertyKeySelector_ThrowsArgumentException()
    {
        var records = ImmutableList<Item>.Empty;

        Assert.Throws<ArgumentException>(() => new NonPropertyKeyTable(records));
    }

    private sealed class NonPropertyKeyTable : StaticDataTable<NonPropertyKeyTable, Item, int>
    {
        public NonPropertyKeyTable(ImmutableList<Item> records)
            : base(records, r => r.Id + 1)
        {
        }
    }
}
