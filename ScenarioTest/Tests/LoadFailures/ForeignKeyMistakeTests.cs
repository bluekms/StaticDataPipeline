using Microsoft.Extensions.Logging.Abstractions;
using ScenarioTest.Managers;
using ScenarioTest.Support;

namespace ScenarioTest.Tests.LoadFailures;

[Collection(CliCollectionDefinition.Name)]
public class ForeignKeyMistakeTests(ExtractedCsvFixture fixture)
{
    [Fact]
    public async Task StudentAssignedToNonexistentClass399_FailsWithRowKeyAndColumn()
    {
        using var culture = CultureScope.Invariant();
        using var csv = fixture.CopyMainCsv();
        CsvDocument.Edit(csv.FileOf("School.Students.csv"), x => x.Set("Id", "20260101", "ClassRoomId", "399"));
        var school = new SchoolStaticData(NullLogger.Instance);

        var exception = await LoadAssert.FailsWithAggregate(() => school.LoadAsync(csv.Root), LoadAssert.FkFailed);

        var inner = Assert.Single(exception.InnerExceptions);
        LoadAssert.ContainsAll(inner, "StudentRecord[Id=20260101]", "ClassRoomId", "399", "ClassRoomTable.Id");
    }

    [Fact]
    public async Task LoanBorrowerNotEnrolled_FailsAcrossWorkbooks()
    {
        using var culture = CultureScope.Invariant();
        using var csv = fixture.CopyMainCsv();
        CsvDocument.Edit(csv.FileOf("Library.Loans.csv"), x => x.Rows[2][x.Column("Borrower")] = "20250999");
        var school = new SchoolStaticData(NullLogger.Instance);

        var exception = await LoadAssert.FailsWithAggregate(() => school.LoadAsync(csv.Root), LoadAssert.FkFailed);

        var inner = Assert.Single(exception.InnerExceptions);
        LoadAssert.ContainsAll(inner, "LoanRecord", "Borrower", "20250999", "StudentTable.Id");
    }

    [Fact]
    public async Task GachaPickupItemNotInItems_FailsPerCollectionElement()
    {
        using var culture = CultureScope.Invariant();
        using var csv = fixture.CopyMainCsv();
        CsvDocument.Edit(csv.FileOf("Rpg.Gacha.csv"), x => x.Set("Grade", "SSR", "PickupItemIds[1]", "19999"));
        var rpg = new RpgStaticData(NullLogger.Instance);

        var exception = await LoadAssert.FailsWithAggregate(() => rpg.LoadAsync(csv.Root), LoadAssert.FkFailed);

        var inner = Assert.Single(exception.InnerExceptions);
        LoadAssert.ContainsAll(inner, "GachaRecord", "PickupItemIds", "19999", "ItemTable.Id");
    }

    [Fact]
    public async Task CurrencyRewardPointingAtItemId_FailsWhenKindIsCurrency()
    {
        using var culture = CultureScope.Invariant();
        using var csv = fixture.CopyMainCsv();
        CsvDocument.Edit(csv.FileOf("Rpg.Rewards.csv"), x => x.Set("Id", "3", "TargetId", "10001"));
        var rpg = new RpgStaticData(NullLogger.Instance);

        var exception = await LoadAssert.FailsWithAggregate(() => rpg.LoadAsync(csv.Root), LoadAssert.FkFailed);

        var inner = Assert.Single(exception.InnerExceptions);
        LoadAssert.ContainsAll(inner, "RewardRecord[Id=3]", "TargetId(10001)", "(when Kind=Currency)");
    }

    [Fact]
    public async Task PremiumPassRewardIdOutOfPremiumTable_FailsWhenIsPremiumTrue()
    {
        using var culture = CultureScope.Invariant();
        using var csv = fixture.CopyMainCsv();
        CsvDocument.Edit(csv.FileOf("Rpg.SeasonPass.csv"), x => x.Set("Id", "2", "RewardId", "99"));
        var rpg = new RpgStaticData(NullLogger.Instance);

        var exception = await LoadAssert.FailsWithAggregate(() => rpg.LoadAsync(csv.Root), LoadAssert.FkFailed);

        var inner = Assert.Single(exception.InnerExceptions);
        LoadAssert.ContainsAll(inner, "SeasonPassRecord[Id=2]", "RewardId(99)", "PremiumRewardTable.Id", "IsPremium=");
    }

