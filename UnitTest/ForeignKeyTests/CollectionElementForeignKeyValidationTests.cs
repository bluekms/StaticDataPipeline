using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using Sdp.Attributes;
using Sdp.Manager;
using Sdp.Table;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.ForeignKeyTests;

public partial class CollectionElementForeignKeyValidationTests(ITestOutputHelper testOutputHelper)
{
    private const string SkillCsv =
        """
        Id,Name
        1001,강타
        1002,방패막기
        2001,저격
        """;

    private const string ValidHeroCsv =
        """
        Id,Name,SkillIds[0],SkillIds[1],SkillIds[2]
        1,전사,1001,1002,
        2,궁수,2001,,
        """;

    private const string ErrorHeroCsv =
        """
        Id,Name,SkillIds[0],SkillIds[1],SkillIds[2]
        1,전사,1001,9999,
        2,궁수,2001,,
        """;

    private const string ValidPartyCsv =
        """
        Id,HeroIds
        1,"1,2"
        2,2
        """;

    private const string ErrorPartyCsv =
        """
        Id,HeroIds
        1,"1,7"
        """;

    [StaticDataRecord("Skill", "Sheet1")]
    private partial record SkillRecord(int Id, string Name);

    [StaticDataRecord("Hero", "Sheet1")]
    private partial record HeroRecord(
        [Key] int Id,
        string Name,
        [Length(3)] [NullString("")] [ForeignKey("Skill", "Id")] ImmutableArray<int?> SkillIds);

    [StaticDataRecord("Party", "Sheet1")]
    private partial record PartyRecord(
        [Key] int Id,
        [SingleColumnCollection(",")] [ForeignKey("Hero", "Id")] ImmutableArray<int> HeroIds);

    private sealed partial class SkillTable : StaticDataTable<SkillRecord>;

    private sealed partial class HeroTable : StaticDataTable<HeroRecord>;

    private sealed partial class PartyTable : StaticDataTable<PartyRecord>;

    private sealed partial class StaticData(ILogger logger)
        : StaticDataManager<StaticData.TableSet>(logger)
    {
        public sealed partial record TableSet(SkillTable? Skill, HeroTable? Hero, PartyTable? Party);
    }

    [Fact]
    public async Task Load_ElementsExistInTarget_Succeeds()
    {
        var logger = CreateLogger();

        using var dir = new CsvTestDirectory();
        dir.Write("Skill.Sheet1.csv", SkillCsv);
        dir.Write("Hero.Sheet1.csv", ValidHeroCsv);
        dir.Write("Party.Sheet1.csv", ValidPartyCsv);

        var staticData = new StaticData(logger);
        await staticData.LoadAsync(dir.Path);

        Assert.Equal(2, staticData.Current.Hero!.Records.Length);
        Assert.Equal(2, staticData.Current.Party!.Records.Length);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public async Task Load_MultiColumnElementMissing_ThrowsWithValue()
    {
        var logger = CreateLogger();

        using var dir = new CsvTestDirectory();
        dir.Write("Skill.Sheet1.csv", SkillCsv);
        dir.Write("Hero.Sheet1.csv", ErrorHeroCsv);
        dir.Write("Party.Sheet1.csv", ValidPartyCsv);

        var staticData = new StaticData(logger);

        var ex = await Assert.ThrowsAsync<AggregateException>(() => staticData.LoadAsync(dir.Path));

        var inner = Assert.Single(ex.InnerExceptions);
        Assert.Contains("9999", inner.Message);
        Assert.Contains("Skill", inner.Message);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public async Task Load_SingleColumnElementMissing_ThrowsWithValue()
    {
        var logger = CreateLogger();

        using var dir = new CsvTestDirectory();
        dir.Write("Skill.Sheet1.csv", SkillCsv);
        dir.Write("Hero.Sheet1.csv", ValidHeroCsv);
        dir.Write("Party.Sheet1.csv", ErrorPartyCsv);

        var staticData = new StaticData(logger);

        var ex = await Assert.ThrowsAsync<AggregateException>(() => staticData.LoadAsync(dir.Path));

        var inner = Assert.Single(ex.InnerExceptions);
        Assert.Contains("7", inner.Message);
        Assert.Contains("Hero", inner.Message);
        Assert.Empty(logger.Logs);
    }

    private TestOutputLogger<CollectionElementForeignKeyValidationTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<CollectionElementForeignKeyValidationTests>() is not TestOutputLogger<CollectionElementForeignKeyValidationTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
