using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;
using Sdp.Attributes;
using Sdp.Manager;
using Sdp.Table;
using Sdp.View;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.StaticDataTests;

public class StaticDataManagerWithViewTests(ITestOutputHelper testOutputHelper)
{
    [StaticDataRecord("Catalog", "Events")]
    private sealed record EventRecord([Key] int Id, string Name);

    [StaticDataRecord("Catalog", "Weapons")]
    private sealed record WeaponRecord(
        [Key] int Id,
        [ForeignKey("Events", "Id")] int EventId,
        string Name,
        int AttackPower);

    [StaticDataRecord("Catalog", "Armors")]
    private sealed record ArmorRecord(
        [Key] int Id,
        [ForeignKey("Events", "Id")] int EventId,
        string Name,
        int DefensePower);

    private enum EquipmentType
    {
        Weapon,
        Armor,
    }

    private sealed record Equipment(
        int Id,
        EquipmentType Type,
        string Name,
        int AttackPower,
        int DefensePower);

    private sealed class EventTable(ImmutableList<EventRecord> records)
        : StaticDataTable<EventTable, EventRecord>(records);

    private sealed class WeaponTable(ImmutableList<WeaponRecord> records)
        : StaticDataTable<WeaponTable, WeaponRecord>(records);

    private sealed class ArmorTable(ImmutableList<ArmorRecord> records)
        : StaticDataTable<ArmorTable, ArmorRecord>(records);

    private sealed record EventBundle(
        EventRecord Event,
        IReadOnlyList<Equipment> Equipments);

    private sealed class EventBundleView(GameStaticData.TableSet tables)
        : StaticDataView<EventBundleView, GameStaticData.TableSet>(tables)
    {
        private readonly UniqueIndex<EventBundle, int> byEventId = Build(tables);

        public EventBundle Get(int eventId)
            => byEventId.Get(eventId);

        private static UniqueIndex<EventBundle, int> Build(GameStaticData.TableSet t)
        {
            var weaponsByEvent = new MultiIndex<WeaponRecord, int>(t.Weapons!.Records, x => x.EventId);
            var armorsByEvent = new MultiIndex<ArmorRecord, int>(t.Armors!.Records, x => x.EventId);

            var bundles = new List<EventBundle>();
            foreach (var ev in t.Events!.Records)
            {
                var equipments = new List<Equipment>();

                foreach (var weapon in weaponsByEvent.Get(ev.Id))
                {
                    equipments.Add(new Equipment(
                        weapon.Id,
                        EquipmentType.Weapon,
                        weapon.Name,
                        weapon.AttackPower,
                        0));
                }

                foreach (var armor in armorsByEvent.Get(ev.Id))
                {
                    equipments.Add(new Equipment(
                        armor.Id,
                        EquipmentType.Armor,
                        armor.Name,
                        0,
                        armor.DefensePower));
                }

                bundles.Add(new EventBundle(ev, equipments));
            }

            return new UniqueIndex<EventBundle, int>(bundles, b => b.Event.Id);
        }
    }

    private sealed record EventAttackTotal(int EventId, int TotalAttackPower);

    private sealed class EventAttackTotalView(GameStaticData.TableSet tables)
        : StaticDataView<EventAttackTotalView, GameStaticData.TableSet>(tables)
    {
        private readonly UniqueIndex<EventAttackTotal, int> byEventId = Build(tables);

        public EventAttackTotal Get(int eventId)
            => byEventId.Get(eventId);

        private static UniqueIndex<EventAttackTotal, int> Build(GameStaticData.TableSet t)
        {
            var weaponsByEvent = new MultiIndex<WeaponRecord, int>(t.Weapons!.Records, w => w.EventId);

            var totals = new List<EventAttackTotal>();
            foreach (var ev in t.Events!.Records)
            {
                var total = 0;
                foreach (var weapon in weaponsByEvent.Get(ev.Id))
                {
                    total += weapon.AttackPower;
                }

                totals.Add(new EventAttackTotal(ev.Id, total));
            }

            return new UniqueIndex<EventAttackTotal, int>(totals, x => x.EventId);
        }
    }

    private sealed class GameStaticData(ILogger logger)
        : StaticDataManager<GameStaticData.TableSet, GameStaticData.ViewSet>(logger)
    {
        public sealed record TableSet(
            EventTable? Events,
            WeaponTable? Weapons,
            ArmorTable? Armors);

        public sealed record ViewSet(
            EventBundleView? EventBundles,
            EventAttackTotalView? EventAttackTotals);

        public EventBundleView EventBundles => CurrentViews.EventBundles!;

        public EventAttackTotalView EventAttackTotals => CurrentViews.EventAttackTotals!;
    }

