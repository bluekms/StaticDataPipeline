using System.Collections.Immutable;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Sdp.Attributes;
using Sdp.Csv;
using Sdp.Manager;
using Sdp.Table;
using UnitTest.Utility;

namespace UnitTest.StaticDataTests;

public partial class KeyIndexGenerationTests
{
    private const string ItemsCsv =
        """
        Id,Name,IsHidden
        1,Sword,false
        2,Shield,true
        """;

    [StaticDataRecord("Items", "Sheet1")]
    private partial record ItemRecord([Key] int Id, string Name, bool IsHidden);

    [StaticDataRecord("Branded", "Sheet1")]
    private partial record BrandedRecord([Key] BrandedRecord.BrandedId Id, string Name)
    {
        public record struct BrandedId(int Value);
    }

    private sealed partial record Slot(int Day, int Period);

    [StaticDataRecord("Timetable", "Sheet1")]
    private partial record TimetableRecord([Key] Slot Slot, string Subject);

    [StaticDataRecord("Keyless", "Sheet1")]
    private partial record KeylessRecord(int Id, string Name);

    private sealed partial class ItemTable(ImmutableArray<ItemRecord> records)
        : StaticDataTable<ItemRecord>(records);

    private sealed partial class VisibleItemTable(ImmutableArray<ItemRecord> records)
        : StaticDataTable<ItemRecord>(records.Where(x => !x.IsHidden).ToImmutableArray());

    private sealed partial class HiddenItemTable(ImmutableArray<ItemRecord> records)
        : StaticDataTable<ItemRecord>(records.Where(x => x.IsHidden).ToImmutableArray());

    private sealed partial class BrandedTable(ImmutableArray<BrandedRecord> records)
        : StaticDataTable<BrandedRecord>(records);

    private sealed partial class TimetableTable(ImmutableArray<TimetableRecord> records)
        : StaticDataTable<TimetableRecord>(records);

    private sealed partial class KeylessTable(ImmutableArray<KeylessRecord> records)
        : StaticDataTable<KeylessRecord>(records);

    [NoKeyIndex]
    private sealed partial class ItemByNameTable(ImmutableArray<ItemRecord> records)
        : StaticDataTable<ItemRecord>(records);

    private sealed partial class ItemWithOwnGetTable(ImmutableArray<ItemRecord> records)
        : StaticDataTable<ItemRecord>(records)
    {
        public ItemRecord Get(int id)
        {
            return Records.Last(x => x.Id <= id);
        }
    }

    private sealed partial class SplitStaticData(ILogger logger)
        : StaticDataManager<SplitStaticData.TableSet>(logger)
    {
        public sealed partial record TableSet(VisibleItemTable? Visible, HiddenItemTable? Hidden);
    }

    [Fact]
    public void TableOfKeyedRecord_FindsRowsByKey()
    {
        var table = new ItemTable(Parse(ItemsCsv, ItemRecord.MapFromCsvRow));

        Assert.Equal("Shield", table.Get(2).Name);
        Assert.True(table.TryGet(1, out var sword));
        Assert.Equal("Sword", sword.Name);
        Assert.False(table.TryGet(3, out _));
        Assert.Throws<KeyNotFoundException>(() => table.Get(3));
    }

    [Fact]
    public void TableThatFiltersRows_IndexesOnlyTheRowsItKeeps()
    {
        var table = new VisibleItemTable(Parse(ItemsCsv, ItemRecord.MapFromCsvRow));

        Assert.True(table.TryGet(1, out _));
        Assert.False(table.TryGet(2, out _));
    }

    [Fact]
    public void BrandingKey_IsTheLookupType()
    {
        var table = new BrandedTable(Parse("Id,Name\n7,Seven", BrandedRecord.MapFromCsvRow));

        Assert.Equal("Seven", table.Get(new BrandedRecord.BrandedId(7)).Name);
    }

    [Fact]
    public void NestedRecordKey_IsTheLookupType()
    {
        var table = new TimetableTable(Parse("Slot.Day,Slot.Period,Subject\n1,2,Math", TimetableRecord.MapFromCsvRow));

        Assert.Equal("Math", table.Get(new Slot(1, 2)).Subject);
    }

    [Fact]
    public void UserDeclaredGet_IsKept_AndTryGetIsStillGenerated()
    {
        var table = new ItemWithOwnGetTable(Parse(ItemsCsv, ItemRecord.MapFromCsvRow));

        Assert.Equal("Shield", table.Get(99).Name);
        Assert.False(table.TryGet(99, out _));
    }

    [Theory]
    [InlineData(typeof(KeylessTable))]
    [InlineData(typeof(ItemByNameTable))]
    public void KeylessOrOptedOutTable_HasNoGeneratedLookup(Type tableType)
    {
        const BindingFlags declaredPublic = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;

        Assert.Null(tableType.GetMethod("Get", declaredPublic));
        Assert.Null(tableType.GetMethod("TryGet", declaredPublic));
    }

    [Fact]
    public async Task TablesSharingOneRecord_ReportTheSameCsvErrorOnce()
    {
        using var dir = new CsvTestDirectory();
        dir.Write("Items.Sheet1.csv", "Id,Name,IsHidden\n1,Sword,false\n1,Bow,true");
        var staticData = new SplitStaticData(NullLogger.Instance);

        var exception = await Assert.ThrowsAsync<AggregateException>(() => staticData.LoadAsync(dir.Path));

        Assert.Single(exception.InnerExceptions);
    }

    private static ImmutableArray<T> Parse<T>(string csv, Func<CsvHeaderIndex, string[], T> mapper)
    {
        return CsvLoader.Parse(csv, mapper);
    }
}
