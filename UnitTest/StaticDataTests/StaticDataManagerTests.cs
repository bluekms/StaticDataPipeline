using System.Collections.Immutable;
using System.Text;
using Sdp.Attributes;
using Sdp.Manager;
using Sdp.Table;
using UnitTest.Utility;

namespace UnitTest.StaticDataTests;

public class StaticDataManagerTests
{
    [StaticDataRecord("Fake", "Sheet1")]
    private sealed record FakeRecord(int Id);

    private sealed class FakeTable(ImmutableList<FakeRecord> records)
        : StaticDataTable<FakeTable, FakeRecord>(records);

    private sealed class FakeManager : StaticDataManager<FakeManager.TableSet>
    {
        public sealed record TableSet(FakeTable? Items);

        public TableSet Tables => Current;
    }

    [Fact]
    public async Task ConcurrentLoadAndRead_AlwaysSeesCompleteDataset()
    {
        const int CountA = 3;
        const int CountB = 7;

        using var dirA = new CsvTestDirectory();
        using var dirB = new CsvTestDirectory();
        dirA.Write("Fake.Sheet1.csv", BuildCsv(CountA));
        dirB.Write("Fake.Sheet1.csv", BuildCsv(CountB));

        var manager = new FakeManager();
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

    private static string BuildCsv(int count)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Id");
        for (var i = 1; i <= count; i++)
        {
            sb.AppendLine(FormattableString.Invariant($"{i}"));
        }

        return sb.ToString();
    }
}
