using Microsoft.Extensions.Logging;
using Sdp.Attributes;
using Sdp.Csv;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.CsvMappingBehaviorTests;

public partial class EnumMappingTests(ITestOutputHelper testOutputHelper)
{
    public enum ItemCategory
    {
        Consumable,
        Weapon,
        Armor,
    }

    [StaticDataRecord("Enum", "Category")]
    public sealed partial record CategoryRecord(int Id, ItemCategory Category);

    [Fact]
    public void MapNonKeyEnum_DefinedMember_Succeeds()
    {
        var logger = CreateLogger();

        var csv = "Id,Category\n1,Weapon";

        var record = Assert.Single(CsvLoader.Parse(csv, CategoryRecord.MapFromCsvRow));

        Assert.Equal(1, record.Id);
        Assert.Equal(ItemCategory.Weapon, record.Category);
        logger.LogInformation("Category mapped to {Category}", record.Category);
    }

    [Fact]
    public void MapNonKeyEnum_DefinedNumericValue_Maps()
    {
        var logger = CreateLogger();

        var csv = "Id,Category\n1,1";

        var record = Assert.Single(CsvLoader.Parse(csv, CategoryRecord.MapFromCsvRow));

        Assert.Equal(ItemCategory.Weapon, record.Category);
        logger.LogInformation("Numeric cell mapped to {Category}", record.Category);
    }

    [Fact]
    public void MapNonKeyEnum_UndefinedNumericValue_Throws()
    {
        var logger = CreateLogger();

        var csv = "Id,Category\n1,99";

        var ex = Assert.Throws<InvalidOperationException>(() => CsvLoader.Parse(csv, CategoryRecord.MapFromCsvRow));

        Assert.IsType<ArgumentException>(ex.InnerException);
        logger.LogInformation("Undefined enum value threw: {Message}", ex.Message);
    }

    public enum ItemId
    {
        None = 0,
    }

    [StaticDataRecord("Enum", "Item")]
    public sealed partial record ItemRecord([Key] ItemId Id, string Name);

    [Fact]
    public void MapKeyEnum_UndefinedNumericValue_ReturnsCastEnum()
    {
        var logger = CreateLogger();

        var csv = "Id,Name\n1001,Sword";

        var record = Assert.Single(CsvLoader.Parse(csv, ItemRecord.MapFromCsvRow));

        Assert.Equal((ItemId)1001, record.Id);
        Assert.Equal("Sword", record.Name);
        logger.LogInformation("Key enum mapped: Id={Id}, Name={Name}", record.Id, record.Name);
    }

    private TestOutputLogger<EnumMappingTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Information);
        if (factory.CreateLogger<EnumMappingTests>() is not TestOutputLogger<EnumMappingTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