    private sealed class FailingViewA : StaticDataView<FailingViewA, FailingViewManager.TableSet>
    {
        public FailingViewA(FailingViewManager.TableSet tables)
            : base(tables)
        {
            throw new InvalidOperationException("failure A");
        }
    }

    private sealed class FailingViewB : StaticDataView<FailingViewB, FailingViewManager.TableSet>
    {
        public FailingViewB(FailingViewManager.TableSet tables)
            : base(tables)
        {
            throw new InvalidOperationException("failure B");
        }
    }

    private sealed class FailingViewManager(ILogger logger)
        : StaticDataManager<FailingViewManager.TableSet, FailingViewManager.ViewSet>(logger)
    {
        public sealed record TableSet(
            EventTable? Events,
            WeaponTable? Weapons,
            ArmorTable? Armors);

        public sealed record ViewSet(
            FailingViewA? A,
            FailingViewB? B);
    }

    private sealed class InvalidParameterManager(ILogger logger)
        : StaticDataManager<InvalidParameterManager.TableSet, InvalidParameterManager.ViewSet>(logger)
    {
        public sealed record TableSet(
            EventTable? Events,
            WeaponTable? Weapons,
            ArmorTable? Armors);

        public sealed record ViewSet(string? NotAView);
    }

    private sealed class BadCtorView : StaticDataView<BadCtorView, BadCtorViewManager.TableSet>
    {
        public BadCtorView(BadCtorViewManager.TableSet tables, int unused)
            : base(tables)
        {
        }
    }

    private sealed class BadCtorViewManager(ILogger logger)
        : StaticDataManager<BadCtorViewManager.TableSet, BadCtorViewManager.ViewSet>(logger)
    {
        public sealed record TableSet(
            EventTable? Events,
            WeaponTable? Weapons,
            ArmorTable? Armors);

        public sealed record ViewSet(BadCtorView? Bad);
    }

    [Fact]
    public async Task LoadAsync_BuildsViewFromTables()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<StaticDataManagerWithViewTests>() is not TestOutputLogger<StaticDataManagerWithViewTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        using var dir = new CsvTestDirectory();
        WriteSampleCsvs(dir);

        var manager = new GameStaticData(logger);
        await manager.LoadAsync(dir.Path);

        var bundle = manager.EventBundles.Get(1);
        Assert.Equal("WinterFest", bundle.Event.Name);
        Assert.Equal(3, bundle.Equipments.Count);
        Assert.Equal(2, bundle.Equipments.Count(e => e.Type == EquipmentType.Weapon));
        Assert.Single(bundle.Equipments, e => e.Type == EquipmentType.Armor);

        var bundle2 = manager.EventBundles.Get(2);
        var only = Assert.Single(bundle2.Equipments);
        Assert.Equal(EquipmentType.Weapon, only.Type);

