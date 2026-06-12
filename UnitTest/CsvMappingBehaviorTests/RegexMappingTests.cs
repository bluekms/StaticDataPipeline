using Microsoft.Extensions.Logging;
using Sdp.Attributes;
using Sdp.Csv;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.CsvMappingBehaviorTests;

public partial class RegexMappingTests(ITestOutputHelper testOutputHelper)
{
    [StaticDataRecord("Regex", "Icon")]
    public sealed partial record IconRecord(
        int Id,
        [RegularExpression(@"^icons/[a-z]+\.png$")] string IconPath);

    [Theory]
    [InlineData("icons/sword.png")]
    [InlineData("icons/shield.png")]
    public void MatchingValue_Maps(string cell)
    {
        var logger = CreateLogger();

        var csv = $"Id,IconPath\n1,{cell}";

        var record = Assert.Single(CsvLoader.Parse(csv, IconRecord.MapFromCsvRow));

        Assert.Equal(cell, record.IconPath);
        logger.LogInformation("Matching cell mapped to {IconPath}", record.IconPath);
    }

    [Theory]
    [InlineData("icons/Sword.png")]
    [InlineData("sword.png")]
    [InlineData("icons/sword.jpg")]
    public void NonMatchingValue_Throws(string cell)
    {
        var logger = CreateLogger();

        var csv = $"Id,IconPath\n1,{cell}";

        var ex = Assert.Throws<InvalidOperationException>(() => CsvLoader.Parse(csv, IconRecord.MapFromCsvRow));

        Assert.IsType<ArgumentException>(ex.InnerException);
        logger.LogInformation("Non-matching cell '{Cell}' threw: {Message}", cell, ex.Message);
    }

    [StaticDataRecord("Regex", "NullableIcon")]
    public sealed partial record NullableIconRecord(
        int Id,
        [NullString("-")][RegularExpression(@"^icons/[a-z]+\.png$")] string? IconPath);

    [Fact]
    public void NullableValueWithNullString_SkipsPattern()
    {
        var logger = CreateLogger();

        var csv = "Id,IconPath\n1,-";

        var record = Assert.Single(CsvLoader.Parse(csv, NullableIconRecord.MapFromCsvRow));

        Assert.Null(record.IconPath);
        logger.LogInformation("Null string cell skipped pattern validation and mapped to null");
    }

    private TestOutputLogger<RegexMappingTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Information);
        if (factory.CreateLogger<RegexMappingTests>() is not TestOutputLogger<RegexMappingTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
