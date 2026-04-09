using System.Collections.Immutable;
using Sdp;
using Sdp.Table;

namespace UnitTest.StaticDataTests;

public class StaticDataManagerTests
{
    private sealed record FakeRecord(int Id);

    private sealed class FakeTable : StaticDataTable<FakeRecord, int>
    {
        public const string PathA = "version-a";
        public const string PathB = "version-b";
        public const int CountA = 3;
        public const int CountB = 7;

        public FakeTable(string path)
            : base(CreateRecords(path), r => r.Id)
        {
            Thread.Sleep(50);
        }

        private static ImmutableList<FakeRecord> CreateRecords(string path)
        {
            var count = path == PathB ? CountB : CountA;
            return Enumerable.Range(1, count)
                .Select(i => new FakeRecord(i))
                .ToImmutableList();
        }
    }

    private sealed class FakeManager : StaticDataManager<FakeManager.TableSet>
    {
        public sealed record TableSet(FakeTable? Items);

        public TableSet Tables => Current;
    }

    [Fact]
    public void ConcurrentLoadAndRead_AlwaysSeesCompleteDataset()
    {
        var manager = new FakeManager();
        manager.Load(FakeTable.PathA);

        var cts = new CancellationTokenSource();

        var loaderThread = new Thread(() =>
        {
            var toggle = false;
            while (!cts.Token.IsCancellationRequested)
            {
                manager.Load(toggle ? FakeTable.PathB : FakeTable.PathA);
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
