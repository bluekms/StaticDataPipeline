using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Sdp.Attributes;
using Sdp.Csv;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.CsvMappingBehaviorTests;

public partial class DateTimeFormatMappingTests(ITestOutputHelper testOutputHelper)
{
    [StaticDataRecord("DateTime", "Iso")]
    public sealed partial record DateTimeRecord(
        int Id,
        [DateTimeFormat("yyyy-MM-dd")] DateTime Date);

    [Fact]
    public void DateTime_StandardIsoFormat_Parses()
    {
        var logger = CreateLogger();

        var csv = "Id,Date\n1,2026-05-19";

        var record = Assert.Single(CsvLoader.Parse(csv, DateTimeRecord.MapFromCsvRow));

        Assert.Equal(new DateTime(2026, 5, 19), record.Date);
        logger.LogInformation("ISO format DateTime mapped to {Date}", record.Date);
    }

    [StaticDataRecord("DateTime", "Us")]
    public sealed partial record DateTimeUsFormatRecord(
        int Id,
        [DateTimeFormat("MM/dd/yyyy")] DateTime Date);

    [Fact]
    public void DateTime_NonIsoFormat_ParsesWithExactFormat()
    {
        var logger = CreateLogger();

        var csv = "Id,Date\n1,05/19/2026";

        var record = Assert.Single(CsvLoader.Parse(csv, DateTimeUsFormatRecord.MapFromCsvRow));

        Assert.Equal(new DateTime(2026, 5, 19), record.Date);
        logger.LogInformation("US format DateTime mapped to {Date}", record.Date);
    }

    [Fact]
    public void DateTime_ValueMismatchFormat_Throws()
    {
        var logger = CreateLogger();

        var csv = "Id,Date\n1,2026-05-19";

        var ex = Assert.Throws<InvalidOperationException>(() => CsvLoader.Parse(csv, DateTimeUsFormatRecord.MapFromCsvRow));

        Assert.IsType<FormatException>(ex.InnerException);
        logger.LogInformation("DateTime format mismatch threw: {Message}", ex.Message);
    }

    [StaticDataRecord("DateTime", "NullableDate")]
    public sealed partial record NullableDateTimeRecord(
        int Id,
        [DateTimeFormat("yyyy-MM-dd")][NullString("NULL")] DateTime? Date);

    [Fact]
    public void NullableDateTime_NullString_ReturnsNull()
    {
        var logger = CreateLogger();

        var csv = "Id,Date\n1,NULL";

        var record = Assert.Single(CsvLoader.Parse(csv, NullableDateTimeRecord.MapFromCsvRow));

        Assert.Null(record.Date);
        logger.LogInformation("Nullable DateTime mapped to null for null string cell");
    }

    [Fact]
    public void NullableDateTime_Value_ParsesWithFormat()
    {
        var logger = CreateLogger();

        var csv = "Id,Date\n1,2026-05-19";

        var record = Assert.Single(CsvLoader.Parse(csv, NullableDateTimeRecord.MapFromCsvRow));

        Assert.Equal(new DateTime(2026, 5, 19), record.Date);
        logger.LogInformation("Nullable DateTime mapped to {Date}", record.Date);
    }

    [StaticDataRecord("DateTime", "TimeSpan")]
    public sealed partial record TimeSpanRecord(
        int Id,
        [TimeSpanFormat(@"hh\:mm\:ss")] TimeSpan Cooldown);

    [Fact]
    public void TimeSpan_WithFormat_Parses()
    {
        var logger = CreateLogger();

        var csv = "Id,Cooldown\n1,01:30:00";

        var record = Assert.Single(CsvLoader.Parse(csv, TimeSpanRecord.MapFromCsvRow));

        Assert.Equal(TimeSpan.FromMinutes(90), record.Cooldown);
        logger.LogInformation("TimeSpan mapped to {Cooldown}", record.Cooldown);
    }

    [Fact]
    public void TimeSpan_ValueMismatchFormat_Throws()
    {
        var logger = CreateLogger();

        var csv = "Id,Cooldown\n1,90m";

        var ex = Assert.Throws<InvalidOperationException>(() => CsvLoader.Parse(csv, TimeSpanRecord.MapFromCsvRow));

        Assert.IsType<FormatException>(ex.InnerException);
        logger.LogInformation("TimeSpan format mismatch threw: {Message}", ex.Message);
    }

    [StaticDataRecord("DateTime", "NullableTimeSpan")]
    public sealed partial record NullableTimeSpanRecord(
        int Id,
        [TimeSpanFormat(@"hh\:mm\:ss")][NullString("-")] TimeSpan? Cooldown);

    [Fact]
    public void NullableTimeSpan_NullString_ReturnsNull()
    {
        var logger = CreateLogger();

        var csv = "Id,Cooldown\n1,-";

        var record = Assert.Single(CsvLoader.Parse(csv, NullableTimeSpanRecord.MapFromCsvRow));

        Assert.Null(record.Cooldown);
        logger.LogInformation("Nullable TimeSpan mapped to null for null string cell");
    }

    // 포맷 속성이 없는 DateTime/TimeSpan 은 이제 컴파일타임에 SDP0004(Error)로 차단되므로
    // 런타임 throw 동작 테스트 대신 생성기 테스트(Skips_emit_for_*_without_*Format)가 이를 검증한다.
    [StaticDataRecord("DateTime", "DateArray")]
    public sealed partial record DateTimeArrayRecord(
        int Id,
        [DateTimeFormat("yyyy-MM-dd")][Length(2)] ImmutableArray<DateTime> Period);

