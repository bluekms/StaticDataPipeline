using System.Collections.Immutable;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Sdp.Attributes;
using Sdp.Manager;
using Sdp.Table;
using UnitTest.Utility;
using SdpMessages = Sdp.Resources.Messages;

namespace UnitTest.StaticDataTests;

public partial class KeyDuplicateOnLoadTests
{
    private const string DuplicatedIdCsv =
        """
        Id,Name
        1,Sword
        2,Shield
        1,Bow
        """;

    private const string HiddenDuplicateCsv =
        """
        Id,Name,IsHidden
        1,Sword,false
        1,Bow,true
        """;

    [StaticDataRecord("Keyed", "Sheet1")]
    private partial record KeyedRecord([Key] int Id, string Name);

    [StaticDataRecord("Branded", "Sheet1")]
    private partial record BrandedRecord([Key] BrandedRecord.BrandedId Id, string Name)
    {
        public record struct BrandedId(int Value);
    }

    [StaticDataRecord("Keyless", "Sheet1")]
    private partial record KeylessRecord(int Id, string Name);

    [StaticDataRecord("Filtered", "Sheet1")]
    private partial record FilteredRecord([Key] int Id, string Name, bool IsHidden);

    private sealed partial class KeyedTable(ImmutableArray<KeyedRecord> records)
        : StaticDataTable<KeyedRecord>(records);

    private sealed partial class BrandedTable(ImmutableArray<BrandedRecord> records)
        : StaticDataTable<BrandedRecord>(records);

    private sealed partial class KeylessTable(ImmutableArray<KeylessRecord> records)
        : StaticDataTable<KeylessRecord>(records);

    private sealed partial class VisibleTable(ImmutableArray<FilteredRecord> records)
        : StaticDataTable<FilteredRecord>(records.Where(x => !x.IsHidden).ToImmutableArray());

    private sealed partial class KeyedStaticData(ILogger logger)
        : StaticDataManager<KeyedStaticData.TableSet>(logger)
    {
        public sealed partial record TableSet(KeyedTable? Keyed);
    }

    private sealed partial class BrandedStaticData(ILogger logger)
        : StaticDataManager<BrandedStaticData.TableSet>(logger)
    {
        public sealed partial record TableSet(BrandedTable? Branded);
    }

    private sealed partial class KeylessStaticData(ILogger logger)
        : StaticDataManager<KeylessStaticData.TableSet>(logger)
    {
        public sealed partial record TableSet(KeylessTable? Keyless);
    }

    private sealed partial class VisibleStaticData(ILogger logger)
        : StaticDataManager<VisibleStaticData.TableSet>(logger)
    {
        public sealed partial record TableSet(VisibleTable? Visible);
    }

    private static string DuplicateKeyMessage(string fileName, string key, string recordName)
    {
        var format = SdpMessages.Composite.CsvDuplicateKey;
        return string.Format(CultureInfo.CurrentCulture, format, fileName, key, recordName);
    }

    [Fact]
    public async Task KeyedRecordWithoutIndex_DuplicateKey_FailsTableLoad()
    {
        using var dir = new CsvTestDirectory();
        dir.Write("Keyed.Sheet1.csv", DuplicatedIdCsv);
        var staticData = new KeyedStaticData(NullLogger.Instance);

        var exception = await Assert.ThrowsAsync<AggregateException>(() => staticData.LoadAsync(dir.Path));

        var inner = Assert.Single(exception.InnerExceptions);
        Assert.Equal(DuplicateKeyMessage("Keyed.Sheet1.csv", "1", nameof(KeyedRecord)), inner.Message);
    }

    [Fact]
    public async Task BrandingKey_DuplicateKey_MessageShowsInnerValue()
    {
        using var dir = new CsvTestDirectory();
        dir.Write("Branded.Sheet1.csv", DuplicatedIdCsv);
        var staticData = new BrandedStaticData(NullLogger.Instance);

        var exception = await Assert.ThrowsAsync<AggregateException>(() => staticData.LoadAsync(dir.Path));

        var inner = Assert.Single(exception.InnerExceptions);
        Assert.Equal(DuplicateKeyMessage("Branded.Sheet1.csv", "1", nameof(BrandedRecord)), inner.Message);
    }

    [Fact]
    public async Task KeylessRecord_SameValueTwice_Loads()
    {
        using var dir = new CsvTestDirectory();
        dir.Write("Keyless.Sheet1.csv", DuplicatedIdCsv);
        var staticData = new KeylessStaticData(NullLogger.Instance);

        await staticData.LoadAsync(dir.Path);

        Assert.Equal(3, staticData.Current.Keyless!.Records.Length);
    }

    [Fact]
    public async Task DuplicateAmongRowsTheTableFiltersOut_StillFails()
    {
        using var dir = new CsvTestDirectory();
        dir.Write("Filtered.Sheet1.csv", HiddenDuplicateCsv);
        var staticData = new VisibleStaticData(NullLogger.Instance);

        var exception = await Assert.ThrowsAsync<AggregateException>(() => staticData.LoadAsync(dir.Path));

        var inner = Assert.Single(exception.InnerExceptions);
        Assert.Equal(DuplicateKeyMessage("Filtered.Sheet1.csv", "1", nameof(FilteredRecord)), inner.Message);
    }
}
