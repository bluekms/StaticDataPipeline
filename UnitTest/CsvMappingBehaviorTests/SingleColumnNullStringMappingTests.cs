using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using Sdp.Attributes;
using Sdp.Csv;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.CsvMappingBehaviorTests;

public partial class SingleColumnNullStringMappingTests(ITestOutputHelper testOutputHelper)
{
    [StaticDataRecord("ScNs", "Dash")]
    public sealed partial record DashRecord(
        int Id,
        [SingleColumnCollection(",")]
        [CountRange(1, 5)]
        [NullString("-")]
        ImmutableArray<int?> Values);

    [Fact]
    public void NullStringDash_DashCell_LoadsAsNullElement()
    {
        var logger = CreateLogger();

        var csv = "Id,Values\n1,-";

        var record = Assert.Single(CsvLoader.Parse(csv, DashRecord.MapFromCsvRow));

        var value = Assert.Single(record.Values);
        Assert.Null(value);
        logger.LogInformation("Dash cell mapped to single null element");
    }

    [Fact]
    public void NullStringDash_MixedCell_LoadsWithNullInMiddle()
    {
        var logger = CreateLogger();

        var csv = "Id,Values\n1,\"1,-,3\"";

        var record = Assert.Single(CsvLoader.Parse(csv, DashRecord.MapFromCsvRow));

        Assert.Equal(3, record.Values.Length);
        Assert.Equal(1, record.Values[0]);
        Assert.Null(record.Values[1]);
        Assert.Equal(3, record.Values[2]);
        logger.LogInformation(
            "Mixed cell mapped: [{V0}, {V1}, {V2}]",
            record.Values[0],
            record.Values[1],
            record.Values[2]);
    }

    [StaticDataRecord("ScNs", "Empty")]
    public sealed partial record EmptyRecord(
        int Id,
        [SingleColumnCollection(",")]
        [CountRange(1, 5)]
        [NullString("")]
        ImmutableArray<int?> Values);

    [Fact]
    public void NullStringEmpty_EmptyCell_LoadsAsSingleNullElement()
    {
        var logger = CreateLogger();

        var csv = "Id,Values\n1,";

        var record = Assert.Single(CsvLoader.Parse(csv, EmptyRecord.MapFromCsvRow));

        var value = Assert.Single(record.Values);
        Assert.Null(value);
        logger.LogInformation("Empty cell mapped to single null element");
    }

    private TestOutputLogger<SingleColumnNullStringMappingTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Information);
        if (factory.CreateLogger<SingleColumnNullStringMappingTests>()
            is not TestOutputLogger<SingleColumnNullStringMappingTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
