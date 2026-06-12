using System.Globalization;
using Microsoft.Extensions.Logging;
using Sdp.Attributes;
using Sdp.Csv;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.CsvMappingBehaviorTests;

public partial class TypedRangeMappingTests(ITestOutputHelper testOutputHelper)
{
    public enum Grade
    {
        Low,
        Mid,
        High,
        Extreme,
    }

    [StaticDataRecord("TypedRange", "Enum")]
    public sealed partial record EnumRangeRecord(
        int Id,
        [Range(typeof(Grade), "Low", "High")] Grade Grade);

    [Fact]
    public void EnumValueInRange_Maps()
    {
        var logger = CreateLogger();

        var csv = "Id,Grade\n1,Mid";

        var record = Assert.Single(CsvLoader.Parse(csv, EnumRangeRecord.MapFromCsvRow));

        Assert.Equal(Grade.Mid, record.Grade);
        logger.LogInformation("In-range enum mapped to {Grade}", record.Grade);
    }

    [Fact]
    public void EnumValueOutOfRange_Throws()
    {
        var logger = CreateLogger();

        var csv = "Id,Grade\n1,Extreme";

        var ex = Assert.Throws<InvalidOperationException>(() => CsvLoader.Parse(csv, EnumRangeRecord.MapFromCsvRow));

        Assert.IsType<ArgumentOutOfRangeException>(ex.InnerException);
        logger.LogInformation("Out-of-range enum threw: {Message}", ex.Message);
    }

    [StaticDataRecord("TypedRange", "String")]
    public sealed partial record StringRangeRecord(
        int Id,
        [Range(typeof(string), "apple", "zebra")] string Fruit);

    [Theory]
    [InlineData("apple")]
    [InlineData("mango")]
    [InlineData("zebra")]
    public void StringValueInLexicalRange_Maps(string cell)
    {
        var logger = CreateLogger();

        var csv = $"Id,Fruit\n1,{cell}";

        var record = Assert.Single(CsvLoader.Parse(csv, StringRangeRecord.MapFromCsvRow));

        Assert.Equal(cell, record.Fruit);
        logger.LogInformation("In-range string mapped to {Fruit}", record.Fruit);
    }

    [Theory]
    [InlineData("ant")]
    [InlineData("zzz")]
    public void StringValueOutOfLexicalRange_Throws(string cell)
    {
        var logger = CreateLogger();

        var csv = $"Id,Fruit\n1,{cell}";

        var ex = Assert.Throws<InvalidOperationException>(() => CsvLoader.Parse(csv, StringRangeRecord.MapFromCsvRow));

        Assert.IsType<ArgumentOutOfRangeException>(ex.InnerException);
        logger.LogInformation("Out-of-range string '{Cell}' threw: {Message}", cell, ex.Message);
    }

    [StaticDataRecord("TypedRange", "DateTime")]
    public sealed partial record DateTimeRangeRecord(
        int Id,
        [DateTimeFormat("yyyy-MM-dd")]
        [Range(typeof(DateTime), "2020-01-01", "2020-12-31")] DateTime Date);

    [Theory]
    [InlineData("2020-01-01")]
    [InlineData("2020-06-15")]
    [InlineData("2020-12-31")]
    public void DateTimeValueInRange_Maps(string cell)
    {
        var logger = CreateLogger();

        var csv = $"Id,Date\n1,{cell}";

        var record = Assert.Single(CsvLoader.Parse(csv, DateTimeRangeRecord.MapFromCsvRow));

        Assert.Equal(DateTime.ParseExact(cell, "yyyy-MM-dd", CultureInfo.InvariantCulture), record.Date);
        logger.LogInformation("In-range DateTime mapped to {Date}", record.Date);
    }

    [Theory]
    [InlineData("2019-12-31")]
    [InlineData("2021-01-01")]
    public void DateTimeValueOutOfRange_Throws(string cell)
    {
        var logger = CreateLogger();

        var csv = $"Id,Date\n1,{cell}";

        var ex = Assert.Throws<InvalidOperationException>(() => CsvLoader.Parse(csv, DateTimeRangeRecord.MapFromCsvRow));

        Assert.IsType<ArgumentOutOfRangeException>(ex.InnerException);
        logger.LogInformation("Out-of-range DateTime '{Cell}' threw: {Message}", cell, ex.Message);
    }

    [StaticDataRecord("TypedRange", "TimeSpan")]
    public sealed partial record TimeSpanRangeRecord(
        int Id,
        [TimeSpanFormat(@"hh\:mm\:ss")]
        [Range(typeof(TimeSpan), "00:00:00", "01:00:00")] TimeSpan Duration);

    [Theory]
    [InlineData("00:00:00")]
    [InlineData("00:30:00")]
    [InlineData("01:00:00")]
    public void TimeSpanValueInRange_Maps(string cell)
    {
        var logger = CreateLogger();

        var csv = $"Id,Duration\n1,{cell}";

        var record = Assert.Single(CsvLoader.Parse(csv, TimeSpanRangeRecord.MapFromCsvRow));

        Assert.Equal(TimeSpan.ParseExact(cell, @"hh\:mm\:ss", CultureInfo.InvariantCulture), record.Duration);
        logger.LogInformation("In-range TimeSpan mapped to {Duration}", record.Duration);
    }

    [Theory]
    [InlineData("01:00:01")]
    [InlineData("02:00:00")]
    public void TimeSpanValueOutOfRange_Throws(string cell)
    {
        var logger = CreateLogger();

        var csv = $"Id,Duration\n1,{cell}";

        var ex = Assert.Throws<InvalidOperationException>(() => CsvLoader.Parse(csv, TimeSpanRangeRecord.MapFromCsvRow));

        Assert.IsType<ArgumentOutOfRangeException>(ex.InnerException);
        logger.LogInformation("Out-of-range TimeSpan '{Cell}' threw: {Message}", cell, ex.Message);
    }

    [StaticDataRecord("TypedRange", "Numeric")]
    public sealed partial record NumericTypedRangeRecord(
        int Id,
        [Range(typeof(long), "1", "10")] long Level);

    [Theory]
    [InlineData("1")]
    [InlineData("5")]
    [InlineData("10")]
    public void NumericTypedValueInRange_Maps(string cell)
    {
        var logger = CreateLogger();

        var csv = $"Id,Level\n1,{cell}";

        var record = Assert.Single(CsvLoader.Parse(csv, NumericTypedRangeRecord.MapFromCsvRow));

        Assert.Equal(long.Parse(cell, CultureInfo.InvariantCulture), record.Level);
        logger.LogInformation("In-range numeric mapped to {Level}", record.Level);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("11")]
    public void NumericTypedValueOutOfRange_Throws(string cell)
    {
        var logger = CreateLogger();

        var csv = $"Id,Level\n1,{cell}";

        var ex = Assert.Throws<InvalidOperationException>(() => CsvLoader.Parse(csv, NumericTypedRangeRecord.MapFromCsvRow));

        Assert.IsType<ArgumentOutOfRangeException>(ex.InnerException);
        logger.LogInformation("Out-of-range numeric '{Cell}' threw: {Message}", cell, ex.Message);
    }

    private TestOutputLogger<TypedRangeMappingTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Information);
        if (factory.CreateLogger<TypedRangeMappingTests>() is not TestOutputLogger<TypedRangeMappingTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
