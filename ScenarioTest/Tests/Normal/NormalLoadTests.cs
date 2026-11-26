using Microsoft.Extensions.Logging.Abstractions;
using ScenarioTest.Managers;
using ScenarioTest.Records.Rpg;
using ScenarioTest.Records.School;
using ScenarioTest.Support;

namespace ScenarioTest.Tests.Normal;

[Collection(CliCollectionDefinition.Name)]
public class NormalLoadTests(ExtractedCsvFixture fixture)
{
    [Fact]
    public async Task SchoolCsv_LoadsWithReportCardsAndTeacherSchedules()
    {
        ExtractionAssert.Succeeded(fixture.SchoolResult);
        var school = new SchoolStaticData(NullLogger.Instance);

        await school.LoadAsync(fixture.CsvSchool);

        var snapshot = school.Current;
        var card = snapshot.Views.ReportCardView.Get(new StudentRecord.StudentId(20260101));
        Assert.Equal("김하늘", card.Student.Name);
        Assert.Equal(new ClassRoomRecord.ClassRoomId(101), card.ClassRoom.Id);
        Assert.NotNull(card.Grade);
        Assert.Equal(92, card.Grade.Scores[SubjectCode.Korean].Score);
        Assert.Equal('A', card.Grade.Scores[SubjectCode.English].Rank);
        Assert.Equal([21, 19, 20, null, 18], card.Grade.MonthlyAttendance.ToArray());
        Assert.Equal(3, snapshot.Tables.StudentTable.Records.Count(x => x.ClassRoomId.Value == 101));
        Assert.Equal(2, snapshot.Views.TeacherScheduleView.Get(new TeacherRecord.TeacherId(7)).Count);

        var students = snapshot.Tables.StudentTable;
        Assert.Equal(6, students.Records.Length);
        Assert.Null(students.Get(new StudentRecord.StudentId(20240308)).ClubId);
        Assert.Equal("하늘이", students.Get(new StudentRecord.StudentId(20260101)).Nickname);
        Assert.Equal(4000123412341234UL, students.Get(new StudentRecord.StudentId(20260101)).CardNo);
        Assert.True(students.Get(new StudentRecord.StudentId(20250215)).IsDormitory);
        Assert.Equal(new DateOnly(2010, 4, 17), students.Get(new StudentRecord.StudentId(20260101)).BirthDate);

        var teacher = snapshot.Tables.TeacherTable.Get(new TeacherRecord.TeacherId(7));
        Assert.Equal(["정교사2급", "정보처리기사"], teacher.Certificates.Order(StringComparer.Ordinal));
        Assert.Null(snapshot.Tables.TeacherTable.Get(new TeacherRecord.TeacherId(8)).Office);
        Assert.Empty(snapshot.Tables.TeacherTable.Get(new TeacherRecord.TeacherId(9)).Certificates);

        var chuseok = snapshot.Tables.CalendarTable!.Records.Single(x => x.Id == 4);
        Assert.Equal(new DateOnly(2026, 9, 26), chuseok.End);
        Assert.Null(snapshot.Tables.CalendarTable.Records.Single(x => x.Id == 1).End);
        Assert.Null(snapshot.Tables.ClubTable!.Records.Single(x => x.Id == 7).RequiresAudition);
        var robotics = snapshot.Tables.ClubTable.Records.Single(x => x.Id == 3);
        Assert.Equal([DayOfWeek.Tuesday, DayOfWeek.Thursday], robotics.MeetingDays.ToArray());
        Assert.Equal(new TimeOnly(16, 30), robotics.MeetingTime);
        Assert.Null(snapshot.Tables.ClubTable.Records.Single(x => x.Id == 5).MeetingTime);

        var lunch = snapshot.Tables.CafeteriaTable!.Records[0];
        Assert.Equal(4, lunch.Menu.Length);
        Assert.Equal(new byte[] { 1, 5, 9, 10 }, lunch.Allergens.Order());

        Assert.Equal(3, snapshot.Tables.BookTalkTable!.Records.Length);
        var secondTalk = snapshot.Tables.BookTalkTable.Records[1];
        Assert.Equal(Guid.Parse("{3f2504e0-4f89-11d3-9a0c-0305e82c3301}"), secondTalk.RoomId);
        Assert.Empty(snapshot.Tables.RecommendationTable!.Records);
        var books = snapshot.Tables.BookTable!.Records;
        Assert.Null(books.Single(x => x.Id == new BookId(2)).Isbn13);
        Assert.Equal("도서관의\n밤", books.Single(x => x.Id == new BookId(3)).Title);
        Assert.Null(books.Single(x => x.Id == new BookId(2)).ShelfCode);
    }