    [Fact]
    public void ImmutableArray_DateTimeElements_AppliesFormatPerElement()
    {
        var logger = CreateLogger();

        var csv = "Id,Period[0],Period[1]\n1,2026-05-01,2026-05-31";

        var record = Assert.Single(CsvLoader.Parse(csv, DateTimeArrayRecord.MapFromCsvRow));

        Assert.Equal(new DateTime(2026, 5, 1), record.Period[0]);
        Assert.Equal(new DateTime(2026, 5, 31), record.Period[1]);
        logger.LogInformation("Period mapped: [{P0}, {P1}]", record.Period[0], record.Period[1]);
    }

    [StaticDataRecord("DateTime", "SpanArray")]
    public sealed partial record TimeSpanArrayRecord(
        int Id,
        [TimeSpanFormat(@"hh\:mm\:ss")][Length(2)] ImmutableArray<TimeSpan> Cooldowns);

    [Fact]
    public void ImmutableArray_TimeSpanElements_AppliesFormatPerElement()
    {
        var logger = CreateLogger();

        var csv = "Id,Cooldowns[0],Cooldowns[1]\n1,00:00:30,01:00:00";

        var record = Assert.Single(CsvLoader.Parse(csv, TimeSpanArrayRecord.MapFromCsvRow));

        Assert.Equal(TimeSpan.FromSeconds(30), record.Cooldowns[0]);
        Assert.Equal(TimeSpan.FromHours(1), record.Cooldowns[1]);
        logger.LogInformation("Cooldowns mapped: [{C0}, {C1}]", record.Cooldowns[0], record.Cooldowns[1]);
    }

    [StaticDataRecord("DateTime", "SingleColDate")]
    public sealed partial record SingleColumnDateTimeRecord(
        int Id,
        [DateTimeFormat("yyyy-MM-dd")][SingleColumnCollection("|")] ImmutableArray<DateTime> Dates);

    [Fact]
    public void SingleColumnCollection_DateTimeElements_AppliesFormat()
    {
        var logger = CreateLogger();

        var csv = "Id,Dates\n1,2026-05-01|2026-05-02|2026-05-03";

        var record = Assert.Single(CsvLoader.Parse(csv, SingleColumnDateTimeRecord.MapFromCsvRow));

        Assert.Equal(3, record.Dates.Length);
        Assert.Equal(new DateTime(2026, 5, 1), record.Dates[0]);
        Assert.Equal(new DateTime(2026, 5, 3), record.Dates[2]);
        logger.LogInformation(
            "Dates mapped with {Count} elements, first={First}",
            record.Dates.Length,
            record.Dates[0]);
    }

    [StaticDataRecord("DateTime", "DateSet")]
    public sealed partial record FrozenSetDateTimeRecord(
        int Id,
        [DateTimeFormat("yyyy-MM-dd")][Length(2)] FrozenSet<DateTime> Dates);

    [Fact]
    public void FrozenSet_DateTimeElements_AppliesFormat()
    {
        var logger = CreateLogger();

        var csv = "Id,Dates[0],Dates[1]\n1,2026-05-01,2026-05-02";

        var record = Assert.Single(CsvLoader.Parse(csv, FrozenSetDateTimeRecord.MapFromCsvRow));

        Assert.Equal(2, record.Dates.Count);
        Assert.Contains(new DateTime(2026, 5, 1), (IEnumerable<DateTime>)record.Dates);
        Assert.Contains(new DateTime(2026, 5, 2), (IEnumerable<DateTime>)record.Dates);
        logger.LogInformation("Date set mapped with {Count} elements", record.Dates.Count);
    }

    [Theory]
    [InlineData("yyyy-MM-ddTHH:mm:ss", "2026-05-19T09:30:00", true)]
    [InlineData("yyyy-MM-dd'T'HH:mm:ss", "2026-05-19T09:30:00", true)]
    [InlineData("yyyy-MM-dd,HH:mm:ss", "2026-05-19,09:30:00", true)]
    [InlineData("yyyy-MM-dd','HH:mm:ss", "2026-05-19,09:30:00", true)]
    [InlineData("yyyy-MM-ddFooHH:mm:ss", "2026-05-19Foo09:30:00", false)]
    [InlineData("yyyy-MM-dd'Foo'HH:mm:ss", "2026-05-19Foo09:30:00", true)]
    public void DateTimeFormat_LiteralSeparator(string format, string input, bool shouldSucceed)
    {
        var logger = CreateLogger();

        if (shouldSucceed)
        {
            var parsed = DateTime.ParseExact(input, format, CultureInfo.InvariantCulture);

            Assert.Equal(new DateTime(2026, 5, 19, 9, 30, 0), parsed);
            logger.LogInformation("format '{Format}' parsed '{Input}' to {Parsed}", format, input, parsed);
        }
        else
        {
            var ex = Assert.Throws<FormatException>(
                () => DateTime.ParseExact(input, format, CultureInfo.InvariantCulture));

            logger.LogInformation("format '{Format}' on '{Input}' threw: {Message}", format, input, ex.Message);
        }
    }

    private TestOutputLogger<DateTimeFormatMappingTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Information);
        if (factory.CreateLogger<DateTimeFormatMappingTests>()
            is not TestOutputLogger<DateTimeFormatMappingTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
