using Microsoft.Extensions.Logging.Abstractions;
using ScenarioTest.Managers;
using ScenarioTest.Support;

namespace ScenarioTest.Tests.LoadFailures;

[Collection(CliCollectionDefinition.Name)]
public class ValidationRuleTests(ExtractedCsvFixture fixture)
{
    private static readonly double[] PercentRates = [0.02, 0.18, 0.80];

    [Fact]
    public async Task GachaRatesAddingUpTo99_999_FailsGachaTableValidate()
    {
        using var culture = CultureScope.Invariant();
        using var csv = fixture.CopyMainCsv();
        CsvDocument.Edit(csv.FileOf("Rpg.Gacha.csv"), x => x.Set("Grade", "R", "Rate", "79.999"));
        var rpg = new RpgStaticData(NullLogger.Instance);

        var exception = await LoadAssert.FailsWithAggregate(() => rpg.LoadAsync(csv.Root), LoadAssert.TablesFailed);

        LoadAssert.ContainsAll(exception, "Gacha rates must add up to 100 but add up to 99.999");
    }

    [Fact]
    public async Task TeacherBookedInTwoClassesAtOnce_FailsTimetableTableValidate()
    {
        using var culture = CultureScope.Invariant();
        using var csv = fixture.CopyMainCsv();
        CsvDocument.Edit(csv.FileOf("School.Timetable.csv"), DoubleBookTeacher7);
        var school = new SchoolStaticData(NullLogger.Instance);

        var exception = await LoadAssert.FailsWithAggregate(() => school.LoadAsync(csv.Root), LoadAssert.TablesFailed);

        LoadAssert.ContainsAll(exception, "Teacher 7 has two classes at Mon period 1.");
    }

    [Fact]
    public async Task OneTeacherHomeroomOfTwoClasses_FailsManagerValidate()
    {
        using var culture = CultureScope.Invariant();
        using var csv = fixture.CopyMainCsv();
        CsvDocument.Edit(csv.FileOf("School.ClassRooms.csv"), x => x.Set("Id", "102", "HomeroomTeacherId", "7"));
        var school = new SchoolStaticData(NullLogger.Instance);

        var exception = await Record.ExceptionAsync(() => school.LoadAsync(csv.Root));

        var invalid = Assert.IsType<InvalidOperationException>(exception);
        Assert.Equal("Teacher 7 is the homeroom teacher of more than one class.", invalid.Message);
    }

    [Fact]
    public async Task TwoStudentsInSameSeatOfSameClass_FailsManagerValidate()
    {
        using var culture = CultureScope.Invariant();
        using var csv = fixture.CopyMainCsv();
        CsvDocument.Edit(csv.FileOf("School.Students.csv"), x =>
        {
            x.Set("Id", "20260104", "ClassRoomId", "101");
            x.Set("Id", "20260104", "Seat.Col", "3");
        });
        var school = new SchoolStaticData(NullLogger.Instance);

        var exception = await Record.ExceptionAsync(() => school.LoadAsync(csv.Root));

        var invalid = Assert.IsType<InvalidOperationException>(exception);
        Assert.Equal("Two students share seat (1, 3) in class 101.", invalid.Message);
    }

    [Fact]
    public async Task TeacherWithoutAnyClass_FailsTeacherScheduleView()
    {
        using var culture = CultureScope.Invariant();
        using var csv = fixture.CopyMainCsv();
        CsvDocument.Edit(csv.FileOf("School.Teachers.csv"), AddIdleTeacher);
        var school = new SchoolStaticData(NullLogger.Instance);

        var exception = await LoadAssert.FailsWithAggregate(() => school.LoadAsync(csv.Root), LoadAssert.ViewsFailed);

        LoadAssert.ContainsAll(Assert.Single(exception.InnerExceptions), "Teachers without any class: 최민호");
    }

    [Fact]
    public async Task TwoTableValidateFailures_AreAggregated_AndCurrentKeepsPreviousSnapshot()
    {
        using var culture = CultureScope.Invariant();
        var school = await LoadedSchool();
        var previous = school.Current;
        using var csv = fixture.CopyMainCsv();
        CsvDocument.Edit(csv.FileOf("School.Timetable.csv"), DoubleBookTeacher7);
        CsvDocument.Edit(csv.FileOf("School.Calendar.csv"), x => x.Set("Id", "4", "End", "2026-09-23"));

        var exception = await LoadAssert.FailsWithAggregate(() => school.LoadAsync(csv.Root), LoadAssert.TablesFailed);

        Assert.Equal(2, exception.InnerExceptions.Count);
        Assert.Same(previous, school.Current);
    }

