using System.Collections.Frozen;
using Sdp.Attributes;
using Sdp.Csv;

namespace UnitTest.CsvRecordMapperTests;

public class FrozenSetMapperTests
{
    public sealed record RecordWithIntSet([Length(3)] FrozenSet<int> Ids);

    [Fact]
    public void MapFrozenSetOfPrimitives()
    {
        var headers = new[] { "Ids[0]", "Ids[1]", "Ids[2]" };
        var values = new[] { "1", "2", "3" };

        var result = CsvRecordMapper.MapToRecord<RecordWithIntSet>(headers, values);

        Assert.Equal(3, result.Ids.Count);

        var array = result.Ids.ToArray();
        Assert.Contains(1, array);
        Assert.Contains(2, array);
        Assert.Contains(3, array);
    }

    public sealed record RecordWithStringSet([Length(2)] FrozenSet<string> Tags);

    [Fact]
    public void MapFrozenSetOfStrings()
    {
        var headers = new[] { "Tags[0]", "Tags[1]" };
        var values = new[] { "important", "urgent" };

        var result = CsvRecordMapper.MapToRecord<RecordWithStringSet>(headers, values);

        Assert.Equal(2, result.Tags.Count);

        var array = result.Tags.ToArray();
        Assert.Contains("important", array);
        Assert.Contains("urgent", array);
    }

    public sealed record RecordWithColumnNameSet(
        [ColumnName("Tag")][Length(3)] FrozenSet<string> Tags);

    [Fact]
    public void MapFrozenSetWithColumnName()
    {
        var headers = new[] { "Tag[0]", "Tag[1]", "Tag[2]" };
        var values = new[] { "a", "b", "c" };

        var result = CsvRecordMapper.MapToRecord<RecordWithColumnNameSet>(headers, values);

        Assert.Equal(3, result.Tags.Count);

        var array = result.Tags.ToArray();
        Assert.Contains("a", array);
        Assert.Contains("b", array);
        Assert.Contains("c", array);
    }

    public sealed record MixedWithSet(
        int Id,
        [Length(2)] FrozenSet<int> Values,
        string Name);

    [Fact]
    public void MapRecordWithMixedFieldsIncludingSet()
    {
        var headers = new[] { "Id", "Values[0]", "Values[1]", "Name" };
        var values = new[] { "42", "100", "200", "Test" };

        var result = CsvRecordMapper.MapToRecord<MixedWithSet>(headers, values);

        Assert.Equal(42, result.Id);
        Assert.Equal(2, result.Values.Count);

        var array = result.Values.ToArray();
        Assert.Contains(100, array);
        Assert.Contains(200, array);

        Assert.Equal("Test", result.Name);
    }
}
