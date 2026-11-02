using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using Sdp.Attributes;
using Sdp.Manager;
using Sdp.Table;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.ForeignKeyTests;

public partial class ForeignKeyPrimitiveColumnToBrandingKeyTests(ITestOutputHelper testOutputHelper)
{
    private TestOutputLogger<ForeignKeyPrimitiveColumnToBrandingKeyTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<ForeignKeyPrimitiveColumnToBrandingKeyTests>()
            is not TestOutputLogger<ForeignKeyPrimitiveColumnToBrandingKeyTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }

    private enum RewardKind
    {
        Item,
        Currency,
    }

    [StaticDataRecord("Item", "Sheet1")]
    private partial record ItemRecord([Key] ItemRecord.ItemId Id, string Name)
    {
        public readonly record struct ItemId(int Value);
    }

    [StaticDataRecord("Currency", "Sheet1")]
    private partial record CurrencyRecord([Key] CurrencyRecord.CurrencyId Id, string Name)
    {
        public readonly record struct CurrencyId(int Value);
    }

    [StaticDataRecord("Attendance", "Sheet1")]
    private partial record AttendanceRecord(
        byte Day,
        [ForeignKey("Item", "Id")]
        [ForeignKey("Currency", "Id")]
        int RewardId);

    [StaticDataRecord("Reward", "Sheet1")]
    private partial record RewardRecord(
        int Id,
        RewardKind Kind,
        [SwitchForeignKey(nameof(Kind), "Item", "Item", "Id")]
        [SwitchForeignKey(nameof(Kind), "Currency", "Currency", "Id")]
        int TargetId);

    [StaticDataRecord("Shop", "Sheet1")]
    private partial record ShopRecord(
        int Id,
        [ForeignKey("Item", "Id")] ItemRecord.ItemId ItemId);

    private sealed partial class ItemTable(ImmutableArray<ItemRecord> records)
        : StaticDataTable<ItemRecord>(records);

    private sealed partial class CurrencyTable(ImmutableArray<CurrencyRecord> records)
        : StaticDataTable<CurrencyRecord>(records);

    private sealed partial class AttendanceTable(ImmutableArray<AttendanceRecord> records)
        : StaticDataTable<AttendanceRecord>(records);

    private sealed partial class RewardTable(ImmutableArray<RewardRecord> records)
        : StaticDataTable<RewardRecord>(records);

    private sealed partial class ShopTable(ImmutableArray<ShopRecord> records)
        : StaticDataTable<ShopRecord>(records);

    private sealed partial class PrimitiveToBrandingStaticData(ILogger logger)
        : StaticDataManager<PrimitiveToBrandingStaticData.TableSet>(logger)
    {
        public sealed partial record TableSet(
            ItemTable? Item,
            CurrencyTable? Currency,
            AttendanceTable? Attendance,
            RewardTable? Reward,
            ShopTable? Shop);

        public AttendanceTable AttendanceTable => Current.Attendance!;

        public RewardTable RewardTable => Current.Reward!;

        public ShopTable ShopTable => Current.Shop!;
    }

    private const string ItemCsv =
        """
        Id,Name
        10001,Sword
        10002,Shield
        """;

    private const string CurrencyCsv =
        """
        Id,Name
        901,Gold
        902,Gem
        """;

    private const string ValidAttendanceCsv =
        """
        Day,RewardId
        1,901
        2,10001
        """;

    private const string MissingAttendanceCsv =
        """
        Day,RewardId
        1,901
        2,77777
        """;

    private const string ValidRewardCsv =
        """
        Id,Kind,TargetId
        1,Item,10001
        2,Currency,901
        """;

    private const string MismatchedRewardCsv =
        """
        Id,Kind,TargetId
        1,Item,901
        """;

    private const string ValidShopCsv =
        """
        Id,ItemId
        1,10001
        2,10002
        """;

    private static CsvTestDirectory CreateDirectory(string attendanceCsv, string rewardCsv)
    {
        var dir = new CsvTestDirectory();
        dir.Write("Item.Sheet1.csv", ItemCsv);
        dir.Write("Currency.Sheet1.csv", CurrencyCsv);
        dir.Write("Attendance.Sheet1.csv", attendanceCsv);
        dir.Write("Reward.Sheet1.csv", rewardCsv);
        dir.Write("Shop.Sheet1.csv", ValidShopCsv);

        return dir;
    }

    [Fact]
    public async Task Load_PrimitiveColumnWithTwoBrandingTargets_Succeeds()
    {
        var logger = CreateLogger();

        using var dir = CreateDirectory(ValidAttendanceCsv, ValidRewardCsv);

        var staticData = new PrimitiveToBrandingStaticData(logger);
        await staticData.LoadAsync(dir.Path);

        Assert.Equal(2, staticData.AttendanceTable.Records.Length);
        Assert.Equal(2, staticData.RewardTable.Records.Length);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public async Task Load_PrimitiveColumnValueMissingFromBothTargets_ThrowsAggregateException()
    {
        var logger = CreateLogger();

        using var dir = CreateDirectory(MissingAttendanceCsv, ValidRewardCsv);

        var staticData = new PrimitiveToBrandingStaticData(logger);

        var ex = await Assert.ThrowsAsync<AggregateException>(() => staticData.LoadAsync(dir.Path));

        var inner = Assert.Single(ex.InnerExceptions);
        Assert.Contains("RewardId", inner.Message, StringComparison.Ordinal);
        Assert.Contains("77777", inner.Message, StringComparison.Ordinal);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public async Task Load_PrimitiveSwitchColumnValueInOtherBranchTarget_ThrowsAggregateException()
    {
        var logger = CreateLogger();

        using var dir = CreateDirectory(ValidAttendanceCsv, MismatchedRewardCsv);

        var staticData = new PrimitiveToBrandingStaticData(logger);

        var ex = await Assert.ThrowsAsync<AggregateException>(() => staticData.LoadAsync(dir.Path));

        var inner = Assert.Single(ex.InnerExceptions);
        Assert.Contains("TargetId", inner.Message, StringComparison.Ordinal);
        Assert.Contains("901", inner.Message, StringComparison.Ordinal);
        Assert.Contains("Kind=", inner.Message, StringComparison.Ordinal);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public async Task Load_BrandingColumnToBrandingKey_Succeeds()
    {
        var logger = CreateLogger();

        using var dir = CreateDirectory(ValidAttendanceCsv, ValidRewardCsv);

        var staticData = new PrimitiveToBrandingStaticData(logger);
        await staticData.LoadAsync(dir.Path);

        var shop = staticData.ShopTable.Records.First(r => r.Id == 2);
        Assert.Equal(new ItemRecord.ItemId(10002), shop.ItemId);
        Assert.Empty(logger.Logs);
    }
}
