using System.Collections.Immutable;
using Sdp.Attributes;
using Sdp.Csv;

namespace UnitTest.CsvRecordMapperTests;

public class ImmutableArrayMapperTests
{
    public sealed record RecordWithIntArray([Length(3)] ImmutableArray<int> Scores);

    [Fact]
    public void MapImmutableArrayOfPrimitives()
    {
        var headers = new[] { "Scores[0]", "Scores[1]", "Scores[2]" };
        var values = new[] { "10", "20", "30" };

        var result = CsvRecordMapper.MapToRecord<RecordWithIntArray>(headers, values);

        Assert.Equal(3, result.Scores.Length);
        Assert.Equal(10, result.Scores[0]);
        Assert.Equal(20, result.Scores[1]);
        Assert.Equal(30, result.Scores[2]);
    }

    public sealed record RecordWithStringArray([Length(2)] ImmutableArray<string> Names);

    [Fact]
    public void MapImmutableArrayOfStrings()
    {
        var headers = new[] { "Names[0]", "Names[1]" };
        var values = new[] { "Alice", "Bob" };

        var result = CsvRecordMapper.MapToRecord<RecordWithStringArray>(headers, values);

        Assert.Equal(2, result.Names.Length);
        Assert.Equal("Alice", result.Names[0]);
        Assert.Equal("Bob", result.Names[1]);
    }

    public sealed record RecordWithColumnNameArray([ColumnName("Score")][Length(3)] ImmutableArray<int> Scores);

    [Fact]
    public void MapImmutableArrayWithColumnName()
    {
        var headers = new[] { "Score[0]", "Score[1]", "Score[2]" };
        var values = new[] { "100", "200", "300" };

        var result = CsvRecordMapper.MapToRecord<RecordWithColumnNameArray>(headers, values);

        Assert.Equal(3, result.Scores.Length);
        Assert.Equal(100, result.Scores[0]);
        Assert.Equal(200, result.Scores[1]);
        Assert.Equal(300, result.Scores[2]);
    }

    public sealed record InnerRecord(int Id, string Name);

    public sealed record RecordWithRecordArray([Length(2)] ImmutableArray<InnerRecord> Items);

    [Fact]
    public void MapImmutableArrayOfRecords()
    {
        var headers = new[]
        {
            "Items[0].Id", "Items[0].Name",
            "Items[1].Id", "Items[1].Name",
        };
        var values = new[] { "1", "First", "2", "Second" };

        var result = CsvRecordMapper.MapToRecord<RecordWithRecordArray>(headers, values);

        Assert.Equal(2, result.Items.Length);
        Assert.Equal(1, result.Items[0].Id);
        Assert.Equal("First", result.Items[0].Name);
        Assert.Equal(2, result.Items[1].Id);
        Assert.Equal("Second", result.Items[1].Name);
    }

    public sealed record MixedRecord(int Id, [Length(2)] ImmutableArray<int> Values, string Name);

    [Fact]
    public void MapRecordWithMixedFields()
    {
        var headers = new[] { "Id", "Values[0]", "Values[1]", "Name" };
        var values = new[] { "42", "10", "20", "Test" };

        var result = CsvRecordMapper.MapToRecord<MixedRecord>(headers, values);

        Assert.Equal(42, result.Id);
        Assert.Equal(2, result.Values.Length);
        Assert.Equal(10, result.Values[0]);
        Assert.Equal(20, result.Values[1]);
        Assert.Equal("Test", result.Name);
    }
}