    [Fact]
    public async Task SeasonRewardDatedOutsideAnySeason_FailsWithNoMatchingCondition()
    {
        using var culture = CultureScope.Invariant();
        using var csv = fixture.CopyMainCsv();
        CsvDocument.Edit(csv.FileOf("Rpg.SeasonRewards.csv"), x => x.Set("Id", "1", "Season", "2026-07-01"));
        var rpg = new RpgStaticData(NullLogger.Instance);

        var exception = await LoadAssert.FailsWithAggregate(() => rpg.LoadAsync(csv.Root), LoadAssert.FkFailed);

        var inner = Assert.Single(exception.InnerExceptions);
        LoadAssert.ContainsAll(inner, "has no matching condition for 'Season=2026-07-01'");
    }

    [Fact]
    public async Task AttendanceRewardIdInNeitherItemsNorCurrencies_FailsNamingBothTargets()
    {
        using var culture = CultureScope.Invariant();
        using var csv = fixture.CopyMainCsv();
        CsvDocument.Edit(csv.FileOf("Rpg.Attendance.csv"), x => x.Set("Day", "1", "RewardId", "77777"));
        var rpg = new RpgStaticData(NullLogger.Instance);

        var exception = await LoadAssert.FailsWithAggregate(() => rpg.LoadAsync(csv.Root), LoadAssert.FkFailed);

        var inner = Assert.Single(exception.InnerExceptions);
        LoadAssert.ContainsAll(inner, "AttendanceRecord", "RewardId", "77777", "ItemTable.Id", "CurrencyTable.Id");
    }

    [Fact]
    public async Task PremiumRewardItemIdNotInItems_Fails()
    {
        using var culture = CultureScope.Invariant();
        using var csv = fixture.CopyMainCsv();
        CsvDocument.Edit(csv.FileOf("Rpg.PremiumRewards.csv"), x => x.Set("Id", "1", "ItemId", "19999"));
        var rpg = new RpgStaticData(NullLogger.Instance);

        var exception = await LoadAssert.FailsWithAggregate(() => rpg.LoadAsync(csv.Root), LoadAssert.FkFailed);

        var inner = Assert.Single(exception.InnerExceptions);
        LoadAssert.ContainsAll(inner, "PremiumRewardRecord", "ItemId", "19999", "ItemTable.Id");
    }

    [Fact]
    public async Task SeasonRewardItemIdNotInSeason1Items_FailsWhenSeasonIsJune()
    {
        using var culture = CultureScope.Invariant();
        using var csv = fixture.CopyMainCsv();
        CsvDocument.Edit(csv.FileOf("Rpg.SeasonRewards.csv"), x => x.Set("Id", "1", "ItemId", "29999"));
        var rpg = new RpgStaticData(NullLogger.Instance);

        var exception = await LoadAssert.FailsWithAggregate(() => rpg.LoadAsync(csv.Root), LoadAssert.FkFailed);

        var inner = Assert.Single(exception.InnerExceptions);
        LoadAssert.ContainsAll(
            inner,
            "SeasonRewardRecord",
            "ItemId",
            "29999",
            "Season1ItemTable.Id",
            "Season=2026-06-01");
    }

    [Fact]
    public async Task QuestPrevQuestIdNotInQuests_FailsAgainstOwnTable()
    {
        using var culture = CultureScope.Invariant();
        using var csv = fixture.CopyMainCsv();
        CsvDocument.Edit(csv.FileOf("Rpg.Quests.csv"), x => x.Set("Id", "3", "PrevQuestId", "99"));
        var rpg = new RpgStaticData(NullLogger.Instance);

        var exception = await LoadAssert.FailsWithAggregate(() => rpg.LoadAsync(csv.Root), LoadAssert.FkFailed);

        var inner = Assert.Single(exception.InnerExceptions);
        LoadAssert.ContainsAll(inner, "QuestRecord[Id=3]", "PrevQuestId", "99", "QuestTable.Id");
    }

    [Theory]
    [InlineData("de-DE", "not found in any of")]
    [InlineData("ko-KR", "중 어디에도 존재하지 않습니다")]
    public async Task FkMessageLanguage_FollowsUICultureWithEnglishFallback(string cultureName, string expected)
    {
        using var culture = CultureScope.Of(cultureName);
        using var csv = fixture.CopyMainCsv();
        CsvDocument.Edit(csv.FileOf("School.Students.csv"), x => x.Set("Id", "20260101", "ClassRoomId", "399"));
        var school = new SchoolStaticData(NullLogger.Instance);

        var exception = await Record.ExceptionAsync(() => school.LoadAsync(csv.Root));

        var aggregate = Assert.IsType<AggregateException>(exception);
        LoadAssert.ContainsAll(Assert.Single(aggregate.InnerExceptions), expected);
    }
}
