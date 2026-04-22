using Sdp.Attributes;
using Sdp.Csv;

namespace UnitTest.DocSampleTests;

// Docs/ko/03-usage/01-first-record.md 의 Item 예제를 검증한다.
public class FirstRecordSampleTests
{
    private enum ItemCategory
    {
        Consumable,
        Weapon,
        Armor,
    }

    [StaticDataRecord("Items", "Items")]
    private sealed record ItemRecord(
        [Key] int Id,
        string Name,
        int Price,
        ItemCategory Category);

    private const string ItemsCsv =
        """
        Id,Name,Price,Category
        1,Potion,100,Consumable
        2,Sword,5000,Weapon
        3,Shield,4000,Armor
        """;

    private static readonly int[] ExpectedIds = [1, 2, 3];

    [Fact]
    public void Parse_ConceptualCsv_ReturnsThreeItems()
    {
        var records = CsvLoader.Parse<ItemRecord>(ItemsCsv);

        Assert.Equal(3, records.Count);

        Assert.Equal(1, records[0].Id);
        Assert.Equal("Potion", records[0].Name);
        Assert.Equal(100, records[0].Price);
        Assert.Equal(ItemCategory.Consumable, records[0].Category);

        Assert.Equal(ItemCategory.Weapon, records[1].Category);
        Assert.Equal(ItemCategory.Armor, records[2].Category);
    }

    [Fact]
    public void Parse_PreservesRowOrder()
    {
        var records = CsvLoader.Parse<ItemRecord>(ItemsCsv);

        Assert.Equal(ExpectedIds, records.Select(x => x.Id).ToArray());
    }
}
