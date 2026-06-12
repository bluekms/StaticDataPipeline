using Microsoft.Extensions.Logging;
using Sdp.Attributes;
using Sdp.Csv;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.CsvMappingBehaviorTests;

public partial class ErrorHandlingMappingTests(ITestOutputHelper testOutputHelper)
{
    public enum Status
    {
        Active,
        Inactive,
    }

    [StaticDataRecord("Error", "Status")]
    public sealed partial record StatusRecord(int Id, Status Status);

    [Fact]
    public void ThrowsWhenInvalidEnumValue()
    {
        var logger = CreateLogger();

        var csv = "Id,Status\n1,InvalidStatus";

        var ex = Assert.Throws<InvalidOperationException>(() => CsvLoader.Parse(csv, StatusRecord.MapFromCsvRow));

        Assert.IsType<ArgumentException>(ex.InnerException);
        logger.LogInformation("Invalid enum value threw: {Message}", ex.Message);
    }

    [StaticDataRecord("Error", "Simple")]
    public sealed partial record SimpleRecord(int Id, string Name);

    [Fact]
    public void ThrowsWhenInvalidIntegerValue()
    {
        var logger = CreateLogger();

        var csv = "Id,Name\nnot_a_number,Test";

        var ex = Assert.Throws<InvalidOperationException>(() => CsvLoader.Parse(csv, SimpleRecord.MapFromCsvRow));

        Assert.IsType<FormatException>(ex.InnerException);
        logger.LogInformation("Invalid integer value threw: {Message}", ex.Message);
    }

    [Fact]
    public void ThrowsWhenHeaderNotFound()
    {
        var logger = CreateLogger();

        var csv = "Id\n1";

        var ex = Assert.Throws<InvalidOperationException>(() => CsvLoader.Parse(csv, SimpleRecord.MapFromCsvRow));

        Assert.IsType<InvalidOperationException>(ex.InnerException);
        Assert.Contains("'Name'", ex.Message);
        logger.LogInformation("Missing header threw: {Message}", ex.Message);
    }

    [StaticDataRecord("Error", "Double")]
    public sealed partial record RecordWithDoubleRecord(double Value);

    [Fact]
    public void ThrowsWhenInvalidDoubleValue()
    {
        var logger = CreateLogger();

        var csv = "Value\nnot_a_double";

        var ex = Assert.Throws<InvalidOperationException>(() => CsvLoader.Parse(csv, RecordWithDoubleRecord.MapFromCsvRow));

        Assert.IsType<FormatException>(ex.InnerException);
        logger.LogInformation("Invalid double value threw: {Message}", ex.Message);
    }

    private TestOutputLogger<ErrorHandlingMappingTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Information);
        if (factory.CreateLogger<ErrorHandlingMappingTests>() is not TestOutputLogger<ErrorHandlingMappingTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
