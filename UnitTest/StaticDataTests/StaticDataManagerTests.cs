using System.Collections.Immutable;
using Sdp.Manager;
using Sdp.Table;

namespace UnitTest.StaticDataTests;

public class StaticDataManagerTests
{
    private sealed record FakeRecord(int Id);

    private sealed class FakeTable(ImmutableList<FakeRecord> records)
        : StaticDataTable<FakeTable, FakeRecord>(records);

    private sealed class FakeManager : StaticDataManager<FakeManager.TableSet>
    {
        public sealed record TableSet(FakeTable? Items);

        public TableSet Tables => Current;

        public void Load(int count)
        {
            var records = Enumerable.Range(1, count)
                .Select(i => new FakeRecord(i))
                .ToImmutableList();
            Load(new TableSet(new FakeTable(records)));
        }
    }

    [Fact]
    public void ConcurrentLoadAndRead_AlwaysSeesCompleteDataset()
    {
        const int CountA = 3;
        const int CountB = 7;

        var manager = new FakeManager();
        manager.Load(CountA);

        var cts = new CancellationTokenSource();

        var loaderThread = new Thread(() =>
        {
            var toggle = false;
            while (!cts.Token.IsCancellationRequested)
            {
                manager.Load(toggle ? CountB : CountA);
                toggle = !toggle;
            }
        });
        loaderThread.IsBackground = true;
        loaderThread.Start();

        for (var i = 0; i < 10000; i++)
        {
            var count = manager.Tables.Items?.Records.Count;
            Assert.True(
                count == CountA || count == CountB,
                FormattableString.Invariant($"예상: {CountA} 또는 {CountB}, 실제: {count}"));
        }

        cts.Cancel();
        loaderThread.Join();
    }
}
