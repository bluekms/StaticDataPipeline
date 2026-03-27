using System.Collections.Frozen;
using Sdp.Attributes;
using Sdp.Csv;

namespace UnitTest.CsvRecordMapperTests;

public class FrozenDictionaryMapperTests
{
    public sealed record SimpleValue([Key] int Id, string Name);

    public sealed record SimpleInventoryRecord(
        [Length(2)] FrozenDictionary<int, SimpleValue> Inventory);

    [Fact]
    public void MapFrozenDictionaryWithPrimitiveKey()
    {
        var headers = new[]
        {
            "Inventory[0].Id", "Inventory[0].Name",
            "Inventory[1].Id", "Inventory[1].Name",
        };
        var values = new[] { "1", "First", "2", "Second" };

        var result = CsvRecordMapper.MapToRecord<SimpleInventoryRecord>(headers, values);

        Assert.Equal(2, result.Inventory.Count);
        Assert.True(result.Inventory.ContainsKey(1));
        Assert.True(result.Inventory.ContainsKey(2));
        Assert.Equal("First", result.Inventory[1].Name);
        Assert.Equal("Second", result.Inventory[2].Name);
    }

    public sealed record KeyRecord(int Id, string Name);

    public sealed record ValueRecord([Key] KeyRecord Id, float Grade);

    public sealed record RecordKeyInventoryRecord(
        [Length(2)] FrozenDictionary<KeyRecord, ValueRecord> Inventory);

    [Fact]
    public void MapFrozenDictionaryWithRecordKey()
    {
        var headers = new[]
        {
            "Inventory[0].Id.Id", "Inventory[0].Id.Name", "Inventory[0].Grade",
            "Inventory[1].Id.Id", "Inventory[1].Id.Name", "Inventory[1].Grade",
        };
        var values = new[] { "1", "Alice", "4.0", "2", "Bob", "4.3" };

        var result = CsvRecordMapper.MapToRecord<RecordKeyInventoryRecord>(headers, values);

        Assert.Equal(2, result.Inventory.Count);

        var key1 = new KeyRecord(1, "Alice");
        var key2 = new KeyRecord(2, "Bob");

        Assert.True(result.Inventory.ContainsKey(key1));
        Assert.True(result.Inventory.ContainsKey(key2));
        Assert.Equal(4.0f, result.Inventory[key1].Grade);
        Assert.Equal(4.3f, result.Inventory[key2].Grade);
    }

    public sealed record ComplexValueRecord(
        [Key] KeyRecord Id,
        [Length(3)] System.Collections.Immutable.ImmutableArray<float> Grades);

    public sealed record ComplexInventoryRecord(
        [Length(2)] FrozenDictionary<KeyRecord, ComplexValueRecord> Inventory);

    [Fact]
    public void MapFrozenDictionaryWithNestedCollection()
    {
        var headers = new[]
        {
            "Inventory[0].Id.Id", "Inventory[0].Id.Name",
            "Inventory[0].Grades[0]", "Inventory[0].Grades[1]", "Inventory[0].Grades[2]",
            "Inventory[1].Id.Id", "Inventory[1].Id.Name",
            "Inventory[1].Grades[0]", "Inventory[1].Grades[1]", "Inventory[1].Grades[2]",
        };
        var values = new[]
        {
            "1", "Alice", "4.0", "4.1", "1.5",
            "2", "Bob", "4.3", "2.5", "4.5",
        };

        var result = CsvRecordMapper.MapToRecord<ComplexInventoryRecord>(headers, values);

        Assert.Equal(2, result.Inventory.Count);

        var key1 = new KeyRecord(1, "Alice");
        var key2 = new KeyRecord(2, "Bob");

        Assert.True(result.Inventory.ContainsKey(key1));
        Assert.True(result.Inventory.ContainsKey(key2));

        var value1 = result.Inventory[key1];
        Assert.Equal(3, value1.Grades.Length);
        Assert.Equal(4.0f, value1.Grades[0]);
        Assert.Equal(4.1f, value1.Grades[1]);
        Assert.Equal(1.5f, value1.Grades[2]);

        var value2 = result.Inventory[key2];
        Assert.Equal(3, value2.Grades.Length);
        Assert.Equal(4.3f, value2.Grades[0]);
        Assert.Equal(2.5f, value2.Grades[1]);
        Assert.Equal(4.5f, value2.Grades[2]);
    }

    public sealed record StringKeyValue([Key] string Code, int Amount);

    public sealed record StringKeyItemsRecord(
        [Length(2)] FrozenDictionary<string, StringKeyValue> Items);

    [Fact]
    public void MapFrozenDictionaryWithStringKey()
    {
        var headers = new[]
        {
            "Items[0].Code", "Items[0].Amount",
            "Items[1].Code", "Items[1].Amount",
        };
        var values = new[] { "A001", "100", "B002", "200" };

        var result = CsvRecordMapper.MapToRecord<StringKeyItemsRecord>(headers, values);

        Assert.Equal(2, result.Items.Count);
        Assert.True(result.Items.ContainsKey("A001"));
        Assert.True(result.Items.ContainsKey("B002"));
        Assert.Equal(100, result.Items["A001"].Amount);
        Assert.Equal(200, result.Items["B002"].Amount);
    }

    public sealed record MixedItemsRecord(
        int Id,
        string Name,
        [Length(2)] FrozenDictionary<int, SimpleValue> Items);

    [Fact]
    public void MapMixedRecordWithDictionary()
    {
        var headers = new[]
        {
            "Id", "Name",
            "Items[0].Id", "Items[0].Name",
            "Items[1].Id", "Items[1].Name",
        };
        var values = new[] { "42", "Test", "1", "ItemA", "2", "ItemB" };

        var result = CsvRecordMapper.MapToRecord<MixedItemsRecord>(headers, values);

        Assert.Equal(42, result.Id);
        Assert.Equal("Test", result.Name);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal("ItemA", result.Items[1].Name);
        Assert.Equal("ItemB", result.Items[2].Name);
    }
}
