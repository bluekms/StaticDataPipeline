using System.Collections.Immutable;
using Sdp.Manager;
using Sdp.Table;

namespace UnitTest.StaticDataTests;

public class StaticDataManagerTests
{
    private sealed record FakeRecord(int Id);

    private sealed class FakeTable : StaticDataTable<FakeTable, FakeRecord, int>
    {
        public const string VersionA = "a";
        public const string VersionB = "b";
        public const int CountA = 3;
        public const int CountB = 7;

        private FakeTable(ImmutableList<FakeRecord> records)
            : base(records, r => r.Id)
        {
        }

        public static new async Task<FakeTable> CreateAsync(string version)
        {
            await Task.Delay(50);
            var count = version == VersionB ? CountB : CountA;
            var records = Enumerable.Range(1, count)
                .Select(i => new FakeRecord(i))
                .ToImmutableList();
            return new(records);
        }
    }

    private sealed class FakeManager : StaticDataManager<FakeManager.TableSet>
    {
        public sealed record TableSet(FakeTable? Items);

        public TableSet Tables => Current;
    }

    [Fact]
    public async Task ConcurrentLoadAndRead_AlwaysSeesCompleteDataset()
    {
        var manager = new FakeManager();
        await manager.LoadAsync(FakeTable.VersionA);

        var cts = new CancellationTokenSource();

        var loaderThread = new Thread(() =>
        {
            var toggle = false;
            while (!cts.Token.IsCancellationRequested)
            {
                manager.LoadAsync(toggle ? FakeTable.VersionB : FakeTable.VersionA).GetAwaiter().GetResult();
                toggle = !toggle;
            }
        });
        loaderThread.IsBackground = true;
        loaderThread.Start();

        for (var i = 0; i < 10000; i++)
        {
            var count = manager.Tables.Items?.Records.Count;
            Assert.True(
                count == FakeTable.CountA || count == FakeTable.CountB,
                FormattableString.Invariant($"예상: {FakeTable.CountA} 또는 {FakeTable.CountB}, 실제: {count}"));
        }

        cts.Cancel();
        loaderThread.Join();
    }
}
