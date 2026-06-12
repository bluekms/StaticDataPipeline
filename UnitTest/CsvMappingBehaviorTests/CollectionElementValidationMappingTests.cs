using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using Sdp.Attributes;
using Sdp.Csv;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.CsvMappingBehaviorTests;

public partial class CollectionElementValidationMappingTests(ITestOutputHelper testOutputHelper)
{
    [StaticDataRecord("ElementValidation", "Range")]
    public sealed partial record RangeArrayRecord(
        int Id,
        [Length(3)][Range(1, 100)] ImmutableArray<int> Scores);

    [Fact]
    public void FixedArray_ElementsWithinRange_Map()
    {
        var logger = CreateLogger();

        var csv = "Id,Scores[0],Scores[1],Scores[2]\n1,1,50,100";

        var record = Assert.Single(CsvLoader.Parse(csv, RangeArrayRecord.MapFromCsvRow));

        Assert.Equal(3, record.Scores.Length);
        Assert.Equal(1, record.Scores[0]);
        Assert.Equal(50, record.Scores[1]);
        Assert.Equal(100, record.Scores[2]);
        logger.LogInformation(
            "Scores mapped: [{S0}, {S1}, {S2}]",
            record.Scores[0],
            record.Scores[1],
            record.Scores[2]);
    }

    [Fact]
    public void FixedArray_ElementOutOfRange_Throws()
    {
        var logger = CreateLogger();

        var csv = "Id,Scores[0],Scores[1],Scores[2]\n1,1,50,101";

        var ex = Assert.Throws<InvalidOperationException>(() => CsvLoader.Parse(csv, RangeArrayRecord.MapFromCsvRow));

        Assert.IsType<ArgumentOutOfRangeException>(ex.InnerException);
        logger.LogInformation("Out-of-range element threw: {Message}", ex.Message);
    }

    [StaticDataRecord("ElementValidation", "SingleRange")]
    public sealed partial record SingleColumnRangeRecord(
        int Id,
        [SingleColumnCollection(",")][Range(1, 10)] ImmutableArray<int> Levels);

    [Fact]
    public void SingleColumnArray_ElementsWithinRange_Map()
    {
        var logger = CreateLogger();

        var csv = "Id,Levels\n1,\"1,5,10\"";

        var record = Assert.Single(CsvLoader.Parse(csv, SingleColumnRangeRecord.MapFromCsvRow));

        Assert.Equal(3, record.Levels.Length);
        Assert.Equal(1, record.Levels[0]);
        Assert.Equal(5, record.Levels[1]);
        Assert.Equal(10, record.Levels[2]);
        logger.LogInformation(
            "Levels mapped: [{L0}, {L1}, {L2}]",
            record.Levels[0],
            record.Levels[1],
            record.Levels[2]);
    }

    [Fact]
    public void SingleColumnArray_ElementOutOfRange_Throws()
    {
        var logger = CreateLogger();

        var csv = "Id,Levels\n1,\"1,5,11\"";

        var ex = Assert.Throws<InvalidOperationException>(() => CsvLoader.Parse(csv, SingleColumnRangeRecord.MapFromCsvRow));

        Assert.IsType<ArgumentOutOfRangeException>(ex.InnerException);
        logger.LogInformation("Out-of-range single-column element threw: {Message}", ex.Message);
    }

    [StaticDataRecord("ElementValidation", "Regex")]
    public sealed partial record RegexArrayRecord(
        int Id,
        [SingleColumnCollection(",")][RegularExpression("^[a-z]+$")] ImmutableArray<string> Tags);

    [Fact]
    public void SingleColumnArray_ElementsMatchingPattern_Map()
    {
        var logger = CreateLogger();

        var csv = "Id,Tags\n1,\"alpha,beta\"";

        var record = Assert.Single(CsvLoader.Parse(csv, RegexArrayRecord.MapFromCsvRow));

        Assert.Equal(2, record.Tags.Length);
        Assert.Equal("alpha", record.Tags[0]);
        Assert.Equal("beta", record.Tags[1]);
        logger.LogInformation("Tags mapped: [{T0}, {T1}]", record.Tags[0], record.Tags[1]);
    }

    [Fact]
    public void SingleColumnArray_ElementNotMatchingPattern_Throws()
    {
        var logger = CreateLogger();

        var csv = "Id,Tags\n1,\"alpha,Beta\"";

        var ex = Assert.Throws<InvalidOperationException>(() => CsvLoader.Parse(csv, RegexArrayRecord.MapFromCsvRow));

        Assert.IsType<ArgumentException>(ex.InnerException);
        logger.LogInformation("Pattern-mismatch element threw: {Message}", ex.Message);
    }

    private TestOutputLogger<CollectionElementValidationMappingTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Information);
        if (factory.CreateLogger<CollectionElementValidationMappingTests>()
            is not TestOutputLogger<CollectionElementValidationMappingTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
