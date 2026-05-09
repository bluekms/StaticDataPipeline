using System.Collections.Immutable;
using System.Text;
using Microsoft.Extensions.Logging;
using Sdp.Attributes;
using Sdp.Manager;
using Sdp.Table;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.StaticDataTests;

public class StaticDataManagerTests(ITestOutputHelper testOutputHelper)
{
    [StaticDataRecord("Fake", "Sheet1")]
    private sealed record FakeRecord(int Id);

    private sealed class FakeTable(ImmutableList<FakeRecord> records)
        : StaticDataTable<FakeTable, FakeRecord>(records);

    private sealed class FakeManager(ILogger logger)
        : StaticDataManager<FakeManager.TableSet>(logger)
    {
        public sealed record TableSet(FakeTable? Items);

        public TableSet Tables => Current;
    }

    [Fact]
    public async Task ConcurrentLoadAndRead_AlwaysSeesCompleteDataset()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<StaticDataManagerTests>() is not TestOutputLogger<StaticDataManagerTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        const int CountA = 3;
        const int CountB = 7;

        using var dirA = new CsvTestDirectory();
        using var dirB = new CsvTestDirectory();
        dirA.Write("Fake.Sheet1.csv", BuildCsv(CountA));
        dirB.Write("Fake.Sheet1.csv", BuildCsv(CountB));

        var manager = new FakeManager(logger);
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

        Assert.Empty(logger.Logs);
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
