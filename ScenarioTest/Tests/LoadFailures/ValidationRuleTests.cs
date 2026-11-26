using Microsoft.Extensions.Logging.Abstractions;
using ScenarioTest.Managers;
using ScenarioTest.Support;

namespace ScenarioTest.Tests.LoadFailures;

[Collection(CliCollectionDefinition.Name)]
public class ValidationRuleTests(ExtractedCsvFixture fixture)
{
    [Fact]
    public async Task GachaRatesAddingUpTo99_999_FailsGachaTableValidate()
    {
        using var csv = fixture.CopyRpgCsv();
        csv.Edit("Rpg.Gacha.csv", x => x.Set("Grade", "R", "Rate", "79.999"));

        var exception = await LoadAssert.FailsWithAggregate(Load.Rpg(csv.Root), LoadAssert.TablesFailed);

        LoadAssert.ContainsAll(exception, "가챠 확률의 합은 100 이어야 하는데 99.999");
    }

    [Theory]
    [InlineData("1", "3", "상점 상품 1 의 구매 제한 None 과 제한 수 3 가 맞지 않는다.")]
    [InlineData("2", "0", "상점 상품 2 의 구매 제한 Account 과 제한 수 0 가 맞지 않는다.")]
    public async Task ShopLimitKindAndCountDisagree_FailsShopTableValidate(string id, string limitCount, string expected)
    {
        using var csv = fixture.CopyRpgCsv();
        csv.Edit("Rpg.Shop.csv", x => x.Set("Id", id, "LimitCount", limitCount));

        var exception = await LoadAssert.FailsWithAggregate(Load.Rpg(csv.Root), LoadAssert.TablesFailed);

        LoadAssert.ContainsAll(exception, expected);
    }

    [Fact]
    public async Task StageRequiredLevelOutOfRange_FailsInBothRecordsReadingTheColumn()
    {
        using var csv = fixture.CopyRpgCsv();
        csv.Edit("Rpg.Stages.csv", x => x.Set("Id", "1", "RequiredLevel", "0"));

        var exception = await LoadAssert.FailsWithAggregate(Load.Rpg(csv.Root), LoadAssert.TablesFailed);

        // StageClientRecord 와 StageServerRecord 가 같은 열을 읽어 같은 문구가 두 번 나온다. 문구에 Record 이름은 없다.
        Assert.Equal(2, exception.InnerExceptions.Count);
        foreach (var inner in exception.InnerExceptions)
        {
            LoadAssert.ContainsAll(inner, "[Rpg.Stages.csv] CSV row 2: 'RequiredLevel' must be within [1, 99].");
        }
    }

    [Fact]
    public async Task TeacherBookedInTwoClassesAtOnce_FailsTimetableTableValidate()
    {
        using var csv = fixture.CopySchoolCsv();
        csv.Edit("School.Timetable.csv", DoubleBookTeacher7);

        var exception = await LoadAssert.FailsWithAggregate(Load.School(csv.Root), LoadAssert.TablesFailed);

        LoadAssert.ContainsAll(exception, "교사 7 은 Mon 1 교시에 수업이 둘이다.");
    }

    [Fact]
    public async Task OneTeacherHomeroomOfTwoClasses_FailsManagerValidate()
    {
        using var csv = fixture.CopySchoolCsv();
        csv.Edit("School.ClassRooms.csv", x => x.Set("Id", "102", "HomeroomTeacherId", "7"));

        var exception = await Record.ExceptionAsync(Load.School(csv.Root));

        var invalid = Assert.IsType<InvalidOperationException>(exception);
        Assert.Equal("교사 7 은 둘 이상의 반의 담임이다.", invalid.Message);
    }

    [Fact]
    public async Task TwoStudentsInSameSeatOfSameClass_FailsManagerValidate()
    {
        using var csv = fixture.CopySchoolCsv();
        csv.Edit("School.Students.csv", x =>
        {
            x.Set("Id", "20260104", "ClassRoomId", "101");
            x.Set("Id", "20260104", "Seat.Col", "3");
        });

        var exception = await Record.ExceptionAsync(Load.School(csv.Root));

        var invalid = Assert.IsType<InvalidOperationException>(exception);
        Assert.Equal("101 반의 자리 (1, 3) 에 학생이 둘이다.", invalid.Message);
    }

    [Fact]
    public async Task TeacherWithoutAnyClass_FailsTeacherScheduleView()
    {
        using var csv = fixture.CopySchoolCsv();
        csv.Edit("School.Teachers.csv", AddIdleTeacher);

        await LoadAssert.FailsWithSingle(Load.School(csv.Root), LoadAssert.ViewsFailed, "수업이 없는 교사: 최민호");
    }

