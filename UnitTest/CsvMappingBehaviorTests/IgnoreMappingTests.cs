using Microsoft.Extensions.Logging;
using Sdp.Attributes;
using Sdp.Csv;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.CsvMappingBehaviorTests;

public partial class IgnoreMappingTests(ITestOutputHelper testOutputHelper)
{
    [StaticDataRecord("Ignore", "Main")]
    public sealed partial record CachedRecord(int Id, [Ignore] int CachedValue, [Ignore] string? Note, string Name);

    [Fact]
    public void IgnoredParameters_AreDefaulted_WhenColumnsAbsent()
    {
        var logger = CreateLogger();

        var csv = "Id,Name\n1,Alpha";
        var record = Assert.Single(CsvLoader.Parse(csv, CachedRecord.MapFromCsvRow));
        Assert.Equal(1, record.Id);
        Assert.Equal(0, record.CachedValue);
        Assert.Null(record.Note);
        Assert.Equal("Alpha", record.Name);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void IgnoredParameters_AreDefaulted_EvenWhenColumnsPresent()
    {
        var logger = CreateLogger();

        var csv = "Id,CachedValue,Note,Name\n1,99,memo,Alpha";
        var record = Assert.Single(CsvLoader.Parse(csv, CachedRecord.MapFromCsvRow));
        Assert.Equal(0, record.CachedValue);
        Assert.Null(record.Note);
        Assert.Empty(logger.Logs);
    }

    private TestOutputLogger<IgnoreMappingTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<IgnoreMappingTests>() is not TestOutputLogger<IgnoreMappingTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
