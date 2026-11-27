using Microsoft.Extensions.Logging.Abstractions;
using ScenarioTest.Managers;
using ScenarioTest.Support;

namespace ScenarioTest.Tests.LoadFailures;

[Collection(CliCollectionDefinition.Name)]
public class ForeignKeyMistakeTests(ExtractedCsvFixture fixture)
{
    [Theory]
    [InlineData("School.Students.csv", "Id", "20260101", "ClassRoomId", "399", "StudentRecord[Id=20260101].ClassRoomId(399)", "ClassRoomTable.Id")]
    [InlineData("Rpg.Gacha.csv", "Grade", "SSR", "PickupItemIds[1]", "19999", "GachaRecord", "PickupItemIds", "19999", "ItemTable.Id")]
    [InlineData("Rpg.Monsters.csv", "Id", "Slime", "Drops[1].ItemId", "19999", "MonsterRecord[Id=Slime].Drops[1].ItemId(19999)", "ItemTable.Id")]
    [InlineData("Rpg.Shop.csv", "Id", "1", "Prices[1].Key.Currency", "999", "ShopRecord[Id=1].Prices[Key=", "Kind = Sale", ".Key.Currency(999)", "CurrencyTable.Id")]
    [InlineData("Rpg.Rewards.csv", "Id", "3", "TargetId", "10001", "RewardRecord[Id=3]", "TargetId(10001)", "(when Kind=Currency)")]
    [InlineData("Rpg.Rewards.csv", "Id", "1", "TargetId", "77777", "RewardRecord[Id=1].TargetId(77777)", "ItemTable.Id", "(when Kind=Item)")]
    [InlineData("Rpg.SeasonPass.csv", "Id", "2", "RewardId", "99", "SeasonPassRecord[Id=2].RewardId(99)", "PremiumRewardTable.Id", "IsPremium=true")]
    [InlineData("Rpg.SeasonRewards.csv", "Id", "1", "SeasonStartDay", "2026-07-01", "has no matching condition for 'SeasonStartDay=2026-07-01'")]
    [InlineData("Rpg.SeasonRewards.csv", "Id", "1", "ItemId", "29999", "SeasonRewardRecord", "ItemId", "29999", "SeasonItemTable.Id", "SeasonStartDay=2026-06-01")]
    [InlineData("Rpg.SeasonRewards.csv", "Id", "2", "ItemId", "39999", "SeasonRewardRecord", "ItemId", "39999", "SeasonItemTableV2.Id", "SeasonStartDay=2026-11-01")]
    [InlineData("Rpg.PremiumRewards.csv", "Id", "1", "ItemId", "19999", "PremiumRewardRecord", "ItemId", "19999", "ItemTable.Id")]
    [InlineData("Rpg.Quests.csv", "Id", "3", "PrevQuestId", "99", "QuestRecord[Id=3].PrevQuestId(99)", "QuestTable.Id")]
    [InlineData("Rpg.Quests.csv", "Id", "2", "Collect.Item", "19999", "QuestRecord[Id=2].Collect.Item(19999)", "ItemTable.Id")]
    [InlineData("Rpg.Quests.csv", "Id", "1", "Rewards[1].Item", "19999", "QuestRecord[Id=1].Rewards", "Item(19999)", "ItemTable.Id")]
    public async Task OneCellPointsAtMissingTarget_FailsNamingRowColumnAndTarget(
        string csvFile,
        string keyHeader,
        string key,
        string header,
        string value,
        params string[] expected)
    {
        using var csv = fixture.CopyCsvFor(csvFile);
        csv.Edit(csvFile, x => x.Set(keyHeader, key, header, value));

        await LoadAssert.FailsWithSingle(Load.ForCsv(csv.Root, csvFile), LoadAssert.FkFailed, expected);
    }

    [Fact]
    public async Task BookTalkPresenterNotEnrolled_FailsAcrossWorkbooks()
    {
        using var csv = fixture.CopySchoolCsv();
        csv.Edit("Library.BookTalks.csv", x => x.Rows[2][x.ColumnIndex("Presenter")] = "20250999");

        await LoadAssert.FailsWithSingle(
            Load.School(csv.Root),
            LoadAssert.FkFailed,
            "BookTalkRecord",
            "Presenter",
            "20250999",
            "StudentTable.Id");
    }

    [Fact]
    public async Task MonsterSkillNotInSkills_FailsWithPositionInsideTheCell()
    {
        using var csv = fixture.CopyRpgCsv();
        csv.Edit("Rpg.Skills.csv", x => x.Rows.Remove(x.Row("Id", "Heal")));

        await LoadAssert.FailsWithSingle(
            Load.Rpg(csv.Root),
            LoadAssert.FkFailed,
            "MonsterRecord[Id=BossDragon].Skills[2](Heal)",
            "SkillTable.Id");
    }

    [Fact]
    public async Task GoblinNotInMonsters_FailsForQuestKillTargetAndStageBoss()
    {
        using var csv = fixture.CopyRpgCsv();
        csv.Edit("Rpg.Monsters.csv", x => x.Rows.Remove(x.Row("Id", "1002")));

        var exception = await LoadAssert.FailsWithAggregate(Load.Rpg(csv.Root), LoadAssert.FkFailed);

        Assert.Equal(3, exception.InnerExceptions.Count);
        LoadAssert.ContainsAll(
            exception,
            "QuestRecord[Id=2].Kill.Monster(Goblin)",
            "StageServerRecord[Id=2].Boss(Goblin)",
            "StageClientRecord[Id=2].Boss(Goblin)",
            "MonsterTable.Id");
    }

    [Theory]
    [InlineData("de-DE", "not found in ClassRoomTable.Id")]
    [InlineData("ko-KR", "ClassRoomTable.Id 에 존재하지 않습니다")]
    public async Task FkMessageLanguage_FollowsUICultureWithEnglishFallback(string cultureName, string expected)
    {
        using var culture = CultureScope.Of(cultureName);
        using var csv = fixture.CopySchoolCsv();
        csv.Edit("School.Students.csv", x => x.Set("Id", "20260101", "ClassRoomId", "399"));
        var school = new SchoolStaticData(NullLogger.Instance);

        var exception = await Record.ExceptionAsync(() => school.LoadAsync(csv.Root));

        var aggregate = Assert.IsType<AggregateException>(exception);
        LoadAssert.ContainsAll(Assert.Single(aggregate.InnerExceptions), expected);
    }
}
