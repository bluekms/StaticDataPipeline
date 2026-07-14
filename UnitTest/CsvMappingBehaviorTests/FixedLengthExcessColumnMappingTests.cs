using System.Collections.Frozen;
using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using Sdp.Attributes;
using Sdp.Csv;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.CsvMappingBehaviorTests;

public partial class FixedLengthExcessColumnMappingTests(ITestOutputHelper testOutputHelper)
{
    [StaticDataRecord("ExcessColumn", "Array")]
    public sealed partial record FixedArrayRecord(int Id, [Length(2)] ImmutableArray<int> Ids);

    [Fact]
    public void FixedArray_ExactColumns_Map()
    {
        var logger = CreateLogger();

        var csv = "Id,Ids[0],Ids[1]\n1,10,20";

        var record = Assert.Single(CsvLoader.Parse(csv, FixedArrayRecord.MapFromCsvRow));

        Assert.Equal(2, record.Ids.Length);
        Assert.Equal(10, record.Ids[0]);
        Assert.Equal(20, record.Ids[1]);
        logger.LogInformation("Exact columns mapped: [{V0}, {V1}]", record.Ids[0], record.Ids[1]);
    }

    [Fact]
    public void FixedArray_ExcessIndexedColumn_Throws()
    {
        var logger = CreateLogger();

        var csv = "Id,Ids[0],Ids[1],Ids[2]\n1,10,20,30";

        var ex = Assert.Throws<InvalidOperationException>(() => CsvLoader.Parse(csv, FixedArrayRecord.MapFromCsvRow));

        Assert.IsType<ArgumentException>(ex.InnerException);
        logger.LogInformation("Excess array column threw: {Message}", ex.Message);
    }

    [Fact]
    public void FixedArray_SimilarNamedColumn_Ignored()
    {
        var logger = CreateLogger();

        var csv = "Id,Ids[0],Ids[1],IdsBackup[5]\n1,10,20,99";

        var record = Assert.Single(CsvLoader.Parse(csv, FixedArrayRecord.MapFromCsvRow));

        Assert.Equal(2, record.Ids.Length);
        logger.LogInformation("Similar-named column ignored, {Count} elements mapped", record.Ids.Length);
    }

    [StaticDataRecord("ExcessColumn", "Set")]
    public sealed partial record FixedSetRecord(int Id, [Length(2)] FrozenSet<int> Ids);

    [Fact]
    public void FixedSet_ExcessIndexedColumn_Throws()
    {
        var logger = CreateLogger();

        var csv = "Id,Ids[0],Ids[1],Ids[2]\n1,10,20,30";

        var ex = Assert.Throws<InvalidOperationException>(() => CsvLoader.Parse(csv, FixedSetRecord.MapFromCsvRow));

        Assert.IsType<ArgumentException>(ex.InnerException);
        logger.LogInformation("Excess set column threw: {Message}", ex.Message);
    }

    public sealed record ItemValue([Key] int Key, string Name);

    [StaticDataRecord("ExcessColumn", "Dict")]
    public sealed partial record FixedDictRecord(int Id, [Length(2)] FrozenDictionary<int, ItemValue> Items);

    [Fact]
    public void FixedDict_ExcessIndexedGroup_Throws()
    {
        var logger = CreateLogger();

        var csv = "Id,Items[0].Key,Items[0].Name,Items[1].Key,Items[1].Name,Items[2].Key,Items[2].Name\n"
                + "1,1,A,2,B,3,C";

        var ex = Assert.Throws<InvalidOperationException>(() => CsvLoader.Parse(csv, FixedDictRecord.MapFromCsvRow));

        Assert.IsType<ArgumentException>(ex.InnerException);
        logger.LogInformation("Excess dictionary group threw: {Message}", ex.Message);
    }

    private TestOutputLogger<FixedLengthExcessColumnMappingTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Information);
        if (factory.CreateLogger<FixedLengthExcessColumnMappingTests>()
            is not TestOutputLogger<FixedLengthExcessColumnMappingTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
