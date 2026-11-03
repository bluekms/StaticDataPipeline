using ScenarioTest.Support;

namespace ScenarioTest.Tests.ExtractionFailures;

[Collection(CliCollectionDefinition.Name)]
public class CellValueMistakeTests
{
    [Theory]
    [InlineData("female")]
    [InlineData("1")]
    public void StudentGenderNotEnumMemberName_FailsWithEnumMismatch(string gender)
    {
        using var workspace = new ScenarioWorkspace();
        workspace.Mutate("School", "Students", sheet => SheetCells.Find(sheet, "20260102", "Gender").Value = gender);

        var result = workspace.RunMain();

        ExtractionAssert.FailsWith(result, FormattableString.Invariant($"Invalid value '{gender}'"), "Gender");
    }

    [Theory]
    [InlineData("Grade", 4, "Value(4) must be between 1 and 3")]
    [InlineData("Floor", -2, "Value(-2) must be between -1 and 5")]
    public void ClassRoomOutOfRange_FailsWithRange(string header, int value, string expected)
    {
        using var workspace = new ScenarioWorkspace();
        workspace.Mutate("School", "ClassRooms", sheet => SheetCells.Find(sheet, "301", header).Value = value);

        var result = workspace.RunMain();

        ExtractionAssert.FailsWith(result, expected);
    }

    [Fact]
    public void StudentPhoneMissingOneDigit_FailsWithPatternMismatch()
    {
        using var workspace = new ScenarioWorkspace();
        workspace.Mutate(
            "School",
            "Students",
            sheet => SheetCells.Find(sheet, "20260101", "Phone").Value = "010-1234-567");

        var result = workspace.RunMain();

        ExtractionAssert.FailsWith(result, "Invalid value '010-1234-567'", "Expected to match pattern");
    }

    [Fact]
    public void CafeteriaMenuWithDoubleComma_FailsWithEmptyItem()
    {
        using var workspace = new ScenarioWorkspace();
        workspace.Mutate("School", "Cafeteria", sheet => sheet.Cell("D4").Value = "현미밥,,깍두기");

        var result = workspace.RunMain();

        ExtractionAssert.FailsWith(result, "contains an empty value");
    }

    [Fact]
    public void CafeteriaMenuLeftBlank_FailsWithCountRange()
    {
        using var workspace = new ScenarioWorkspace();
        workspace.Mutate("School", "Cafeteria", sheet => sheet.Cell("D5").Clear());

        var result = workspace.RunMain();

        ExtractionAssert.FailsWith(result, "contains 0 item(s)", "between 1 and 8");
    }

    [Fact]
    public void GradesAttendanceWithFourMonths_FailsWithCountRange()
    {
        using var workspace = new ScenarioWorkspace();
        workspace.Mutate(
            "School",
            "Grades",
            sheet => SheetCells.Find(sheet, "20260102", "출석일수(3~7월)").Value = "21,19,20,18");

        var result = workspace.RunMain();

        ExtractionAssert.FailsWith(result, "contains 4 item(s)", "between 5 and 5");
    }

    [Theory]
    [InlineData("1,000")]
    [InlineData("150.0")]
    public void ItemPriceTypedAsFormattedText_FailsAsNotUnsignedInteger(string price)
    {
        using var workspace = new ScenarioWorkspace();
        workspace.Mutate("Rpg", "Items", sheet => SheetCells.Find(sheet, "10001", "Price").Value = price);

        var result = workspace.RunMain();

        ExtractionAssert.FailsWith(result, FormattableString.Invariant($"'{price}'"));
    }

    [Fact]
    public void ItemIdOutsideItemBand_FailsWithBrandingInnerRange()
    {
        using var workspace = new ScenarioWorkspace();
        workspace.Mutate("Rpg", "Items", sheet => SheetCells.Find(sheet, "10003", "Id").Value = 99999);

        var result = workspace.RunMain();

        ExtractionAssert.FailsWith(result, "Value(99999) must be between 10000 and 19999");
    }

    [Fact]
    public void ClubMeetingDayNotDayOfWeekMemberName_FailsWithEnumMismatch()
    {
        using var workspace = new ScenarioWorkspace();
        workspace.Mutate("School", "Clubs", sheet => SheetCells.Find(sheet, "3", "MeetingDays").Value = "Tuesday / Funday");

        var result = workspace.RunMain();

        ExtractionAssert.FailsWith(result, "'Funday'");
    }

    [Fact]
    public void MonsterSkillNotDeclaredInEnum_FailsWithEnumMismatch()
    {
        using var workspace = new ScenarioWorkspace();
        workspace.Mutate(
            "Rpg",
            "Monsters",
            sheet => SheetCells.Find(sheet, "BossDragon", "Skills").Value = "Fireball,-,Lightning");

        var result = workspace.RunMain();

        ExtractionAssert.FailsWith(result, "'Lightning'");
    }

