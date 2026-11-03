using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using Sdp.Attributes;
using Sdp.Manager;
using Sdp.Table;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.ForeignKeyTests;

public partial class SelfReferenceForeignKeyTests(ITestOutputHelper testOutputHelper)
{
    private TestOutputLogger<SelfReferenceForeignKeyTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<SelfReferenceForeignKeyTests>()
            is not TestOutputLogger<SelfReferenceForeignKeyTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }

    [StaticDataRecord("Quest", "Sheet1")]
    private partial record QuestRecord(
        [Key] int Id,
        [NullString("")] [ForeignKey("Quest", "Id")] int? PrevQuestId);

    private sealed partial class QuestTable(ImmutableArray<QuestRecord> records)
        : StaticDataTable<QuestRecord>(records);

    private sealed partial class SelfReferenceStaticData(ILogger logger)
        : StaticDataManager<SelfReferenceStaticData.TableSet>(logger)
    {
        public sealed partial record TableSet(QuestTable? Quest);
    }

    private const string ValidQuestCsv =
        """
        Id,PrevQuestId
        1,
        2,1
        3,2
        """;

    private const string MissingParentQuestCsv =
        """
        Id,PrevQuestId
        1,
        2,99
        """;

    private static CsvTestDirectory CreateDirectory(string questCsv)
    {
        var dir = new CsvTestDirectory();
        dir.Write("Quest.Sheet1.csv", questCsv);

        return dir;
    }

    [Fact]
    public async Task Load_SelfReferenceToExistingKey_Succeeds()
    {
        var logger = CreateLogger();

        using var dir = CreateDirectory(ValidQuestCsv);

        var staticData = new SelfReferenceStaticData(logger);
        await staticData.LoadAsync(dir.Path);

        var prevQuestIds = staticData.Current.Quest!.Records
            .Select(record => record.PrevQuestId)
            .ToList();
        Assert.Equal([null, 1, 2], prevQuestIds);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public async Task Load_SelfReferenceToMissingKey_ThrowsAggregateException()
    {
        var logger = CreateLogger();

        using var dir = CreateDirectory(MissingParentQuestCsv);

        var staticData = new SelfReferenceStaticData(logger);

        var ex = await Assert.ThrowsAsync<AggregateException>(() => staticData.LoadAsync(dir.Path));

        var inner = Assert.Single(ex.InnerExceptions);
        Assert.Contains("QuestRecord", inner.Message, StringComparison.Ordinal);
        Assert.Contains("PrevQuestId", inner.Message, StringComparison.Ordinal);
        Assert.Contains("99", inner.Message, StringComparison.Ordinal);
        Assert.Contains("Quest.Id", inner.Message, StringComparison.Ordinal);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public async Task Load_SelfReferenceTableDisabled_Succeeds()
    {
        var logger = CreateLogger();

        using var dir = CreateDirectory(MissingParentQuestCsv);

        var staticData = new SelfReferenceStaticData(logger);
        await staticData.LoadAsync(dir.Path, new List<string> { "Quest" });

        Assert.Null(staticData.Current.Quest);
        Assert.Empty(logger.Logs);
    }
}