    [Fact]
    public async Task GradeTableDisabled_FailsTableLoadBeforeReportCardViewIsBuilt()
    {
        await LoadAssert.FailsWithSingle(
            Load.School(fixture.CsvSchool, ["GradeTable"]),
            LoadAssert.TablesFailed,
            "'GradeTable' is non-nullable");
    }

    [Fact]
    public async Task TwoTableValidateFailures_AreAggregated_AndCurrentKeepsPreviousSnapshot()
    {
        var school = await LoadedSchool();
        var previous = school.Current;
        using var csv = fixture.CopySchoolCsv();
        csv.Edit("School.Timetable.csv", DoubleBookTeacher7);
        csv.Edit("School.Calendar.csv", x => x.Set("Id", "4", "End", "2026-09-23"));

        var exception = await LoadAssert.FailsWithAggregate(() => school.LoadAsync(csv.Root), LoadAssert.TablesFailed);

        Assert.Equal(2, exception.InnerExceptions.Count);
        Assert.Same(previous, school.Current);
    }

    [Fact]
    public async Task ThreeForeignKeyMistakes_AreAggregated_AndCurrentKeepsPreviousSnapshot()
    {
        var school = await LoadedSchool();
        var previous = school.Current;
        using var csv = fixture.CopySchoolCsv();
        csv.Edit("School.Students.csv", x =>
        {
            x.Set("Id", "20260101", "ClassRoomId", "399");
            x.Set("Id", "20260102", "ClubId", "9");
        });
        csv.Edit("Library.BookTalks.csv", x => x.Rows[2][x.ColumnIndex("Presenter")] = "20250999");

        var exception = await LoadAssert.FailsWithAggregate(() => school.LoadAsync(csv.Root), LoadAssert.FkFailed);

        Assert.Equal(3, exception.InnerExceptions.Count);
        Assert.Same(previous, school.Current);
    }

    [Fact]
    public async Task TwoViewFailures_AreAggregated_AndCurrentKeepsPreviousSnapshot()
    {
        var school = await LoadedSchool();
        var previous = school.Current;
        using var csv = fixture.CopySchoolCsv();
        csv.Edit("School.Teachers.csv", AddIdleTeacher);
        csv.Edit("School.Grades.csv", x => x.DuplicateRow("StudentId.Value", "20260101"));

        var exception = await LoadAssert.FailsWithAggregate(() => school.LoadAsync(csv.Root), LoadAssert.ViewsFailed);

        Assert.Equal(2, exception.InnerExceptions.Count);
        Assert.Same(previous, school.Current);
    }

    [Fact]
    public async Task GachaRatesTypedAsPercentCells_WarnOnExtract_AndFailGachaValidateOnLoad()
    {
        using var workspace = new ScenarioWorkspace();
        workspace.UseBroken("Rpg/Gacha/GachaRatesAsPercentCells");

        var result = workspace.RunRpg();

        ExtractionAssert.Succeeded(result);
        var warning = Assert.Single(result.Warnings);
        Assert.Contains("0.000%", warning, StringComparison.Ordinal);
        var exception = await LoadAssert.FailsWithAggregate(Load.Rpg(workspace.CsvRpg), LoadAssert.TablesFailed);
        LoadAssert.ContainsAll(exception, "가챠 확률의 합은 100 이어야 하는데 1");
    }

    private static void DoubleBookTeacher7(CsvDocument timetable)
    {
        var classRoom = timetable.ColumnIndex("Slot.ClassRoomId");
        var day = timetable.ColumnIndex("Slot.Day");
        var period = timetable.ColumnIndex("Slot.Period");
        var row = timetable.Rows.First(x => x[classRoom] == "102" && x[day] == "Mon" && x[period] == "1");
        row[timetable.ColumnIndex("TeacherId")] = "7";
    }

    private static void AddIdleTeacher(CsvDocument teachers)
    {
        var cells = new string[teachers.Headers.Count];
        Array.Fill(cells, string.Empty);
        cells[teachers.ColumnIndex("Id")] = "11";
        cells[teachers.ColumnIndex("Name")] = "최민호";
        cells[teachers.ColumnIndex("MainSubject")] = "PE";
        teachers.AddRow(cells);
    }

    private async Task<SchoolStaticData> LoadedSchool()
    {
        var school = new SchoolStaticData(NullLogger.Instance);
        await school.LoadAsync(fixture.CsvSchool);

        return school;
    }
}
