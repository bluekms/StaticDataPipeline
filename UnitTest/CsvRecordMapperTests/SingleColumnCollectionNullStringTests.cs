using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using Sdp.Attributes;
using Sdp.Manager;
using Sdp.Table;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.CsvRecordMapperTests;

public class SingleColumnCollectionNullStringTests(ITestOutputHelper testOutputHelper)
{
    [StaticDataRecord("ScNs", "Items")]
    private sealed record DashRecord(
        int Id,
        [SingleColumnCollection(",")]
        [CountRange(1, 5)]
        [NullString("-")]
        ImmutableArray<int?> Values);

    [StaticDataRecord("ScNs", "Items")]
    private sealed record EmptyRecord(
        int Id,
        [SingleColumnCollection(",")]
        [CountRange(1, 5)]
        [NullString("")]
        ImmutableArray<int?> Values);

    private sealed class DashTable(ImmutableList<DashRecord> records)
        : StaticDataTable<DashTable, DashRecord>(records);

    private sealed class EmptyTable(ImmutableList<EmptyRecord> records)
        : StaticDataTable<EmptyTable, EmptyRecord>(records);

    private sealed class DashManager(ILogger logger)
        : StaticDataManager<DashManager.TableSet>(logger)
    {
        public sealed record TableSet(DashTable? Items);
    }

    private sealed class EmptyManager(ILogger logger)
        : StaticDataManager<EmptyManager.TableSet>(logger)
    {
        public sealed record TableSet(EmptyTable? Items);
    }

    [Fact]
    public async Task NullStringDash_DashCell_LoadsAsNullElement()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<SingleColumnCollectionNullStringTests>() is not TestOutputLogger<SingleColumnCollectionNullStringTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        using var dir = new CsvTestDirectory();
        dir.Write("ScNs.Items.csv", "Id,Values\n1,-\n");

        var manager = new DashManager(logger);
        await manager.LoadAsync(dir.Path);

        var record = manager.Current.Items!.Records[0];
        var value = Assert.Single(record.Values);
        Assert.Null(value);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public async Task NullStringDash_MixedCell_LoadsWithNullInMiddle()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<SingleColumnCollectionNullStringTests>() is not TestOutputLogger<SingleColumnCollectionNullStringTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        using var dir = new CsvTestDirectory();
        dir.Write("ScNs.Items.csv", "Id,Values\n1,\"1,-,3\"\n");

        var manager = new DashManager(logger);
        await manager.LoadAsync(dir.Path);

        var record = manager.Current.Items!.Records[0];
        Assert.Equal(3, record.Values.Length);
        Assert.Equal(1, record.Values[0]);
        Assert.Null(record.Values[1]);
        Assert.Equal(3, record.Values[2]);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public async Task NullStringEmpty_EmptyCell_LoadsAsSingleNullElement()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<SingleColumnCollectionNullStringTests>() is not TestOutputLogger<SingleColumnCollectionNullStringTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        using var dir = new CsvTestDirectory();
        dir.Write("ScNs.Items.csv", "Id,Values\n1,\n");

        var manager = new EmptyManager(logger);
        await manager.LoadAsync(dir.Path);

        var record = manager.Current.Items!.Records[0];
        var value = Assert.Single(record.Values);
        Assert.Null(value);
        Assert.Empty(logger.Logs);
    }
}