        Assert.Empty(logger.Logs);
    }

    [Fact]
    public async Task LoadAsync_BuildsMultipleViewsInOneSet()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<StaticDataManagerWithViewTests>() is not TestOutputLogger<StaticDataManagerWithViewTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        using var dir = new CsvTestDirectory();
        WriteSampleCsvs(dir);

        var manager = new GameStaticData(logger);
        await manager.LoadAsync(dir.Path);

        Assert.Equal("WinterFest", manager.EventBundles.Get(1).Event.Name);

        var total1 = manager.EventAttackTotals.Get(1);
        Assert.Equal(55, total1.TotalAttackPower);

        var total2 = manager.EventAttackTotals.Get(2);
        Assert.Equal(40, total2.TotalAttackPower);

        Assert.Throws<KeyNotFoundException>(() => manager.EventAttackTotals.Get(99));

        Assert.Empty(logger.Logs);
    }

    [Fact]
    public async Task LoadAsync_AggregatesViewBuildFailures()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<StaticDataManagerWithViewTests>() is not TestOutputLogger<StaticDataManagerWithViewTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        using var dir = new CsvTestDirectory();
        WriteSampleCsvs(dir);

        var manager = new FailingViewManager(logger);

        var ex = await Assert.ThrowsAsync<AggregateException>(
            () => manager.LoadAsync(dir.Path));
        Assert.Equal(2, ex.InnerExceptions.Count);
        Assert.Contains(ex.InnerExceptions, e => e.Message == "failure A");
        Assert.Contains(ex.InnerExceptions, e => e.Message == "failure B");
        Assert.Empty(logger.Logs);
    }

    [Theory]
    [InlineData("en", "Parameter 'NotAView' of type 'String' is not a StaticDataView<,> subtype.")]
    [InlineData("ko", "'NotAView' 파라미터의 타입 'String'이(가) StaticDataView<,> 서브타입이 아닙니다.")]
    public async Task LoadAsync_NonViewParameter_ThrowsLocalizedMessage(string locale, string expected)
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<StaticDataManagerWithViewTests>() is not TestOutputLogger<StaticDataManagerWithViewTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        var savedCulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(locale);
        try
        {
            using var dir = new CsvTestDirectory();
            WriteSampleCsvs(dir);

            var manager = new InvalidParameterManager(logger);

            var ex = await Assert.ThrowsAsync<AggregateException>(
                () => manager.LoadAsync(dir.Path));
            var inner = Assert.Single(ex.InnerExceptions);
            Assert.Equal(expected, inner.Message);
            Assert.Empty(logger.Logs);
        }
        finally
        {
            CultureInfo.CurrentUICulture = savedCulture;
        }
    }

    [Theory]
    [InlineData("en", "BadCtorView must have a constructor accepting TableSet.")]
    [InlineData("ko", "BadCtorView에는 TableSet을(를) 받는 생성자가 필요합니다.")]
    public async Task LoadAsync_ViewMissingTableSetConstructor_ThrowsLocalizedMessage(string locale, string expected)
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<StaticDataManagerWithViewTests>() is not TestOutputLogger<StaticDataManagerWithViewTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        var savedCulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(locale);
        try
        {
            using var dir = new CsvTestDirectory();
            WriteSampleCsvs(dir);

            var manager = new BadCtorViewManager(logger);

            var ex = await Assert.ThrowsAsync<AggregateException>(
                () => manager.LoadAsync(dir.Path));
            var inner = Assert.Single(ex.InnerExceptions);
            Assert.Equal(expected, inner.Message);
            Assert.Empty(logger.Logs);
        }
        finally
        {
            CultureInfo.CurrentUICulture = savedCulture;
        }
    }

    [Fact]
    public async Task ConcurrentLoadAndRead_AlwaysSeesCompleteView()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<StaticDataManagerWithViewTests>() is not TestOutputLogger<StaticDataManagerWithViewTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        using var dirA = new CsvTestDirectory();
        using var dirB = new CsvTestDirectory();
        WriteSampleCsvs(dirA);
        WriteSampleCsvs(dirB, eventNameSuffix: "_B");

        var manager = new GameStaticData(logger);
        await manager.LoadAsync(dirA.Path);

        var cts = new CancellationTokenSource();

        var loaderThread = new Thread(() =>
        {
            var toggle = false;
            while (!cts.Token.IsCancellationRequested)
            {
                manager.LoadAsync(toggle ? dirB.Path : dirA.Path).GetAwaiter().GetResult();
                toggle = !toggle;
            }
        });
        loaderThread.IsBackground = true;
        loaderThread.Start();

        for (var i = 0; i < 5000; i++)
        {
            var name = manager.EventBundles.Get(1).Event.Name;
            Assert.True(
                name == "WinterFest" || name == "WinterFest_B",
                FormattableString.Invariant($"Unexpected: {name}"));
        }

        cts.Cancel();
        loaderThread.Join();

        Assert.Empty(logger.Logs);
    }

    private static void WriteSampleCsvs(CsvTestDirectory dir, string eventNameSuffix = "")
    {
        var events = new StringBuilder();
        events.AppendLine("Id,Name");
        events.AppendLine(FormattableString.Invariant($"1,WinterFest{eventNameSuffix}"));
        events.AppendLine(FormattableString.Invariant($"2,SummerFest{eventNameSuffix}"));

        var weapons = new StringBuilder();
        weapons.AppendLine("Id,EventId,Name,AttackPower");
        weapons.AppendLine("1,1,IceSword,30");
        weapons.AppendLine("2,1,FrostBow,25");
        weapons.AppendLine("3,2,SunBlade,40");

        var armors = new StringBuilder();
        armors.AppendLine("Id,EventId,Name,DefensePower");
        armors.AppendLine("1,1,WinterCoat,15");

        dir.Write("Catalog.Events.csv", events.ToString());
        dir.Write("Catalog.Weapons.csv", weapons.ToString());
        dir.Write("Catalog.Armors.csv", armors.ToString());
    }
}