    [Fact]
    public async Task ScienceCsv_LoadsRealElementValues()
    {
        ExtractionAssert.Succeeded(fixture.ScienceResult);
        var science = new ScienceStaticData(NullLogger.Instance);

        await science.LoadAsync(fixture.CsvScience);

        var elements = science.Current.ElementTable!;
        Assert.Equal(8, elements.Records.Length);
        Assert.Equal(7.874, elements.GetBySymbol("Fe").Density);
        Assert.Equal(0.00008988, elements.GetBySymbol("H").Density);
        Assert.Null(elements.GetBySymbol("He").Electronegativity);
        Assert.Null(elements.GetBySymbol("He").CommonOxidation);
        Assert.Null(elements.GetBySymbol("Og").Density);
        Assert.Null(elements.GetBySymbol("Og").MeltingPointK);
        Assert.Null(elements.GetBySymbol("La").Group);
        Assert.Null(elements.GetBySymbol("Au").DiscoveredYear);
        Assert.Equal((sbyte)-2, elements.GetBySymbol("O").CommonOxidation);
        Assert.Equal(1337.33m, elements.GetBySymbol("Au").MeltingPointK);
    }

    [Fact]
    public async Task RpgCsv_LoadsSharedSheetsFilteredTablesAndNullableCollections()
    {
        var rpg = new RpgStaticData(NullLogger.Instance);

        await rpg.LoadAsync(fixture.CsvRpg);

        var tables = rpg.Current;
        var monsters = tables.MonsterTable!;
        Assert.Null(monsters.Get(MonsterId.Slime).Weakness[1]);
        Assert.Equal([SkillId.Fireball, null, SkillId.Heal], monsters.Get(MonsterId.BossDragon).Skills.ToArray());
        Assert.Equal(35.5m, monsters.Get(MonsterId.Slime).Drops[0].Rate);
        Assert.Equal(new MapId(3), monsters.Get(MonsterId.Slime).Spawn.Map);
        Assert.Equal(TimeSpan.FromMinutes(30), monsters.Get(MonsterId.BossDragon).RespawnAt);

        var shop = tables.ShopTable!;
        Assert.Equal(2, shop.Records.Length);
        var potionShop = shop.Get(new ShopRecord.ShopItemId(1));
        Assert.Equal([new ItemRecord.ItemId(10001), new ItemRecord.ItemId(10001)], potionShop.ItemIds.ToArray());
        Assert.Equal("icons/potion.png", potionShop.IconPath);
        Assert.Equal(LimitKind.None, potionShop.LimitKind);
        Assert.Equal(0u, potionShop.LimitCount);
        Assert.Equal(new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.FromHours(9)), potionShop.SaleStart);
        Assert.Null(potionShop.SaleDuration);
        Assert.Equal(2, potionShop.Prices.Count);
        var shopCurrency = new CurrencyRecord.CurrencyId(901);
        Assert.Equal("봄 이벤트", potionShop.Prices[new PriceKey(shopCurrency, PurchaseKind.Sale)].Note);
        Assert.Null(potionShop.Prices[new PriceKey(shopCurrency, PurchaseKind.Normal)].Note);
        var gearShop = shop.Get(new ShopRecord.ShopItemId(2));
        Assert.Equal(LimitKind.Account, gearShop.LimitKind);
        Assert.Equal(1u, gearShop.LimitCount);
        Assert.Equal(TimeSpan.FromDays(7), gearShop.SaleDuration);

        // 한 시트를 두 Record 가 나눠 읽는다. RequiredLevel, DailyLimit 은 양쪽이 같은 열을 읽는다.
        var serverStages = tables.StageServerTable!;
        Assert.Equal(3, serverStages.Records.Length);
        var dragonStage = serverStages.Get(new StageServerRecord.StageId(3));
        Assert.Equal(MonsterId.BossDragon, dragonStage.Boss);
        Assert.Equal(180, dragonStage.MinClearSeconds);
        Assert.Equal(3u, dragonStage.DailyLimit);
        Assert.Null(serverStages.Get(new StageServerRecord.StageId(1)).DailyLimit);
        var clientStages = tables.StageClientTable!.Records;
        Assert.Equal(3, clientStages.Length);
        var goblinStage = clientStages.Single(x => x.Id == new StageClientRecord.StageId(2));
        Assert.Equal("고블린 숲", goblinStage.Name);
        Assert.Null(goblinStage.Bgm);
        Assert.Equal(5, goblinStage.RequiredLevel);
        Assert.Equal(MonsterId.Goblin, goblinStage.Boss);
        Assert.Equal(3u, clientStages.Single(x => x.Id == new StageClientRecord.StageId(3)).DailyLimit);

