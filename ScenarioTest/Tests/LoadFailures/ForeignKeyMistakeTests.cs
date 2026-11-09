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
    [InlineData("Rpg.Shop.csv", "ItemId", "10001", "Prices[1].Key.Currency", "999", "ShopServerRecord[ItemId=10001].Prices[Key=", "Kind = Sale", ".Key.Currency(999)", "CurrencyTable.Id")]
    [InlineData("Rpg.Rewards.csv", "Id", "3", "TargetId", "10001", "RewardRecord[Id=3]", "TargetId(10001)", "(when Kind=Currency)")]
    [InlineData("Rpg.SeasonPass.csv", "Id", "2", "RewardId", "99", "SeasonPassRecord[Id=2].RewardId(99)", "PremiumRewardTable.Id", "IsPremium=true")]
    [InlineData("Rpg.SeasonRewards.csv", "Id", "1", "Season", "2026-07-01", "has no matching condition for 'Season=2026-07-01'")]
    [InlineData("Rpg.SeasonRewards.csv", "Id", "1", "ItemId", "29999", "SeasonRewardRecord", "ItemId", "29999", "Season1ItemTable.Id", "Season=2026-06-01")]
    [InlineData("Rpg.Attendance.csv", "Day", "1", "RewardId", "77777", "AttendanceRecord", "RewardId", "77777", "ItemTable.Id", "CurrencyTable.Id")]
    [InlineData("Rpg.PremiumRewards.csv", "Id", "1", "ItemId", "19999", "PremiumRewardRecord", "ItemId", "19999", "ItemTable.Id")]
    [InlineData("Rpg.Quests.csv", "Id", "3", "PrevQuestId", "99", "QuestRecord[Id=3]", "PrevQuestId", "99", "QuestTable.Id")]
    public async Task OneCellPointsAtMissingTarget_FailsNamingRowColumnAndTarget(
        string csvFile,
        string keyHeader,
        string key,
        string header,
        string value,
        params string[] expected)
    {
        using var csv = fixture.CopyMainCsv();
        csv.Edit(csvFile, x => x.Set(keyHeader, key, header, value));

        await LoadAssert.FailsWithSingle(Load.ForCsv(csv.Root, csvFile), LoadAssert.FkFailed, expected);
    }

    [Fact]
    public async Task LoanBorrowerNotEnrolled_FailsAcrossWorkbooks()
    {
        using var csv = fixture.CopyMainCsv();
        csv.Edit("Library.Loans.csv", x => x.Rows[2][x.ColumnIndex("Borrower")] = "20250999");

        await LoadAssert.FailsWithSingle(Load.School(csv.Root), LoadAssert.FkFailed, "LoanRecord", "Borrower", "20250999", "StudentTable.Id");
    }

    [Fact]
    public async Task MonsterSkillNotInSkills_FailsWithPositionInsideTheCell()
    {
        using var csv = fixture.CopyMainCsv();
        csv.Edit("Rpg.Skills.csv", x => x.Rows.RemoveAll(row => row[x.ColumnIndex("Id")] == "Heal"));

        await LoadAssert.FailsWithSingle(Load.Rpg(csv.Root), LoadAssert.FkFailed, "MonsterRecord[Id=BossDragon].Skills[2](Heal)", "SkillTable.Id");
    }

    [Theory]
    [InlineData("de-DE", "not found in any of")]
    [InlineData("ko-KR", "중 어디에도 존재하지 않습니다")]
    public async Task FkMessageLanguage_FollowsUICultureWithEnglishFallback(string cultureName, string expected)
    {
        using var culture = CultureScope.Of(cultureName);
        using var csv = fixture.CopyMainCsv();
        csv.Edit("School.Students.csv", x => x.Set("Id", "20260101", "ClassRoomId", "399"));
        var school = new SchoolStaticData(NullLogger.Instance);

        var exception = await Record.ExceptionAsync(() => school.LoadAsync(csv.Root));

        var aggregate = Assert.IsType<AggregateException>(exception);
        LoadAssert.ContainsAll(Assert.Single(aggregate.InnerExceptions), expected);
    }
}
