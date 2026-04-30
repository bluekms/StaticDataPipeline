using System.Collections.Immutable;
using Sdp.Attributes;
using Sdp.Csv;
using Sdp.Table;

namespace UnitTest.DocSampleTests;

// Docs/ko/03-usage/02-static-data-table.md 의 ItemTable + UniqueIndex 예제를 검증한다.
public class StaticDataTableSampleTests
{
    private enum ItemCategory
    {
        Consumable,
        Weapon,
        Armor,
    }

    [StaticDataRecord("Items", "Items")]
    private sealed record Item(
        [Key] int Id,
        string Name,
        int Price,
        ItemCategory Category);

    private sealed class ItemTable : StaticDataTable<ItemTable, Item>
    {
        private readonly UniqueIndex<Item, int> byId;

        public ItemTable(ImmutableList<Item> records)
            : base(records)
        {
            byId = new UniqueIndex<Item, int>(records, x => x.Id);
        }

        public Item Get(int id)
            => byId.Get(id);

        public bool TryGet(int id, out Item? record)
            => byId.TryGet(id, out record);
    }

    private const string ItemsCsv =
        """
        Id,Name,Price,Category
        1,Potion,100,Consumable
        2,Sword,5000,Weapon
        3,Shield,4000,Armor
        """;

    private static readonly int[] ExpectedIds = [1, 2, 3];

    private static ItemTable LoadTable()
        => new(CsvLoader.Parse<Item>(ItemsCsv));

    [Fact]
    public void Records_PreservesCsvOrder()
    {
        var table = LoadTable();

        Assert.Equal(ExpectedIds, table.Records.Select(x => x.Id).ToArray());
    }

    [Fact]
    public void Get_ByExistingKey_ReturnsRecord()
    {
        var table = LoadTable();

        var sword = table.Get(2);

        Assert.Equal("Sword", sword.Name);
        Assert.Equal(5000, sword.Price);
    }

    [Fact]
    public void TryGet_ByExistingKey_ReturnsTrueAndRecord()
    {
        var table = LoadTable();

        var found = table.TryGet(1, out var potion);

        Assert.True(found);
        Assert.NotNull(potion);
        Assert.Equal("Potion", potion!.Name);
    }

    [Fact]
    public void TryGet_ByMissingKey_ReturnsFalse()
    {
        var table = LoadTable();

        var found = table.TryGet(999, out var record);

        Assert.False(found);
        Assert.Null(record);
    }

    [Fact]
    public void DuplicateKey_ThrowsInvalidOperationException()
    {
        const string DuplicateCsv =
            """
            Id,Name,Price,Category
            1,Potion,100,Consumable
            1,Sword,5000,Weapon
            """;

        var records = CsvLoader.Parse<Item>(DuplicateCsv);

        Assert.Throws<InvalidOperationException>(() => new ItemTable(records));
    }
}
