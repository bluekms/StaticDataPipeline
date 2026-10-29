using Microsoft.Extensions.Logging;
using Sdp.Attributes;
using Sdp.Csv;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.CsvMappingBehaviorTests;

public partial class ChainedTypeBrandingMappingTests(ITestOutputHelper testOutputHelper)
{
    public sealed record Inner(long Value);

    public sealed record Middle(Inner Inner);

    [StaticDataRecord("Branding", "Chained")]
    public sealed partial record ChainedRecord(int Id, Middle Middle);

    [Fact]
    public void FullyExpandedHeader_Maps()
    {
        var logger = CreateLogger();

        var csv = "Id,Middle.Inner.Value\n1,12345";
        var record = Assert.Single(CsvLoader.Parse(csv, ChainedRecord.MapFromCsvRow));
        Assert.Equal(1, record.Id);
        Assert.Equal(12345L, record.Middle.Inner.Value);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void BrandedLeafHeader_Maps()
    {
        var logger = CreateLogger();

        var csv = "Id,Middle.Inner\n1,12345";
        var record = Assert.Single(CsvLoader.Parse(csv, ChainedRecord.MapFromCsvRow));
        Assert.Equal(1, record.Id);
        Assert.Equal(12345L, record.Middle.Inner.Value);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void FullyCollapsedHeader_Throws()
    {
        var logger = CreateLogger();

        var csv = "Id,Middle\n1,12345";
        var ex = Assert.Throws<InvalidOperationException>(() => CsvLoader.Parse(csv, ChainedRecord.MapFromCsvRow));
        Assert.Empty(logger.Logs);
    }

    public sealed record Wrap(Middle Middle);

    [StaticDataRecord("Branding", "DeepChained")]
    public sealed partial record DeepChainedRecord(int Id, Wrap Wrap);

    [Fact]
    public void ThreeLevelChain_BrandedLeafHeader_Maps()
    {
        var logger = CreateLogger();

        var csv = "Id,Wrap.Middle.Inner\n1,777";
        var record = Assert.Single(CsvLoader.Parse(csv, DeepChainedRecord.MapFromCsvRow));
        Assert.Equal(1, record.Id);
        Assert.Equal(777L, record.Wrap.Middle.Inner.Value);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void ThreeLevelChain_IntermediateCollapse_Throws()
    {
        var logger = CreateLogger();

        var csv = "Id,Wrap.Middle\n1,777";
        var ex = Assert.Throws<InvalidOperationException>(() => CsvLoader.Parse(csv, DeepChainedRecord.MapFromCsvRow));
        Assert.Empty(logger.Logs);
    }

    private TestOutputLogger<ChainedTypeBrandingMappingTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<ChainedTypeBrandingMappingTests>() is not TestOutputLogger<ChainedTypeBrandingMappingTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
