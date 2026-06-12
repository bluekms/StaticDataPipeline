using Microsoft.Extensions.Logging;
using Sdp.Attributes;
using Sdp.Csv;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.CsvMappingBehaviorTests;

public partial class SingleParameterMappingTests(ITestOutputHelper testOutputHelper)
{
    public sealed record Identifier(int Value);

    [StaticDataRecord("Single", "Entity")]
    public sealed partial record EntityRecord(Identifier Id, string Name);

    [Fact]
    public void MapSingleParameterRecord_UsesParameterNameAsHeader()
    {
        var logger = CreateLogger();

        var csv = "Id,Name\n42,Test Entity";

        var record = Assert.Single(CsvLoader.Parse(csv, EntityRecord.MapFromCsvRow));

        Assert.Equal(42, record.Id.Value);
        Assert.Equal("Test Entity", record.Name);
        logger.LogInformation("Entity mapped: Id={Id}, Name={Name}", record.Id.Value, record.Name);
    }

    public sealed record UserId(long Value);

    public sealed record ProductId(int Value);

    [StaticDataRecord("Single", "Order")]
    public sealed partial record OrderRecord(UserId UserId, ProductId ProductId, int Quantity);

    [Fact]
    public void MapMultipleSingleParameterRecords()
    {
        var logger = CreateLogger();

        var csv = "UserId,ProductId,Quantity\n1001,500,3";

        var record = Assert.Single(CsvLoader.Parse(csv, OrderRecord.MapFromCsvRow));

        Assert.Equal(1001L, record.UserId.Value);
        Assert.Equal(500, record.ProductId.Value);
        Assert.Equal(3, record.Quantity);
        logger.LogInformation(
            "Order mapped: UserId={UserId}, ProductId={ProductId}, Quantity={Quantity}",
            record.UserId.Value,
            record.ProductId.Value,
            record.Quantity);
    }

    public sealed record DecimalId(decimal Value);

    [StaticDataRecord("Single", "Price")]
    public sealed partial record PriceRecord(DecimalId Id, decimal Amount);

    [Fact]
    public void MapSingleParameterRecord_WithDecimalType()
    {
        var logger = CreateLogger();

        var csv = "Id,Amount\n123.456,999.99";

        var record = Assert.Single(CsvLoader.Parse(csv, PriceRecord.MapFromCsvRow));

        Assert.Equal(123.456m, record.Id.Value);
        Assert.Equal(999.99m, record.Amount);
        logger.LogInformation("Price mapped: Id={Id}, Amount={Amount}", record.Id.Value, record.Amount);
    }

    public enum Grade
    {
        Bronze,
        Silver,
        Gold,
    }

    public sealed record GradeBrand(Grade Value);

    [StaticDataRecord("Single", "Graded")]
    public sealed partial record GradedRecord(int Id, GradeBrand Grade);

    [Fact]
    public void MapSingleParameterRecord_WithEnumType()
    {
        var logger = CreateLogger();

        var csv = "Id,Grade\n1,Gold";

        var record = Assert.Single(CsvLoader.Parse(csv, GradedRecord.MapFromCsvRow));

        Assert.Equal(Grade.Gold, record.Grade.Value);
        logger.LogInformation("Enum branding mapped: Grade={Grade}", record.Grade.Value);
    }

    public sealed record MultiParamRecord(int X, int Y);

    [StaticDataRecord("Single", "Container")]
    public sealed partial record ContainerWithMultiParamRecord(MultiParamRecord Point, string Label);

    [Fact]
    public void MapMultiParameterRecord_StillUsesNestedHeaders()
    {
        var logger = CreateLogger();

        var csv = "Point.X,Point.Y,Label\n10,20,Origin";

        var record = Assert.Single(CsvLoader.Parse(csv, ContainerWithMultiParamRecord.MapFromCsvRow));

        Assert.Equal(10, record.Point.X);
        Assert.Equal(20, record.Point.Y);
        Assert.Equal("Origin", record.Label);
        logger.LogInformation("Container mapped: Point={Point}, Label={Label}", record.Point, record.Label);
    }

    [Fact]
    public void MapSingleParameterRecord_BackwardCompatibility_WithValueSuffix()
    {
        var logger = CreateLogger();

        var csv = "Id.Value,Name\n99,Legacy Entity";

        var record = Assert.Single(CsvLoader.Parse(csv, EntityRecord.MapFromCsvRow));

        Assert.Equal(99, record.Id.Value);
        Assert.Equal("Legacy Entity", record.Name);
        logger.LogInformation("Legacy header mapped: Id={Id}, Name={Name}", record.Id.Value, record.Name);
    }

    private TestOutputLogger<SingleParameterMappingTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Information);
        if (factory.CreateLogger<SingleParameterMappingTests>()
            is not TestOutputLogger<SingleParameterMappingTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
