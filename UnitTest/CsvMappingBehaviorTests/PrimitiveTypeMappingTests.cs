using Microsoft.Extensions.Logging;
using Sdp.Attributes;
using Sdp.Csv;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.CsvMappingBehaviorTests;

public partial class PrimitiveTypeMappingTests(ITestOutputHelper testOutputHelper)
{
    [StaticDataRecord("Primitive", "Simple")]
    public sealed partial record SimpleRecord(int Id, string Name, double Score);

    [Fact]
    public void MapSimplePrimitiveTypes()
    {
        var logger = CreateLogger();

        var csv = "Id,Name,Score\n1,Alice,95.5";
        var record = Assert.Single(CsvLoader.Parse(csv, SimpleRecord.MapFromCsvRow));
        Assert.Equal(1, record.Id);
        Assert.Equal("Alice", record.Name);
        Assert.Equal(95.5, record.Score);
        Assert.Empty(logger.Logs);
    }

    [StaticDataRecord("Primitive", "ColumnName")]
    public sealed partial record RecordWithColumnNameRecord(
        [ColumnName("StudentId")] int Id,
        [ColumnName("StudentName")] string Name);

    [Fact]
    public void MapWithColumnNameAttribute()
    {
        var logger = CreateLogger();

        var csv = "StudentId,StudentName\n42,Bob";
        var record = Assert.Single(CsvLoader.Parse(csv, RecordWithColumnNameRecord.MapFromCsvRow));
        Assert.Equal(42, record.Id);
        Assert.Equal("Bob", record.Name);
        Assert.Empty(logger.Logs);
    }

    [StaticDataRecord("Primitive", "Nullable")]
    public sealed partial record RecordWithNullableRecord(
        int Id,
        [NullString("-")] int? OptionalValue,
        [NullString("N/A")] string? NullableString);

    [Fact]
    public void MapNullableWithNullString()
    {
        var logger = CreateLogger();

        var csv = "Id,OptionalValue,NullableString\n1,100,N/A";
        var record = Assert.Single(CsvLoader.Parse(csv, RecordWithNullableRecord.MapFromCsvRow));
        Assert.Equal(1, record.Id);
        Assert.Equal(100, record.OptionalValue);
        Assert.Null(record.NullableString);
        Assert.Empty(logger.Logs);
    }

    [StaticDataRecord("Primitive", "Decimal")]
    public sealed partial record RecordWithDecimalRecord(
        decimal Price,
        [DateTimeFormat("yyyy-MM-dd")] DateTime Date);

    [Fact]
    public void MapDecimalAndDateTime()
    {
        var logger = CreateLogger();

        var csv = "Price,Date\n123.45,2024-12-22";
        var record = Assert.Single(CsvLoader.Parse(csv, RecordWithDecimalRecord.MapFromCsvRow));
        Assert.Equal(123.45m, record.Price);
        Assert.Equal(new DateTime(2024, 12, 22), record.Date);
        Assert.Empty(logger.Logs);
    }

    private TestOutputLogger<PrimitiveTypeMappingTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<PrimitiveTypeMappingTests>() is not TestOutputLogger<PrimitiveTypeMappingTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
