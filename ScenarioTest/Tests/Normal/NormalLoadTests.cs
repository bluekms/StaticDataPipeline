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
        ExtractionAssert.Succeeded(fixture.MainResult);
        var school = new SchoolStaticData(NullLogger.Instance);

        await school.LoadAsync(fixture.CsvMain);

        var snapshot = school.Current;
        var card = snapshot.Views.ReportCardView.Get(new StudentRecord.StudentId(20260101));
        Assert.Equal("김하늘", card.Student.Name);
        Assert.Equal(new ClassRoomRecord.ClassRoomId(101), card.ClassRoom.Id);
        Assert.NotNull(card.Grade);
        Assert.Equal(92, card.Grade.Scores[SubjectCode.Korean].Score);
        Assert.Equal('A', card.Grade.Scores[SubjectCode.English].Rank);
        Assert.Equal([21, 19, 20, null, 18], card.Grade.MonthlyAttendance.ToArray());
        Assert.Equal(3, snapshot.Tables.StudentTable!.Records.Count(x => x.ClassRoomId.Value == 101));
        Assert.Equal(2, snapshot.Views.TeacherScheduleView.Get(new TeacherRecord.TeacherId(7)).Count);

        var students = snapshot.Tables.StudentTable;
        Assert.Equal(6, students.Records.Length);
        Assert.Null(students.Get(new StudentRecord.StudentId(20240308)).ClubId);
        Assert.Equal("하늘이", students.Get(new StudentRecord.StudentId(20260101)).Nickname);
        Assert.Equal(4000123412341234UL, students.Get(new StudentRecord.StudentId(20260101)).CardNo);
        Assert.True(students.Get(new StudentRecord.StudentId(20250215)).IsDormitory);
        Assert.Equal(new DateOnly(2010, 4, 17), students.Get(new StudentRecord.StudentId(20260101)).BirthDate);

        var teacher = snapshot.Tables.TeacherTable!.Get(new TeacherRecord.TeacherId(7));
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

        Assert.Equal(3, snapshot.Tables.LoanTable!.Records.Length);
        var secondLoan = snapshot.Tables.LoanTable.Records[1];
        Assert.Equal(Guid.Parse("{3f2504e0-4f89-11d3-9a0c-0305e82c3301}"), secondLoan.ReceiptId);
        Assert.Empty(snapshot.Tables.ReservationTable!.Records);
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

        await rpg.LoadAsync(fixture.CsvMain);

        var tables = rpg.Current;
        var monsters = tables.MonsterTable!;
        Assert.Null(monsters.Get(MonsterId.Slime).Weakness[1]);
        Assert.Equal([SkillId.Fireball, null, SkillId.Heal], monsters.Get(MonsterId.BossDragon).Skills.ToArray());
        Assert.Equal(35.5m, monsters.Get(MonsterId.Slime).Drops[0].Rate);
        Assert.Equal(new MapId(3), monsters.Get(MonsterId.Slime).Spawn.Map);
        Assert.Equal(TimeSpan.FromMinutes(30), monsters.Get(MonsterId.BossDragon).RespawnAt);

        var potionShop = tables.ShopServerTable!.Get(new ItemRecord.ItemId(10001));
        Assert.Null(potionShop.Stock);
        Assert.Equal(new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.FromHours(9)), potionShop.SaleStart);
        Assert.Equal(2, potionShop.Prices.Count);
        Assert.Equal("봄 이벤트", potionShop.Prices[new PriceKey(901, PurchaseKind.Sale)].Note);
        Assert.Null(potionShop.Prices[new PriceKey(901, PurchaseKind.Normal)].Note);
        Assert.Equal(2, tables.ShopClientTable!.Records.Length);

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
        var dragonNest = tables.QuestTable!.Records.Single(x => x.Id == new QuestRecord.QuestId(3));
        Assert.Equal(new NpcId(null), dragonNest.Giver);
        Assert.Equal(2, dragonNest.PrevQuestId);
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
        await rpg.LoadAsync(fixture.CsvMain);
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
        await school.LoadAsync(fixture.CsvMain);
        var view = school.Current.Views.TeacherScheduleView;

        Assert.Equal(2, view.Get(new TeacherRecord.TeacherId(7)).Count);
        Assert.Empty(view.Get(new TeacherRecord.TeacherId(99)));
    }

    [Fact]
    public async Task ReportCardView_GetThrowsForUnknownStudent_TryGetReturnsFalse()
    {
        var school = new SchoolStaticData(NullLogger.Instance);
        await school.LoadAsync(fixture.CsvMain);
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

        var first = Task.Run(() => school.LoadAsync(fixture.CsvMain));
        gate.WaitUntilHeld(first);
        var second = await Record.ExceptionAsync(() => school.LoadAsync(fixture.CsvMain));
        gate.Release();
        await first;

        Assert.IsType<InvalidOperationException>(second);
        Assert.NotNull(school.Current);
    }

    [Fact]
    public async Task DisabledGacha_LoadsWithNullGachaTable()
    {
        var rpg = new RpgStaticData(NullLogger.Instance);

        await rpg.LoadAsync(fixture.CsvMain, ["GachaTable"]);

        Assert.Null(rpg.Current.GachaTable);
        Assert.NotNull(rpg.Current.ItemTable);
    }

    [Fact]
    public async Task DisabledTableNameWithWrongCase_IsIgnored()
    {
        var rpg = new RpgStaticData(NullLogger.Instance);

        await rpg.LoadAsync(fixture.CsvMain, ["itemTable"]);

        Assert.Equal(3, rpg.Current.ItemTable.Records.Length);
    }

    [Fact]
    public async Task DisabledCurrencies_FailsFkTargetNotFoundForEveryReferencingTable()
    {
        var exception = await LoadAssert.FailsWithAggregate(Load.Rpg(fixture.CsvMain, ["CurrencyTable"]), LoadAssert.FkFailed);

        var messages = exception.InnerExceptions.Select(x => x.Message).ToList();
        Assert.Contains(messages, x => x.StartsWith("RewardRecord.TargetId", StringComparison.Ordinal));
        Assert.Contains(messages, x => x.StartsWith("AttendanceRecord.RewardId", StringComparison.Ordinal));
        Assert.Contains(messages, x => x.StartsWith("FreeRewardRecord.CurrencyId", StringComparison.Ordinal));
        Assert.All(messages, x => Assert.Contains("CurrencyTable", x, StringComparison.Ordinal));
    }

    [Fact]
    public async Task DisabledNonNullableItems_FailsNonNullableTableDisabled()
    {
        using var culture = CultureScope.Invariant();
        var rpg = new RpgStaticData(NullLogger.Instance);

        var exception = await Record.ExceptionAsync(() => rpg.LoadAsync(fixture.CsvMain, ["ItemTable"]));

        Assert.NotNull(exception);
        LoadAssert.ContainsAll(exception, "'ItemTable' is non-nullable");
    }
}
