using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using Sdp.Attributes;
using Sdp.Csv;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.CsvMappingBehaviorTests;

public partial class SingleColumnTimeSpanMappingTests(ITestOutputHelper testOutputHelper)
{
    [StaticDataRecord("SingleColumnTimeSpan", "Cooldowns")]
    public sealed partial record CooldownRecord(
        int Id,
        [TimeSpanFormat(@"hh\:mm\:ss")][SingleColumnCollection("|")] ImmutableArray<TimeSpan> Cooldowns);

    [Fact]
    public void SingleColumnCollection_TimeSpanElements_AppliesFormat()
    {
        var logger = CreateLogger();

        var csv = "Id,Cooldowns\n1,00:00:30|01:00:00";

        var record = Assert.Single(CsvLoader.Parse(csv, CooldownRecord.MapFromCsvRow));

        Assert.Equal(2, record.Cooldowns.Length);
        Assert.Equal(TimeSpan.FromSeconds(30), record.Cooldowns[0]);
        Assert.Equal(TimeSpan.FromHours(1), record.Cooldowns[1]);
        logger.LogInformation("Cooldowns mapped: [{C0}, {C1}]", record.Cooldowns[0], record.Cooldowns[1]);
    }

    private TestOutputLogger<SingleColumnTimeSpanMappingTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Information);
        if (factory.CreateLogger<SingleColumnTimeSpanMappingTests>()
            is not TestOutputLogger<SingleColumnTimeSpanMappingTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
