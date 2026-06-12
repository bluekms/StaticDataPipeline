using System.Collections.Frozen;
using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using Sdp.Attributes;
using Sdp.Csv;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.CsvMappingBehaviorTests;

public partial class CollectionEnumMappingTests(ITestOutputHelper testOutputHelper)
{
    public enum Color
    {
        Red,
        Green,
        Blue,
    }

    [StaticDataRecord("CollectionEnum", "FixedArray")]
    public sealed partial record FixedEnumArrayRecord(
        int Id,
        [Length(3)] ImmutableArray<Color> Colors);

    [Fact]
    public void FixedLengthArray_EnumElements_MapByNameAndNumber()
    {
        var logger = CreateLogger();

        var csv = "Id,Colors[0],Colors[1],Colors[2]\n1,Red,1,Blue";

        var record = Assert.Single(CsvLoader.Parse(csv, FixedEnumArrayRecord.MapFromCsvRow));

        Assert.Equal(3, record.Colors.Length);
        Assert.Equal(Color.Red, record.Colors[0]);
        Assert.Equal(Color.Green, record.Colors[1]);
        Assert.Equal(Color.Blue, record.Colors[2]);
        logger.LogInformation(
            "Colors mapped: [{C0}, {C1}, {C2}]",
            record.Colors[0],
            record.Colors[1],
            record.Colors[2]);
    }

    [Fact]
    public void FixedLengthArray_UndefinedEnumElement_Throws()
    {
        var logger = CreateLogger();

        var csv = "Id,Colors[0],Colors[1],Colors[2]\n1,Red,99,Blue";

        var ex = Assert.Throws<InvalidOperationException>(() => CsvLoader.Parse(csv, FixedEnumArrayRecord.MapFromCsvRow));

        Assert.IsType<ArgumentException>(ex.InnerException);
        logger.LogInformation("Undefined enum element threw: {Message}", ex.Message);
    }

    [StaticDataRecord("CollectionEnum", "SingleColumn")]
    public sealed partial record SingleColumnEnumRecord(
        int Id,
        [SingleColumnCollection(",")] ImmutableArray<Color> Colors);

    [Fact]
    public void SingleColumnArray_EnumElements_Map()
    {
        var logger = CreateLogger();

        var csv = "Id,Colors\n1,\"Red,Green,Blue\"";

        var record = Assert.Single(CsvLoader.Parse(csv, SingleColumnEnumRecord.MapFromCsvRow));

        Assert.Equal(3, record.Colors.Length);
        Assert.Equal(Color.Red, record.Colors[0]);
        Assert.Equal(Color.Green, record.Colors[1]);
        Assert.Equal(Color.Blue, record.Colors[2]);
        logger.LogInformation(
            "Colors mapped: [{C0}, {C1}, {C2}]",
            record.Colors[0],
            record.Colors[1],
            record.Colors[2]);
    }

    [StaticDataRecord("CollectionEnum", "Set")]
    public sealed partial record EnumSetRecord(
        int Id,
        [SingleColumnCollection(",")] FrozenSet<Color> Colors);

    [Fact]
    public void SingleColumnSet_EnumElements_Map()
    {
        var logger = CreateLogger();

        var csv = "Id,Colors\n1,\"Red,Blue\"";

        var record = Assert.Single(CsvLoader.Parse(csv, EnumSetRecord.MapFromCsvRow));

        Assert.Equal(2, record.Colors.Count);
        Assert.Contains(Color.Red, (IEnumerable<Color>)record.Colors);
        Assert.Contains(Color.Blue, (IEnumerable<Color>)record.Colors);
        logger.LogInformation("Color set mapped with {Count} elements", record.Colors.Count);
    }

    private TestOutputLogger<CollectionEnumMappingTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Information);
        if (factory.CreateLogger<CollectionEnumMappingTests>()
            is not TestOutputLogger<CollectionEnumMappingTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
