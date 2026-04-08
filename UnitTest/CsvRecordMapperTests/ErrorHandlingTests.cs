using System.Globalization;
using Sdp.Csv;

namespace UnitTest.CsvRecordMapperTests;

public class ErrorHandlingTests
{
    public sealed record SimpleRecord(int Id, string Name);

    [Fact]
    public void ThrowsWhenHeaderNotFound()
    {
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

        var headers = new[] { "Id" };
        var values = new[] { "1" };

        var exception = Assert.Throws<InvalidOperationException>(
            () => CsvRecordMapper.MapToRecord<SimpleRecord>(headers, values));

        Assert.Equal("Header 'Name' not found in CSV", exception.Message);
    }

    public enum Status
    {
        Active,
        Inactive,
    }

    public sealed record StatusRecord(int Id, Status Status);

    [Fact]
    public void ThrowsWhenInvalidEnumValue()
    {
        var headers = new[] { "Id", "Status" };
        var values = new[] { "1", "InvalidStatus" };

        Assert.Throws<ArgumentException>(
            () => CsvRecordMapper.MapToRecord<StatusRecord>(headers, values));
    }

    [Fact]
    public void ThrowsWhenInvalidIntegerValue()
    {
        var headers = new[] { "Id", "Name" };
        var values = new[] { "not_a_number", "Test" };

        Assert.Throws<FormatException>(
            () => CsvRecordMapper.MapToRecord<SimpleRecord>(headers, values));
    }

    public sealed record RecordWithDouble(double Value);

    [Fact]
    public void ThrowsWhenInvalidDoubleValue()
    {
        var headers = new[] { "Value" };
        var values = new[] { "not_a_double" };

        Assert.Throws<FormatException>(
            () => CsvRecordMapper.MapToRecord<RecordWithDouble>(headers, values));
    }
}
