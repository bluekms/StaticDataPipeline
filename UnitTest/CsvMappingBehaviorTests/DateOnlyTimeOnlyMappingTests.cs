using Microsoft.Extensions.Logging;
using Sdp.Attributes;
using Sdp.Csv;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.CsvMappingBehaviorTests;

public partial class DateOnlyTimeOnlyMappingTests(ITestOutputHelper testOutputHelper)
{
    [StaticDataRecord("DateOnly", "Date")]
    public sealed partial record DateOnlyRecord(
        int Id,
        [DateTimeFormat("yyyy-MM-dd")] global::System.DateOnly Date);

    [Fact]
    public void DateOnly_ParsesWithFormat()
    {
        var logger = CreateLogger();

        var csv = "Id,Date\n1,2026-05-19";

        var record = Assert.Single(CsvLoader.Parse(csv, DateOnlyRecord.MapFromCsvRow));

        Assert.Equal(new global::System.DateOnly(2026, 5, 19), record.Date);
        logger.LogInformation("DateOnly mapped to {Date}", record.Date);
    }

    [Fact]
    public void DateOnly_ValueMismatchFormat_Throws()
    {
        var logger = CreateLogger();

        var csv = "Id,Date\n1,05/19/2026";

        var ex = Assert.Throws<InvalidOperationException>(() => CsvLoader.Parse(csv, DateOnlyRecord.MapFromCsvRow));

        Assert.IsType<FormatException>(ex.InnerException);
        logger.LogInformation("Format mismatch threw: {Message}", ex.Message);
    }

    [StaticDataRecord("TimeOnly", "Time")]
    public sealed partial record TimeOnlyRecord(
        int Id,
        [DateTimeFormat("HH:mm")] global::System.TimeOnly Open);

    [Fact]
    public void TimeOnly_ParsesWithFormat()
    {
        var logger = CreateLogger();

        var csv = "Id,Open\n1,09:30";

        var record = Assert.Single(CsvLoader.Parse(csv, TimeOnlyRecord.MapFromCsvRow));

        Assert.Equal(new global::System.TimeOnly(9, 30), record.Open);
        logger.LogInformation("TimeOnly mapped to {Open}", record.Open);
    }

    [StaticDataRecord("DateOnly", "NullableDate")]
    public sealed partial record NullableDateOnlyRecord(
        int Id,
        [DateTimeFormat("yyyy-MM-dd")][NullString("-")] global::System.DateOnly? Date);

    [Fact]
    public void NullableDateOnly_NullString_ReturnsNull()
    {
        var logger = CreateLogger();

        var csv = "Id,Date\n1,-";

        var record = Assert.Single(CsvLoader.Parse(csv, NullableDateOnlyRecord.MapFromCsvRow));

        Assert.Null(record.Date);
        logger.LogInformation("Nullable DateOnly mapped to null for null string cell");
    }

    private TestOutputLogger<DateOnlyTimeOnlyMappingTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Information);
        if (factory.CreateLogger<DateOnlyTimeOnlyMappingTests>()
            is not TestOutputLogger<DateOnlyTimeOnlyMappingTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
