using System.Collections.Frozen;
using Microsoft.Extensions.Logging;
using Sdp.Attributes;
using Sdp.Csv;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.CsvMappingBehaviorTests;

public partial class FrozenSetMappingTests(ITestOutputHelper testOutputHelper)
{
    [StaticDataRecord("Set", "Int")]
    public sealed partial record RecordWithIntSetRecord([Length(3)] FrozenSet<int> Ids);

    [Fact]
    public void MapFrozenSetOfPrimitives()
    {
        var logger = CreateLogger();

        var csv = "Ids[0],Ids[1],Ids[2]\n1,2,3";

        var record = Assert.Single(CsvLoader.Parse(csv, RecordWithIntSetRecord.MapFromCsvRow));

        Assert.Equal(3, record.Ids.Count);

        var array = record.Ids.ToArray();
        Assert.Contains(1, array);
        Assert.Contains(2, array);
        Assert.Contains(3, array);
        logger.LogInformation("Int set mapped with {Count} elements", record.Ids.Count);
    }

    [StaticDataRecord("Set", "String")]
    public sealed partial record RecordWithStringSetRecord([Length(2)] FrozenSet<string> Tags);

    [Fact]
    public void MapFrozenSetOfStrings()
    {
        var logger = CreateLogger();

        var csv = "Tags[0],Tags[1]\nimportant,urgent";

        var record = Assert.Single(CsvLoader.Parse(csv, RecordWithStringSetRecord.MapFromCsvRow));

        Assert.Equal(2, record.Tags.Count);

        var array = record.Tags.ToArray();
        Assert.Contains("important", array);
        Assert.Contains("urgent", array);
        logger.LogInformation("String set mapped with {Count} elements", record.Tags.Count);
    }

    [StaticDataRecord("Set", "ColumnName")]
    public sealed partial record RecordWithColumnNameSetRecord(
        [ColumnName("Tag")][Length(3)] FrozenSet<string> Tags);

    [Fact]
    public void MapFrozenSetWithColumnName()
    {
        var logger = CreateLogger();

        var csv = "Tag[0],Tag[1],Tag[2]\na,b,c";

        var record = Assert.Single(CsvLoader.Parse(csv, RecordWithColumnNameSetRecord.MapFromCsvRow));

        Assert.Equal(3, record.Tags.Count);

        var array = record.Tags.ToArray();
        Assert.Contains("a", array);
        Assert.Contains("b", array);
        Assert.Contains("c", array);
        logger.LogInformation("Column-renamed set mapped with {Count} elements", record.Tags.Count);
    }

    [StaticDataRecord("Set", "Mixed")]
    public sealed partial record MixedWithSetRecord(
        int Id,
        [Length(2)] FrozenSet<int> Values,
        string Name);

    [Fact]
    public void MapRecordWithMixedFieldsIncludingSet()
    {
        var logger = CreateLogger();

        var csv = "Id,Values[0],Values[1],Name\n42,100,200,Test";

        var record = Assert.Single(CsvLoader.Parse(csv, MixedWithSetRecord.MapFromCsvRow));

        Assert.Equal(42, record.Id);
        Assert.Equal(2, record.Values.Count);

        var array = record.Values.ToArray();
        Assert.Contains(100, array);
        Assert.Contains(200, array);

        Assert.Equal("Test", record.Name);
        logger.LogInformation(
            "Mixed record mapped: Id={Id}, Values={Count}, Name={Name}",
            record.Id,
            record.Values.Count,
            record.Name);
    }

    private TestOutputLogger<FrozenSetMappingTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Information);
        if (factory.CreateLogger<FrozenSetMappingTests>() is not TestOutputLogger<FrozenSetMappingTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