    [Fact]
    public async Task ThreeForeignKeyMistakes_AreAggregated_AndCurrentKeepsPreviousSnapshot()
    {
        using var culture = CultureScope.Invariant();
        var school = await LoadedSchool();
        var previous = school.Current;
        using var csv = fixture.CopyMainCsv();
        CsvDocument.Edit(csv.FileOf("School.Students.csv"), x =>
        {
            x.Set("Id", "20260101", "ClassRoomId", "399");
            x.Set("Id", "20260102", "ClubId", "9");
        });
        CsvDocument.Edit(csv.FileOf("Library.Loans.csv"), x => x.Rows[2][x.Column("Borrower")] = "20250999");

        var exception = await LoadAssert.FailsWithAggregate(() => school.LoadAsync(csv.Root), LoadAssert.FkFailed);

        Assert.Equal(3, exception.InnerExceptions.Count);
        Assert.Same(previous, school.Current);
    }

    [Fact]
    public async Task TwoViewFailures_AreAggregated_AndCurrentKeepsPreviousSnapshot()
    {
        using var culture = CultureScope.Invariant();
        var school = await LoadedSchool();
        var previous = school.Current;
        using var csv = fixture.CopyMainCsv();
        CsvDocument.Edit(csv.FileOf("School.Teachers.csv"), AddIdleTeacher);
        CsvDocument.Edit(csv.FileOf("School.Grades.csv"), x => x.DuplicateRow("StudentId.Value", "20260101"));

        var exception = await LoadAssert.FailsWithAggregate(() => school.LoadAsync(csv.Root), LoadAssert.ViewsFailed);

        Assert.Equal(2, exception.InnerExceptions.Count);
        Assert.Same(previous, school.Current);
    }

    [Fact]
    public async Task GachaRatesTypedAsPercentCells_WarnOnExtract_AndFailGachaValidateOnLoad()
    {
        using var culture = CultureScope.Invariant();
        using var workspace = new ScenarioWorkspace();
        workspace.Mutate("Rpg", "Gacha", sheet =>
        {
            var rateColumn = SheetCells.Header(sheet, "Rate").Address.ColumnNumber;
            for (var i = 0; i < PercentRates.Length; i++)
            {
                var cell = sheet.Cell(SheetCells.TitledHeaderRow + 1 + i, rateColumn);
                cell.Value = PercentRates[i];
                cell.Style.NumberFormat.Format = "0.000%";
            }
        });

        var result = workspace.RunMain();

        Assert.Equal(0, result.ExitCode);
        var warning = Assert.Single(ExtractionAssert.WarningsExceptLoansStartCell(result));
        Assert.Contains("0.000%", warning, StringComparison.Ordinal);
        var rpg = new RpgStaticData(NullLogger.Instance);
        var exception = await LoadAssert.FailsWithAggregate(() => rpg.LoadAsync(workspace.CsvMain), LoadAssert.TablesFailed);
        LoadAssert.ContainsAll(exception, "Gacha rates must add up to 100 but add up to 1");
    }

    private static void DoubleBookTeacher7(CsvDocument timetable)
    {
        var row = timetable.Rows.First(x => x[0] == "102" && x[1] == "Mon" && x[2] == "1");
        row[timetable.Column("TeacherId")] = "7";
    }

    private static void AddIdleTeacher(CsvDocument teachers)
    {
        var cells = new string[teachers.Headers.Count];
        Array.Fill(cells, string.Empty);
        cells[teachers.Column("Id")] = "11";
        cells[teachers.Column("Name")] = "최민호";
        cells[teachers.Column("MainSubject")] = "PE";
        teachers.AddRow(cells);
    }

    private async Task<SchoolStaticData> LoadedSchool()
    {
        var school = new SchoolStaticData(NullLogger.Instance);
        await school.LoadAsync(fixture.CsvMain);

        return school;
    }
}
