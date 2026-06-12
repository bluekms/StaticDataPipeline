using System.Collections.Frozen;
using Microsoft.Extensions.Logging;
using Sdp.Attributes;
using Sdp.Csv;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.CsvMappingBehaviorTests;

public partial class FrozenDictionaryMappingTests(ITestOutputHelper testOutputHelper)
{
    public sealed record SimpleValue([Key] int Id, string Name);

    [StaticDataRecord("Dict", "Inventory")]
    public sealed partial record SimpleInventoryRecord(
        [Length(2)] FrozenDictionary<int, SimpleValue> Inventory);

    [Fact]
    public void MapFrozenDictionaryWithPrimitiveKey()
    {
        var logger = CreateLogger();

        var csv = "Inventory[0].Id,Inventory[0].Name,Inventory[1].Id,Inventory[1].Name\n"
                + "1,First,2,Second";

        var record = Assert.Single(CsvLoader.Parse(csv, SimpleInventoryRecord.MapFromCsvRow));

        Assert.Equal(2, record.Inventory.Count);
        Assert.True(record.Inventory.ContainsKey(1));
        Assert.True(record.Inventory.ContainsKey(2));
        Assert.Equal("First", record.Inventory[1].Name);
        Assert.Equal("Second", record.Inventory[2].Name);
        logger.LogInformation(
            "Inventory mapped with {Count} entries, [1]={Name}",
            record.Inventory.Count,
            record.Inventory[1].Name);
    }

    [Fact]
    public void MapFrozenDictionaryWithDuplicateKey_Throws()
    {
        var logger = CreateLogger();

        var csv = "Inventory[0].Id,Inventory[0].Name,Inventory[1].Id,Inventory[1].Name\n"
                + "1,First,1,Second";

        var ex = Assert.Throws<InvalidOperationException>(() => CsvLoader.Parse(csv, SimpleInventoryRecord.MapFromCsvRow));

        Assert.IsType<ArgumentException>(ex.InnerException);
        logger.LogInformation("Duplicate dictionary key threw: {Message}", ex.Message);
    }

    public sealed record StringKeyValue([Key] string Code, int Amount);

    [StaticDataRecord("Dict", "StringKey")]
    public sealed partial record StringKeyItemsRecord(
        [Length(2)] FrozenDictionary<string, StringKeyValue> Items);

    [Fact]
    public void MapFrozenDictionaryWithStringKey()
    {
        var logger = CreateLogger();

        var csv = "Items[0].Code,Items[0].Amount,Items[1].Code,Items[1].Amount\n"
                + "A001,100,B002,200";

        var record = Assert.Single(CsvLoader.Parse(csv, StringKeyItemsRecord.MapFromCsvRow));

        Assert.Equal(2, record.Items.Count);
        Assert.True(record.Items.ContainsKey("A001"));
        Assert.True(record.Items.ContainsKey("B002"));
        Assert.Equal(100, record.Items["A001"].Amount);
        Assert.Equal(200, record.Items["B002"].Amount);
        logger.LogInformation(
            "String-key items mapped: A001={A}, B002={B}",
            record.Items["A001"].Amount,
            record.Items["B002"].Amount);
    }

    [StaticDataRecord("Dict", "Mixed")]
    public sealed partial record MixedItemsRecord(
        int Id,
        string Name,
        [Length(2)] FrozenDictionary<int, SimpleValue> Items);

    [Fact]
    public void MapMixedRecordWithDictionary()
    {
        var logger = CreateLogger();

        var csv = "Id,Name,Items[0].Id,Items[0].Name,Items[1].Id,Items[1].Name\n"
                + "42,Test,1,ItemA,2,ItemB";

        var record = Assert.Single(CsvLoader.Parse(csv, MixedItemsRecord.MapFromCsvRow));

        Assert.Equal(42, record.Id);
        Assert.Equal("Test", record.Name);
        Assert.Equal(2, record.Items.Count);
        Assert.Equal("ItemA", record.Items[1].Name);
        Assert.Equal("ItemB", record.Items[2].Name);
        logger.LogInformation(
            "Mixed record mapped: Id={Id}, Name={Name}, Items={Count}",
            record.Id,
            record.Name,
            record.Items.Count);
    }

    public enum ItemId
    {
        None = 0,
    }

    public sealed record EnumKeyValue([Key] ItemId Id, string Name);