        Assert.Equal(2, tables.BuffTable!.Records.Length);
        Assert.Equal(2, tables.DebuffTable!.Records.Length);
        Assert.Equal("독", tables.DebuffTable.Records.Single(x => x.Id == new BuffRecord.BuffId(3)).Name);
        Assert.Equal(3, tables.SkillTable!.Records.Length);
        Assert.Null(tables.SkillTable.Records.Single(x => (int)x.Id == 1003).Cooldown);

        var items = tables.ItemTable;
        Assert.Equal(["healing", "consumable", "starter"], items.Records[0].Tags.ToArray());
        Assert.Null(items.Records[0].Slot);
        Assert.Equal(1.15f, items.Records[1].Stats.Speed);
        Assert.Equal("철 검", items.Get(new ItemRecord.ItemId(10002)).Name);
        Assert.Equal(9999999999L, tables.CurrencyTable!.Records[0].Cap);

        // 코드 이름 SeasonItemTable 은 OldSeasonItems 시트를, SeasonItemTableV2 는 SeasonItems 시트를 읽는다.
        var oldSeasonItems = tables.SeasonItemTable!.Records;
        Assert.Equal(3, oldSeasonItems.Length);
        Assert.Equal("시즌1 기념 검", oldSeasonItems.Single(x => x.Id == new SeasonItemRecord.SeasonItemId(20001)).Name);
        var newSeasonItems = tables.SeasonItemTableV2!.Records;
        Assert.Equal(2, newSeasonItems.Length);
        var bow = newSeasonItems.Single(x => x.Id == new SeasonItemRecordV2.SeasonItemId(30001));
        Assert.Equal("시즌2 기념 활", bow.Name);
        Assert.Equal(Rarity.Rare, bow.Rarity);

