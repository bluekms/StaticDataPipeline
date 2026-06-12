using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using Sdp.Attributes;
using Sdp.Csv;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.CsvMappingBehaviorTests;

public partial class ImmutableArrayMappingTests(ITestOutputHelper testOutputHelper)
{
    [StaticDataRecord("Array", "Int")]
    public sealed partial record RecordWithIntArrayRecord([Length(3)] ImmutableArray<int> Scores);

    [Fact]
    public void MapImmutableArrayOfPrimitives()
    {
        var logger = CreateLogger();

        var csv = "Scores[0],Scores[1],Scores[2]\n10,20,30";

        var record = Assert.Single(CsvLoader.Parse(csv, RecordWithIntArrayRecord.MapFromCsvRow));

        Assert.Equal(3, record.Scores.Length);
        Assert.Equal(10, record.Scores[0]);
        Assert.Equal(20, record.Scores[1]);
        Assert.Equal(30, record.Scores[2]);
        logger.LogInformation(
            "Scores mapped: [{S0}, {S1}, {S2}]",
            record.Scores[0],
            record.Scores[1],
            record.Scores[2]);
    }

    [StaticDataRecord("Array", "String")]
    public sealed partial record RecordWithStringArrayRecord([Length(2)] ImmutableArray<string> Names);

    [Fact]
    public void MapImmutableArrayOfStrings()
    {
        var logger = CreateLogger();

        var csv = "Names[0],Names[1]\nAlice,Bob";

        var record = Assert.Single(CsvLoader.Parse(csv, RecordWithStringArrayRecord.MapFromCsvRow));

        Assert.Equal(2, record.Names.Length);
        Assert.Equal("Alice", record.Names[0]);
        Assert.Equal("Bob", record.Names[1]);
        logger.LogInformation("Names mapped: [{N0}, {N1}]", record.Names[0], record.Names[1]);
    }

    [StaticDataRecord("Array", "ColumnName")]
    public sealed partial record RecordWithColumnNameArrayRecord(
        [ColumnName("Score")][Length(3)] ImmutableArray<int> Scores);

    [Fact]
    public void MapImmutableArrayWithColumnName()
    {
        var logger = CreateLogger();

        var csv = "Score[0],Score[1],Score[2]\n100,200,300";

        var record = Assert.Single(CsvLoader.Parse(csv, RecordWithColumnNameArrayRecord.MapFromCsvRow));

        Assert.Equal(3, record.Scores.Length);
        Assert.Equal(100, record.Scores[0]);
        Assert.Equal(200, record.Scores[1]);
        Assert.Equal(300, record.Scores[2]);
        logger.LogInformation("Column-renamed scores mapped with {Count} elements", record.Scores.Length);
    }

    [StaticDataRecord("Array", "Mixed")]
    public sealed partial record MixedRecord(
        int Id,
        [Length(2)] ImmutableArray<int> Values,
        string Name);

    [Fact]
    public void MapRecordWithMixedFields()
    {
        var logger = CreateLogger();

        var csv = "Id,Values[0],Values[1],Name\n42,10,20,Test";

        var record = Assert.Single(CsvLoader.Parse(csv, MixedRecord.MapFromCsvRow));

        Assert.Equal(42, record.Id);
        Assert.Equal(2, record.Values.Length);
        Assert.Equal(10, record.Values[0]);
        Assert.Equal(20, record.Values[1]);
        Assert.Equal("Test", record.Name);
        logger.LogInformation(
            "Mixed record mapped: Id={Id}, Values={Count}, Name={Name}",
            record.Id,
            record.Values.Length,
            record.Name);
    }

    [StaticDataRecord("Array", "NullableInt")]
    public sealed partial record RecordWithNullableIntArrayRecord(
        [Length(3)][NullString("-")] ImmutableArray<int?> Scores);

    [Fact]
    public void MapImmutableArrayOfNullablePrimitives()
    {
        var logger = CreateLogger();

        var csv = "Scores[0],Scores[1],Scores[2]\n10,-,30";

        var record = Assert.Single(CsvLoader.Parse(csv, RecordWithNullableIntArrayRecord.MapFromCsvRow));

        Assert.Equal(3, record.Scores.Length);
        Assert.Equal(10, record.Scores[0]);
        Assert.Null(record.Scores[1]);
        Assert.Equal(30, record.Scores[2]);
        logger.LogInformation(
            "Nullable scores mapped: [{S0}, {S1}, {S2}]",
            record.Scores[0],
            record.Scores[1],
            record.Scores[2]);
    }

    public sealed record InnerRecord(int Id, string Name);

    [StaticDataRecord("Array", "Records")]
    public sealed partial record RecordWithRecordArrayRecord([Length(2)] ImmutableArray<InnerRecord> Items);

    [Fact]
    public void MapImmutableArrayOfRecords()
    {
        var logger = CreateLogger();

        var csv = "Items[0].Id,Items[0].Name,Items[1].Id,Items[1].Name\n1,First,2,Second";

        var record = Assert.Single(CsvLoader.Parse(csv, RecordWithRecordArrayRecord.MapFromCsvRow));

        Assert.Equal(2, record.Items.Length);
        Assert.Equal(1, record.Items[0].Id);
        Assert.Equal("First", record.Items[0].Name);
        Assert.Equal(2, record.Items[1].Id);
        Assert.Equal("Second", record.Items[1].Name);
        logger.LogInformation("Record items mapped: [{I0}, {I1}]", record.Items[0], record.Items[1]);
    }

    private TestOutputLogger<ImmutableArrayMappingTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Information);
        if (factory.CreateLogger<ImmutableArrayMappingTests>()
            is not TestOutputLogger<ImmutableArrayMappingTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