    [StaticDataRecord("Dict", "EnumKey")]
    public sealed partial record EnumKeyInventoryRecord(
        [Length(2)] FrozenDictionary<ItemId, EnumKeyValue> Items);

    [Fact]
    public void MapFrozenDictionaryWithEnumKey()
    {
        var logger = CreateLogger();

        var csv = "Items[0].Id,Items[0].Name,Items[1].Id,Items[1].Name\n42,Potion,1001,Sword";

        var record = Assert.Single(CsvLoader.Parse(csv, EnumKeyInventoryRecord.MapFromCsvRow));

        Assert.Equal(2, record.Items.Count);
        Assert.True(record.Items.ContainsKey((ItemId)42));
        Assert.True(record.Items.ContainsKey((ItemId)1001));
        Assert.Equal("Potion", record.Items[(ItemId)42].Name);
        Assert.Equal("Sword", record.Items[(ItemId)1001].Name);
        logger.LogInformation(
            "Enum-key items mapped: 42={A}, 1001={B}",
            record.Items[(ItemId)42].Name,
            record.Items[(ItemId)1001].Name);
    }

    public sealed record ComplexValue(
        [Key] int Id,
        [Length(3)] System.Collections.Immutable.ImmutableArray<float> Grades);

    [StaticDataRecord("Dict", "Complex")]
    public sealed partial record ComplexInventoryRecord(
        [Length(2)] FrozenDictionary<int, ComplexValue> Inventory);

    [Fact]
    public void MapFrozenDictionaryWithNestedCollection()
    {
        var logger = CreateLogger();

        var csv = "Inventory[0].Id,Inventory[0].Grades[0],Inventory[0].Grades[1],Inventory[0].Grades[2],"
                + "Inventory[1].Id,Inventory[1].Grades[0],Inventory[1].Grades[1],Inventory[1].Grades[2]\n"
                + "1,4.0,4.1,1.5,2,4.3,2.5,4.5";

        var record = Assert.Single(CsvLoader.Parse(csv, ComplexInventoryRecord.MapFromCsvRow));

        Assert.Equal(2, record.Inventory.Count);
        Assert.Equal(3, record.Inventory[1].Grades.Length);
        Assert.Equal(4.0f, record.Inventory[1].Grades[0]);
        Assert.Equal(1.5f, record.Inventory[1].Grades[2]);
        Assert.Equal(4.3f, record.Inventory[2].Grades[0]);
        Assert.Equal(4.5f, record.Inventory[2].Grades[2]);
        logger.LogInformation(
            "Complex inventory mapped: {Count} entries, [1].Grades={GradeCount} elements",
            record.Inventory.Count,
            record.Inventory[1].Grades.Length);
    }

    public sealed record KeyRecord(int Id, string Name);

    public sealed record RecordKeyValue([Key] KeyRecord Id, float Grade);

    [StaticDataRecord("Dict", "RecordKey")]
    public sealed partial record RecordKeyInventoryRecord(
        [Length(2)] FrozenDictionary<KeyRecord, RecordKeyValue> Inventory);

    [Fact]
    public void MapFrozenDictionaryWithRecordKey()
    {
        var logger = CreateLogger();

        var csv = "Inventory[0].Id.Id,Inventory[0].Id.Name,Inventory[0].Grade,"
                + "Inventory[1].Id.Id,Inventory[1].Id.Name,Inventory[1].Grade\n"
                + "1,Alice,4.0,2,Bob,4.3";

        var record = Assert.Single(CsvLoader.Parse(csv, RecordKeyInventoryRecord.MapFromCsvRow));

        Assert.Equal(2, record.Inventory.Count);

        var key1 = new KeyRecord(1, "Alice");
        var key2 = new KeyRecord(2, "Bob");

        Assert.True(record.Inventory.ContainsKey(key1));
        Assert.True(record.Inventory.ContainsKey(key2));
        Assert.Equal(4.0f, record.Inventory[key1].Grade);
        Assert.Equal(4.3f, record.Inventory[key2].Grade);
        logger.LogInformation(
            "Record-key inventory mapped: {Key1}={Grade1}, {Key2}={Grade2}",
            key1,
            record.Inventory[key1].Grade,
            key2,
            record.Inventory[key2].Grade);
    }

    private TestOutputLogger<FrozenDictionaryMappingTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Information);
        if (factory.CreateLogger<FrozenDictionaryMappingTests>()
            is not TestOutputLogger<FrozenDictionaryMappingTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