        var dragonNest = tables.QuestTable!.Records.Single(x => x.Id == new QuestRecord.QuestId(3));
        Assert.Equal(new QuestRecord.QuestId(2), dragonNest.PrevQuestId);
        Assert.Equal(new KillGoal(MonsterId.BossDragon, 1), dragonNest.Kill);
        Assert.Equal(new CollectGoal(new ItemRecord.ItemId(10003), 1), dragonNest.Collect);
        Assert.Equal(2, dragonNest.Rewards.Count);
        var potionReward = new QuestReward(new ItemRecord.ItemId(10001), 10);
        Assert.Contains(potionReward, (IReadOnlySet<QuestReward>)dragonNest.Rewards);
        Assert.Null(tables.QuestTable.Records.Single(x => x.Id == new QuestRecord.QuestId(1)).PrevQuestId);
        Assert.Equal(100m, tables.GachaTable!.Records.Sum(x => x.Rate));
        var ssr = tables.GachaTable.Records.Single(x => x.Grade == GachaGrade.SSR);
        Assert.Equal([10002, 10003], ssr.PickupItemIds.Select(x => x.Value).Order());
        Assert.Equal("철 검", items.Get(ssr.PickupItemIds.Single(x => x.Value == 10002)).Name);
    }

    [Fact]
    public async Task ItemTable_GetThrowsForUnknownId_TryGetReturnsFalse()
    {
        var rpg = new RpgStaticData(NullLogger.Instance);
        await rpg.LoadAsync(fixture.CsvRpg);
        var items = rpg.Current.ItemTable;

        var found = items.TryGet(new ItemRecord.ItemId(19999), out var missing);

        Assert.Equal("철 검", items.Get(new ItemRecord.ItemId(10002)).Name);
        var exception = Assert.Throws<KeyNotFoundException>(() => items.Get(new ItemRecord.ItemId(19999)));
        Assert.Contains("ItemRecord", exception.Message, StringComparison.Ordinal);
        Assert.False(found);
        Assert.Null(missing);
    }

    [Fact]
    public async Task TeacherScheduleView_UnknownTeacher_ReturnsEmptyList()
    {
        var school = new SchoolStaticData(NullLogger.Instance);
        await school.LoadAsync(fixture.CsvSchool);
        var view = school.Current.Views.TeacherScheduleView;

        Assert.Equal(2, view.Get(new TeacherRecord.TeacherId(7)).Count);
        Assert.Empty(view.Get(new TeacherRecord.TeacherId(99)));
    }

    [Fact]
    public async Task ReportCardView_GetThrowsForUnknownStudent_TryGetReturnsFalse()
    {
        var school = new SchoolStaticData(NullLogger.Instance);
        await school.LoadAsync(fixture.CsvSchool);
        var view = school.Current.Views.ReportCardView;

        var found = view.TryGet(new StudentRecord.StudentId(20239999), out var missing);

        Assert.Equal("김하늘", view.Get(new StudentRecord.StudentId(20260101)).Student.Name);
        Assert.Throws<KeyNotFoundException>(() => view.Get(new StudentRecord.StudentId(20239999)));
        Assert.False(found);
        Assert.Null(missing);
        Assert.Equal(3, view.GetByClassRoom(new ClassRoomRecord.ClassRoomId(101)).Count);
        Assert.Null(view.Get(new StudentRecord.StudentId(20260103)).Grade);
    }

    [Fact]
    public async Task CurrentIsNullBeforeFirstLoad_AndConcurrentLoadIsRejected()
    {
        using var gate = new LoadGate();
        var school = new SchoolStaticData(NullLogger.Instance) { Gate = gate };
        Assert.Null(school.Current);

        var first = Task.Run(() => school.LoadAsync(fixture.CsvSchool));
        gate.WaitUntilHeld(first);
        var second = await Record.ExceptionAsync(() => school.LoadAsync(fixture.CsvSchool));
        gate.Release();
        await first;

        Assert.IsType<InvalidOperationException>(second);
        Assert.NotNull(school.Current);
    }

    [Fact]
    public async Task DisabledGacha_LoadsWithNullGachaTable()
    {
        var rpg = new RpgStaticData(NullLogger.Instance);

        await rpg.LoadAsync(fixture.CsvRpg, ["GachaTable"]);

        Assert.Null(rpg.Current.GachaTable);
        Assert.NotNull(rpg.Current.ItemTable);
    }

    [Fact]
    public async Task DisabledTableNameWithWrongCase_FailsAsArgumentErrorSuggestingTheRealName()
    {
        using var culture = CultureScope.Invariant();

        var exception = await Record.ExceptionAsync(Load.Rpg(fixture.CsvRpg, ["itemTable"]));

        var argumentException = Assert.IsType<ArgumentException>(exception);
        var expected = "disabledTables has names that are not members of the TableSet: "
            + "'itemTable' (did you mean 'ItemTable'?).";
        Assert.StartsWith(expected, argumentException.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DisabledTableNamesMisspelled_FailsAsArgumentErrorNamingOnlyThem()
    {
        using var culture = CultureScope.Invariant();

        var exception = await Record.ExceptionAsync(Load.Rpg(fixture.CsvRpg, ["GachaTables", "ItemTable", "ShopTables"]));

        var argumentException = Assert.IsType<ArgumentException>(exception);
        var expected = "disabledTables has names that are not members of the TableSet: 'GachaTables', 'ShopTables'.";
        Assert.StartsWith(expected, argumentException.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DisabledCurrencies_FailsFkTargetNotFoundForEveryReferencingTable()
    {
        var load = Load.Rpg(fixture.CsvRpg, ["CurrencyTable"]);
        var exception = await LoadAssert.FailsWithAggregate(load, LoadAssert.FkFailed);

        var messages = exception.InnerExceptions.Select(x => x.Message).ToList();
        Assert.Equal(4, messages.Count);
        Assert.Contains("RewardRecord.TargetId: FK target 'CurrencyTable' not loaded or disabled.", messages);
        Assert.Contains(messages, x => x.StartsWith("AttendanceRecord.RewardId", StringComparison.Ordinal));
        Assert.Contains(messages, x => x.StartsWith("FreeRewardRecord.CurrencyId", StringComparison.Ordinal));
        Assert.Contains(messages, x => x.StartsWith("ShopRecord.Prices.Key.Currency", StringComparison.Ordinal));
        Assert.All(messages, x => Assert.Contains("CurrencyTable", x, StringComparison.Ordinal));
    }

    [Fact]
    public async Task DisabledNonNullableItems_FailsNonNullableTableDisabled()
    {
        using var culture = CultureScope.Invariant();
        var rpg = new RpgStaticData(NullLogger.Instance);

        var exception = await Record.ExceptionAsync(() => rpg.LoadAsync(fixture.CsvRpg, ["ItemTable"]));

        Assert.NotNull(exception);
        LoadAssert.ContainsAll(exception, "'ItemTable' is non-nullable");
    }
}
