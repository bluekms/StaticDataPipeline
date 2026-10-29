using System.Collections.Frozen;
using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using Sdp.Attributes;
using Sdp.Csv;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.CsvMappingBehaviorTests;

public partial class DynamicLengthCollectionMappingTests(ITestOutputHelper testOutputHelper)
{
    [StaticDataRecord("DynamicArray", "Int")]
    public sealed partial record BareIntArrayRecord(ImmutableArray<int> Scores);

    [Fact]
    public void BareImmutableArray_ReadsAllIndexedColumns()
    {
        var logger = CreateLogger();

        var csv = "Scores[0],Scores[1],Scores[2]\n10,20,30";
        var record = Assert.Single(CsvLoader.Parse(csv, BareIntArrayRecord.MapFromCsvRow));
        Assert.Equal(3, record.Scores.Length);
        Assert.Equal(10, record.Scores[0]);
        Assert.Equal(20, record.Scores[1]);
        Assert.Equal(30, record.Scores[2]);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void BareImmutableArray_LengthFollowsHeaderCount()
    {
        var logger = CreateLogger();

        var csv = "Scores[0],Scores[1]\n10,20";
        var record = Assert.Single(CsvLoader.Parse(csv, BareIntArrayRecord.MapFromCsvRow));
        Assert.Equal(2, record.Scores.Length);
        Assert.Equal(10, record.Scores[0]);
        Assert.Equal(20, record.Scores[1]);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void BareImmutableArray_NonContiguousIndex_Throws()
    {
        var logger = CreateLogger();

        var csv = "Scores[0],Scores[2]\n10,30";
        var ex = Assert.Throws<InvalidOperationException>(() => CsvLoader.Parse(csv, BareIntArrayRecord.MapFromCsvRow));
        Assert.IsType<ArgumentException>(ex.InnerException);
        Assert.Empty(logger.Logs);
    }

    [StaticDataRecord("DynamicArray", "Mixed")]
    public sealed partial record BareMixedRecord(
        int Id,
        ImmutableArray<int> Values,
        string Name);

    [Fact]
    public void BareImmutableArray_WorksAlongsideScalarFields()
    {
        var logger = CreateLogger();

        var csv = "Id,Values[0],Values[1],Name\n42,10,20,Test";
        var record = Assert.Single(CsvLoader.Parse(csv, BareMixedRecord.MapFromCsvRow));
        Assert.Equal(42, record.Id);
        Assert.Equal(2, record.Values.Length);
        Assert.Equal(10, record.Values[0]);
        Assert.Equal(20, record.Values[1]);
        Assert.Equal("Test", record.Name);
        Assert.Empty(logger.Logs);
    }

    [StaticDataRecord("DynamicSet", "Int")]
    public sealed partial record BareIntSetRecord(FrozenSet<int> Ids);

    [Fact]
    public void BareFrozenSet_ReadsAllIndexedColumns()
    {
        var logger = CreateLogger();

        var csv = "Ids[0],Ids[1]\n5,7";
        var record = Assert.Single(CsvLoader.Parse(csv, BareIntSetRecord.MapFromCsvRow));
        Assert.Equal(2, record.Ids.Count);

        var ids = record.Ids.ToArray();
        Assert.Contains(5, ids);
        Assert.Contains(7, ids);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void BareFrozenSet_DuplicateValue_Throws()
    {
        var logger = CreateLogger();

        var csv = "Ids[0],Ids[1]\n5,5";
        var ex = Assert.Throws<InvalidOperationException>(() => CsvLoader.Parse(csv, BareIntSetRecord.MapFromCsvRow));
        Assert.IsType<ArgumentException>(ex.InnerException);
        Assert.Empty(logger.Logs);
    }

    public sealed record InventoryItem([Key] int Id, string Name);

    [StaticDataRecord("DynamicDict", "Item")]
    public sealed partial record BareItemMapRecord(FrozenDictionary<int, InventoryItem> Items);

    [Fact]
    public void BareFrozenDictionary_ReadsAllIndexedGroups()
    {
        var logger = CreateLogger();

        var csv = "Items[0].Id,Items[0].Name,Items[1].Id,Items[1].Name\n1,First,2,Second";
        var record = Assert.Single(CsvLoader.Parse(csv, BareItemMapRecord.MapFromCsvRow));
        Assert.Equal(2, record.Items.Count);
        Assert.Equal("First", record.Items[1].Name);
        Assert.Equal("Second", record.Items[2].Name);
        Assert.Empty(logger.Logs);
    }

    public sealed record Point(int X, int Y);

    [StaticDataRecord("DynamicArray", "Records")]
    public sealed partial record BareRecordArrayRecord(ImmutableArray<Point> Points);

    [Fact]
    public void BareImmutableArrayOfRecords_ReadsAllIndexedGroups()
    {
        var logger = CreateLogger();

        var csv = "Points[0].X,Points[0].Y,Points[1].X,Points[1].Y\n1,2,3,4";
        var record = Assert.Single(CsvLoader.Parse(csv, BareRecordArrayRecord.MapFromCsvRow));
        Assert.Equal(2, record.Points.Length);
        Assert.Equal(1, record.Points[0].X);
        Assert.Equal(2, record.Points[0].Y);
        Assert.Equal(3, record.Points[1].X);
        Assert.Equal(4, record.Points[1].Y);
        Assert.Empty(logger.Logs);
    }

    [StaticDataRecord("DynamicSet", "Records")]
    public sealed partial record BareRecordSetRecord(FrozenSet<Point> Points);

    [Fact]
    public void BareFrozenSetOfRecords_ReadsAllIndexedGroups()
    {
        var logger = CreateLogger();

        var csv = "Points[0].X,Points[0].Y,Points[1].X,Points[1].Y\n1,2,3,4";
        var record = Assert.Single(CsvLoader.Parse(csv, BareRecordSetRecord.MapFromCsvRow));
        Assert.Equal(2, record.Points.Count);

        var points = record.Points.ToArray();
        Assert.Contains(new Point(1, 2), points);
        Assert.Contains(new Point(3, 4), points);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void BareFrozenSetOfRecords_DuplicateValue_Throws()
    {
        var logger = CreateLogger();

        var csv = "Points[0].X,Points[0].Y,Points[1].X,Points[1].Y\n1,2,1,2";
        var ex = Assert.Throws<InvalidOperationException>(() => CsvLoader.Parse(csv, BareRecordSetRecord.MapFromCsvRow));
        Assert.IsType<ArgumentException>(ex.InnerException);
        Assert.Empty(logger.Logs);
    }

    private TestOutputLogger<DynamicLengthCollectionMappingTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<DynamicLengthCollectionMappingTests>()
            is not TestOutputLogger<DynamicLengthCollectionMappingTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
