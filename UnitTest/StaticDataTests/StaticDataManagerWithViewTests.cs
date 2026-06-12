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

public partial class StaticDataManagerWithViewTests(ITestOutputHelper testOutputHelper)
{
    [StaticDataRecord("Catalog", "Events")]
    private sealed partial record EventRecord([Key] int Id, string Name);

    [StaticDataRecord("Catalog", "Weapons")]
    private sealed partial record WeaponRecord(
        [Key] int Id,
        [ForeignKey("Events", "Id")] int EventId,
        string Name,
        int AttackPower);

    [StaticDataRecord("Catalog", "Armors")]
    private sealed partial record ArmorRecord(
        [Key] int Id,
        [ForeignKey("Events", "Id")] int EventId,
        string Name,
        int DefensePower);

    private enum EquipmentType
    {
        Weapon,
        Armor,
    }

    private sealed partial record Equipment(
        int Id,
        EquipmentType Type,
        string Name,
        int AttackPower,
        int DefensePower);

    private sealed partial class EventTable(ImmutableArray<EventRecord> records)
        : StaticDataTable<EventTable, EventRecord>(records);

    private sealed partial class WeaponTable(ImmutableArray<WeaponRecord> records)
        : StaticDataTable<WeaponTable, WeaponRecord>(records);

    private sealed partial class ArmorTable(ImmutableArray<ArmorRecord> records)
        : StaticDataTable<ArmorTable, ArmorRecord>(records);

    private sealed partial record EventBundle(
        EventRecord Event,
        IReadOnlyList<Equipment> Equipment);

    private sealed partial class EventBundleView(GameStaticData.TableSet tables)
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
                var equipment = new List<Equipment>();

                foreach (var weapon in weaponsByEvent.Get(ev.Id))
                {
                    equipment.Add(new Equipment(
                        weapon.Id,
                        EquipmentType.Weapon,
                        weapon.Name,
                        weapon.AttackPower,
                        0));
                }

                foreach (var armor in armorsByEvent.Get(ev.Id))
                {
                    equipment.Add(new Equipment(
                        armor.Id,
                        EquipmentType.Armor,
                        armor.Name,
                        0,
                        armor.DefensePower));
                }

