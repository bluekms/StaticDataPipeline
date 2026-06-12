using System.Collections.Frozen;
using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using Sdp.Attributes;
using Sdp.Csv;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.CsvMappingBehaviorTests;

public partial class CountRangeMappingTests(ITestOutputHelper testOutputHelper)
{
    [StaticDataRecord("CountRange", "Tags")]
    public sealed partial record TagsRecord(
        int Id,
        [SingleColumnCollection(",")][CountRange(2, 4)] ImmutableArray<string> Tags);

    [Theory]
    [InlineData("a,b", 2)]
    [InlineData("a,b,c", 3)]
    [InlineData("a,b,c,d", 4)]
    public void CountWithinRange_Maps(string cell, int expectedCount)
    {
        var logger = CreateLogger();

        var csv = $"Id,Tags\n1,\"{cell}\"";

        var record = Assert.Single(CsvLoader.Parse(csv, TagsRecord.MapFromCsvRow));

        Assert.Equal(expectedCount, record.Tags.Length);
        logger.LogInformation("Tags cell '{Cell}' mapped to {Count} elements", cell, record.Tags.Length);
    }

    [Fact]
    public void CountBelowMin_Throws()
    {
        var logger = CreateLogger();

        var csv = "Id,Tags\n1,a";

        var ex = Assert.Throws<InvalidOperationException>(() => CsvLoader.Parse(csv, TagsRecord.MapFromCsvRow));

        Assert.IsType<ArgumentException>(ex.InnerException);
        logger.LogInformation("Count below min threw: {Message}", ex.Message);
    }

    [Fact]
    public void CountAboveMax_Throws()
    {
        var logger = CreateLogger();

        var csv = "Id,Tags\n1,\"a,b,c,d,e\"";

        var ex = Assert.Throws<InvalidOperationException>(() => CsvLoader.Parse(csv, TagsRecord.MapFromCsvRow));

        Assert.IsType<ArgumentException>(ex.InnerException);
        logger.LogInformation("Count above max threw: {Message}", ex.Message);
    }

    [StaticDataRecord("CountRange", "TagSet")]
    public sealed partial record TagSetRecord(
        int Id,
        [SingleColumnCollection(",")][CountRange(2, 4)] FrozenSet<string> Tags);

    [Theory]
    [InlineData("a,b", 2)]
    [InlineData("a,b,c", 3)]
    [InlineData("a,b,c,d", 4)]
    public void FrozenSetCountWithinRange_Maps(string cell, int expectedCount)
    {
        var logger = CreateLogger();

        var csv = $"Id,Tags\n1,\"{cell}\"";

        var record = Assert.Single(CsvLoader.Parse(csv, TagSetRecord.MapFromCsvRow));

        Assert.Equal(expectedCount, record.Tags.Count);
        logger.LogInformation("Tag set cell '{Cell}' mapped to {Count} elements", cell, record.Tags.Count);
    }

    [Fact]
    public void FrozenSetWithDuplicate_Throws()
    {
        var logger = CreateLogger();

        var csv = "Id,Tags\n1,\"a,b,a\"";

        var ex = Assert.Throws<InvalidOperationException>(() => CsvLoader.Parse(csv, TagSetRecord.MapFromCsvRow));

        Assert.IsType<ArgumentException>(ex.InnerException);
        logger.LogInformation("Duplicate set element threw: {Message}", ex.Message);
    }

    private TestOutputLogger<CountRangeMappingTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Information);
        if (factory.CreateLogger<CountRangeMappingTests>() is not TestOutputLogger<CountRangeMappingTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
