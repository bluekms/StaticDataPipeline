using Sdp.Attributes;
using Sdp.Csv;

namespace UnitTest.CsvRecordMapperTests;

public class PrimitiveTypeMapperTests
{
    public sealed record SimpleRecord(int Id, string Name, double Score);

    [Fact]
    public void MapSimplePrimitiveTypes()
    {
        var headers = new[] { "Id", "Name", "Score" };
        var values = new[] { "1", "Alice", "95.5" };

        var result = CsvRecordMapper.MapToRecord<SimpleRecord>(headers, values);

        Assert.Equal(1, result.Id);
        Assert.Equal("Alice", result.Name);
        Assert.Equal(95.5, result.Score);
    }

    public sealed record RecordWithColumnName(
        [ColumnName("StudentId")] int Id,
        [ColumnName("StudentName")] string Name);

    [Fact]
    public void MapWithColumnNameAttribute()
    {
        var headers = new[] { "StudentId", "StudentName" };
        var values = new[] { "42", "Bob" };

        var result = CsvRecordMapper.MapToRecord<RecordWithColumnName>(headers, values);

        Assert.Equal(42, result.Id);
        Assert.Equal("Bob", result.Name);
    }

    public enum Status
    {
        Active,
        Inactive,
        Pending,
    }

    public sealed record StatusRecord(int Id, Status Status);

    [Fact]
    public void MapEnumType()
    {
        var headers = new[] { "Id", "Status" };
        var values = new[] { "1", "Active" };

        var result = CsvRecordMapper.MapToRecord<StatusRecord>(headers, values);

        Assert.Equal(1, result.Id);
        Assert.Equal(Status.Active, result.Status);
    }

    public sealed record RecordWithNullable(int Id, [NullString("-")] int? OptionalValue, [NullString("N/A")] string? NullableString);

    [Fact]
    public void MapNullableWithNullString()
    {
        var headers = new[] { "Id", "OptionalValue", "NullableString" };
        var values = new[] { "1", "100", "N/A" };

        var result = CsvRecordMapper.MapToRecord<RecordWithNullable>(headers, values);

        Assert.Equal(1, result.Id);
        Assert.Equal(100, result.OptionalValue);
        Assert.Null(result.NullableString);
    }

    public sealed record RecordWithDecimal(decimal Price, DateTime Date);

    [Fact]
    public void MapDecimalAndDateTime()
    {
        var headers = new[] { "Price", "Date" };
        var values = new[] { "123.45", "2024-12-22" };

        var result = CsvRecordMapper.MapToRecord<RecordWithDecimal>(headers, values);

        Assert.Equal(123.45m, result.Price);
        Assert.Equal(new DateTime(2024, 12, 22), result.Date);
    }
}