                bundles.Add(new EventBundle(ev, equipment));
            }

            return new UniqueIndex<EventBundle, int>(bundles, b => b.Event.Id);
        }
    }

    private sealed partial record EventAttackTotal(int EventId, int TotalAttackPower);

    private sealed partial class EventAttackTotalView(GameStaticData.TableSet tables)
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

    private sealed partial class GameStaticData(ILogger logger)
        : StaticDataManager<GameStaticData.TableSet, GameStaticData.ViewSet>(logger)
    {
        public sealed partial record TableSet(
            EventTable? Events,
            WeaponTable? Weapons,
            ArmorTable? Armors);

        public sealed partial record ViewSet(
            EventBundleView EventBundles,
            EventAttackTotalView EventAttackTotals);
    }

    private sealed partial class BlockingGameStaticData(
        ILogger logger,
        ManualResetEventSlim started,
        ManualResetEventSlim gate)
        : StaticDataManager<GameStaticData.TableSet, GameStaticData.ViewSet>(logger)
    {
        protected override void Validate(GameStaticData.TableSet tableSet)
        {
            started.Set();
            gate.Wait(TimeSpan.FromSeconds(10));
        }
    }

    private sealed partial class FailingViewA : StaticDataView<FailingViewA, FailingViewManager.TableSet>
    {
        public FailingViewA(FailingViewManager.TableSet tables)
            : base(tables)
        {
            throw new InvalidOperationException("failure A");
        }
    }

    private sealed partial class FailingViewB : StaticDataView<FailingViewB, FailingViewManager.TableSet>
    {
        public FailingViewB(FailingViewManager.TableSet tables)
            : base(tables)
        {
            throw new InvalidOperationException("failure B");
        }
    }

    private sealed partial class FailingViewManager(ILogger logger)
        : StaticDataManager<FailingViewManager.TableSet, FailingViewManager.ViewSet>(logger)
    {
        public sealed partial record TableSet(
            EventTable? Events,
            WeaponTable? Weapons,
            ArmorTable? Armors);

        public sealed partial record ViewSet(
            FailingViewA A,
            FailingViewB B);
    }

    private sealed partial class ValidateThrowingViewA(ValidateThrowingViewManager.TableSet tables)
        : StaticDataView<ValidateThrowingViewA, ValidateThrowingViewManager.TableSet>(tables)
    {
        protected override void Validate()
        {
            throw new InvalidOperationException("validate failure A");
        }
    }

    private sealed partial class ValidateThrowingViewB(ValidateThrowingViewManager.TableSet tables)
        : StaticDataView<ValidateThrowingViewB, ValidateThrowingViewManager.TableSet>(tables)
    {
        protected override void Validate()
        {
            throw new InvalidOperationException("validate failure B");
        }
    }

    private sealed partial class ValidateThrowingViewManager(ILogger logger)
        : StaticDataManager<ValidateThrowingViewManager.TableSet, ValidateThrowingViewManager.ViewSet>(logger)
    {
        public sealed partial record TableSet(
            EventTable? Events,
            WeaponTable? Weapons,
            ArmorTable? Armors);

        public sealed partial record ViewSet(
            ValidateThrowingViewA A,
            ValidateThrowingViewB B);
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

        var currentStaticData = manager.Current;

        var bundle = currentStaticData.Views.EventBundles.Get(1);
        Assert.Equal("WinterFest", bundle.Event.Name);
        Assert.Equal(3, bundle.Equipment.Count);
        Assert.Equal(2, bundle.Equipment.Count(e => e.Type == EquipmentType.Weapon));
        Assert.Single(bundle.Equipment, e => e.Type == EquipmentType.Armor);

        var bundle2 = currentStaticData.Views.EventBundles.Get(2);
        var only = Assert.Single(bundle2.Equipment);
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

        var currentStaticData = manager.Current;

        Assert.Equal("WinterFest", currentStaticData.Views.EventBundles.Get(1).Event.Name);

        var total1 = currentStaticData.Views.EventAttackTotals.Get(1);
        Assert.Equal(55, total1.TotalAttackPower);

        var total2 = currentStaticData.Views.EventAttackTotals.Get(2);
        Assert.Equal(40, total2.TotalAttackPower);

        Assert.Throws<KeyNotFoundException>(() => currentStaticData.Views.EventAttackTotals.Get(99));

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

    [Fact]
    public async Task LoadAsync_AggregatesViewValidateFailures()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<StaticDataManagerWithViewTests>() is not TestOutputLogger<StaticDataManagerWithViewTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        using var dir = new CsvTestDirectory();
        WriteSampleCsvs(dir);

        var manager = new ValidateThrowingViewManager(logger);

        var ex = await Assert.ThrowsAsync<AggregateException>(
            () => manager.LoadAsync(dir.Path));
        Assert.Equal(2, ex.InnerExceptions.Count);
        Assert.Contains(ex.InnerExceptions, e => e.Message == "validate failure A");
        Assert.Contains(ex.InnerExceptions, e => e.Message == "validate failure B");
        Assert.Empty(logger.Logs);
    }

    [Theory]
    [InlineData("en", "LoadAsync is already in progress; concurrent loads are not supported.")]
    [InlineData("ko", "LoadAsync가 이미 실행 중입니다. 동시 로드는 지원하지 않습니다.")]
    public async Task LoadAsync_Reentrant_ThrowsLocalizedMessage(string locale, string expected)
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

            using var started = new ManualResetEventSlim();
            using var gate = new ManualResetEventSlim();
            var manager = new BlockingGameStaticData(logger, started, gate);

            var first = Task.Run(() => manager.LoadAsync(dir.Path));
            Assert.True(started.Wait(TimeSpan.FromSeconds(5)));

            // 단언이 실패해도 gate 해제와 첫 로드 수습을 보장한다. 그렇지 않으면 using 해제로
            // dispose 된 gate 를 배경 태스크가 계속 대기하고 first 가 unobserved 로 남는다.
            try
            {
                var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                    () => manager.LoadAsync(dir.Path));
                Assert.Equal(expected, ex.Message);
            }
            finally
            {
                gate.Set();
                await first;
            }

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

        // 단언 실패로 Cancel/Join 없이 unwind 되어 cts 가 dispose 되어도 배경 스레드가
        // ObjectDisposedException 으로 죽지 않도록, dispose 와 무관한 토큰을 미리 캡처한다.
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        var loaderThread = new Thread(() =>
        {
            var toggle = false;
            while (!token.IsCancellationRequested)
            {
                manager.LoadAsync(toggle ? dirB.Path : dirA.Path).GetAwaiter().GetResult();
                toggle = !toggle;
            }
        });
        loaderThread.IsBackground = true;
        loaderThread.Start();

        for (var i = 0; i < 5000; i++)
        {
            var currentStaticData = manager.Current;
            var name = currentStaticData.Views.EventBundles.Get(1).Event.Name;
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