    [Fact]
    public void CalendarStartTypedAsSlashText_FailsWithDateFormat()
    {
        using var workspace = new ScenarioWorkspace();
        workspace.Mutate("School", "Calendar", sheet => SheetCells.Find(sheet, "1", "Start").Value = "2026/03/02");

        var result = workspace.RunMain();

        ExtractionAssert.FailsWith(result, "'2026/03/02'");
    }

    [Fact]
    public void TimetableStartTimeWithoutLeadingZero_FailsWithTimeFormat()
    {
        using var workspace = new ScenarioWorkspace();
        workspace.Mutate("School", "Timetable", sheet => sheet.Cell("E4").Value = "8:40");

        var result = workspace.RunMain();

        ExtractionAssert.FailsWith(result, "'8:40'");
    }

    [Fact]
    public void TimetableStartTimeAfterSchool_FailsWithRange()
    {
        using var workspace = new ScenarioWorkspace();
        workspace.Mutate("School", "Timetable", sheet => sheet.Cell("E4").Value = "18:00");

        var result = workspace.RunMain();

        ExtractionAssert.FailsWith(result, "Value(18:00) must be between 08:00 and 17:00");
    }

    [Fact]
    public void TimetableStartTimeCellWithSeconds_FailsShowingTheSeconds()
    {
        using var workspace = new ScenarioWorkspace();
        workspace.Mutate("School", "Timetable", sheet => CellWriter.Write(sheet.Cell("E4"), new TimeOnly(8, 40, 30)));

        var result = workspace.RunMain();

        ExtractionAssert.FailsWith(result, "1899-12-31 08:40:30");
    }

    [Fact]
    public void StudentBirthDateCellWithTime_FailsShowingTheTime()
    {
        using var workspace = new ScenarioWorkspace();
        workspace.Mutate(
            "School",
            "Students",
            sheet => SheetCells.Find(sheet, "20260101", "BirthDate").Value = new DateTime(2010, 4, 17, 10, 30, 0));

        var result = workspace.RunMain();

        ExtractionAssert.FailsWith(result, "2010-04-17 10:30:00");
    }

    [Fact]
    public void ShopSaleStartWithoutOffset_FailsWithDateFormat()
    {
        using var workspace = new ScenarioWorkspace();
        workspace.Mutate(
            "Rpg",
            "Shop",
            sheet => SheetCells.Find(sheet, "10001", "SaleStart").Value = "2026-03-01 00:00:00");

        var result = workspace.RunMain();

        ExtractionAssert.FailsWith(result, "'2026-03-01 00:00:00'");
    }

    [Fact]
    public void LoanReceiptIdNotGuid_FailsWithGuidMismatch()
    {
        using var workspace = new ScenarioWorkspace();
        workspace.Mutate(
            "Library",
            "Loans",
            sheet => SheetCells.Find(sheet, "2", "ReceiptId", headerRow: 1).Value = "not-a-guid");

        var result = workspace.RunMain();

        ExtractionAssert.FailsWith(result, "'not-a-guid'");
    }

    [Fact]
    public void CalendarStartDateCellWithTime_FailsShowingTheTime()
    {
        using var workspace = new ScenarioWorkspace();
        workspace.Mutate(
            "School",
            "Calendar",
            sheet => SheetCells.Find(sheet, "1", "Start").Value = new DateTime(2026, 3, 2, 10, 0, 0));

        var result = workspace.RunMain();

        ExtractionAssert.FailsWith(result, "2026-03-02 10:00:00");
    }

    [Fact]
    public void GradeRankBeyondE_FailsWithCharRange()
    {
        using var workspace = new ScenarioWorkspace();
        workspace.Mutate("School", "Grades", sheet => SheetCells.Find(sheet, "20260101", "Scores[0].Rank").Value = "F");

        var result = workspace.RunMain();

        ExtractionAssert.FailsWith(result, "Value(F) must be between A and E");
    }

    [Fact]
    public void TeacherMainSubjectWithSurroundingSpaces_WarnsOnceAndFailsEnum()
    {
        using var workspace = new ScenarioWorkspace();
        workspace.Mutate("School", "Teachers", sheet => SheetCells.Find(sheet, "7", "MainSubject").Value = " Math ");

        var result = workspace.RunMain();

        ExtractionAssert.FailsWith(result, "Invalid value ' Math '");
        var warning = Assert.Single(ExtractionAssert.WarningsExceptLoansStartCell(result));
        Assert.Contains("' Math '", warning, StringComparison.Ordinal);
    }
}
