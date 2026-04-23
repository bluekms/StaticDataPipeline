using System.Collections.Frozen;
using Sdp.Attributes;
using Sdp.Csv;

namespace UnitTest.CsvRecordMapperTests;

public class EnumKeyMapperTests
{
    public enum ItemId
    {
        None = 0,
    }

    public enum ItemCategory
    {
        Consumable,
        Weapon,
        Armor,
    }

    public sealed record ItemRecord([Key] ItemId Id, string Name);

    [Fact]
    public void MapKeyEnum_UndefinedIntValue_ReturnsCastEnum()
    {
        var headers = new[] { "Id", "Name" };
        var values = new[] { "1001", "Sword" };

        var result = CsvRecordMapper.MapToRecord<ItemRecord>(headers, values);

        Assert.Equal((ItemId)1001, result.Id);
        Assert.Equal("Sword", result.Name);
    }

    public sealed record CategoryRecord(int Id, ItemCategory Category);

    [Fact]
    public void MapNonKeyEnum_UndefinedIntValue_Throws()
    {
        var headers = new[] { "Id", "Category" };
        var values = new[] { "1", "99" };

        Assert.Throws<ArgumentException>(() =>
            CsvRecordMapper.MapToRecord<CategoryRecord>(headers, values));
    }

    [Fact]
    public void MapNonKeyEnum_DefinedMember_Succeeds()
    {
        var headers = new[] { "Id", "Category" };
        var values = new[] { "1", "Weapon" };

        var result = CsvRecordMapper.MapToRecord<CategoryRecord>(headers, values);

        Assert.Equal(1, result.Id);
        Assert.Equal(ItemCategory.Weapon, result.Category);
    }

    public sealed record DictValue([Key] ItemId Id, string Name);

    public sealed record DictOwner(
        [Length(2)] FrozenDictionary<ItemId, DictValue> Items);

    [Fact]
    public void MapDictionary_EnumKeyWithUndefinedValues_Succeeds()
    {
        var headers = new[]
        {
            "Items[0].Id", "Items[0].Name",
            "Items[1].Id", "Items[1].Name",
        };
        var values = new[] { "42", "Potion", "1001", "Sword" };

        var result = CsvRecordMapper.MapToRecord<DictOwner>(headers, values);

        Assert.Equal(2, result.Items.Count);
        Assert.True(result.Items.ContainsKey((ItemId)42));
        Assert.True(result.Items.ContainsKey((ItemId)1001));
        Assert.Equal("Potion", result.Items[(ItemId)42].Name);
        Assert.Equal("Sword", result.Items[(ItemId)1001].Name);
    }
}
