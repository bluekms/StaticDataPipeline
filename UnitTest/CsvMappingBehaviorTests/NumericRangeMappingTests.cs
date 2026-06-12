using Microsoft.Extensions.Logging;
using Sdp.Attributes;
using Sdp.Csv;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.CsvMappingBehaviorTests;

public partial class NumericRangeMappingTests(ITestOutputHelper testOutputHelper)
{
    [StaticDataRecord("Range", "Int")]
    public sealed partial record IntRangeRecord(int Id, [Range(1, 100)] int Score);

    [Theory]
    [InlineData("1", 1)]
    [InlineData("50", 50)]
    [InlineData("100", 100)]
    public void IntValueInRange_Maps(string cell, int expected)
    {
        var logger = CreateLogger();

        var csv = $"Id,Score\n1,{cell}";

        var record = Assert.Single(CsvLoader.Parse(csv, IntRangeRecord.MapFromCsvRow));

        Assert.Equal(expected, record.Score);
        logger.LogInformation("In-range cell '{Cell}' mapped to {Score}", cell, record.Score);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("101")]
    public void IntValueOutOfRange_Throws(string cell)
    {
        var logger = CreateLogger();

        var csv = $"Id,Score\n1,{cell}";

        var ex = Assert.Throws<InvalidOperationException>(() => CsvLoader.Parse(csv, IntRangeRecord.MapFromCsvRow));

        Assert.IsType<ArgumentOutOfRangeException>(ex.InnerException);
        logger.LogInformation("Out-of-range cell '{Cell}' threw: {Message}", cell, ex.Message);
    }

    [StaticDataRecord("Range", "NullableInt")]
    public sealed partial record NullableIntRangeRecord(
        int Id,
        [NullString("-")][Range(1, 100)] int? Score);

    [Fact]
    public void NullableValueWithNullString_SkipsRange()
    {
        var logger = CreateLogger();

        var csv = "Id,Score\n1,-";

        var record = Assert.Single(CsvLoader.Parse(csv, NullableIntRangeRecord.MapFromCsvRow));

        Assert.Null(record.Score);
        logger.LogInformation("Null string cell skipped range validation and mapped to null");
    }

    [StaticDataRecord("Range", "Float")]
    public sealed partial record FloatRangeRecord(int Id, [Range(0.0, 0.1)] float Ratio);

    [Fact]
    public void FloatValueAtUpperBoundary_Maps()
    {
        var logger = CreateLogger();

        var csv = "Id,Ratio\n1,0.1";

        var record = Assert.Single(CsvLoader.Parse(csv, FloatRangeRecord.MapFromCsvRow));

        Assert.Equal(0.1f, record.Ratio);
        logger.LogInformation("Upper boundary value mapped to {Ratio}", record.Ratio);
    }

    private TestOutputLogger<NumericRangeMappingTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Information);
        if (factory.CreateLogger<NumericRangeMappingTests>() is not TestOutputLogger<NumericRangeMappingTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
