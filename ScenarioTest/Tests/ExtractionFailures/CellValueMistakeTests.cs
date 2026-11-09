using ScenarioTest.Support;

namespace ScenarioTest.Tests.ExtractionFailures;

[Collection(CliCollectionDefinition.Name)]
public class CellValueMistakeTests
{
    // Excel/Broken 의 워크북(노란 칸이 틀린 자리, 메모가 설명)과 추출 로그에 들어 있어야 하는 조각.
    [Theory]
    [InlineData("School/Students/StudentGenderLowercase", "Invalid value 'female'", "Gender")]
    [InlineData("School/Students/StudentGenderNumber", "Invalid value '1'", "Gender")]
    [InlineData("School/ClassRooms/ClassRoomGradeOutOfRange", "Value(4) must be between 1 and 3")]
    [InlineData("School/ClassRooms/ClassRoomFloorOutOfRange", "Value(-2) must be between -1 and 5")]
    [InlineData("School/Students/StudentPhoneMissingDigit", "Invalid value '010-1234-567'", "Expected to match pattern")]
    [InlineData("School/Grades/GradesAttendanceFourMonths", "contains 4 item(s)", "between 5 and 5")]
    [InlineData("School/Grades/GradeRankBeyondE", "Value(F) must be between A and E")]
    [InlineData("School/Clubs/ClubMeetingDayNotDayOfWeek", "'Funday'")]
    [InlineData("School/Calendar/CalendarStartSlashText", "'2026/03/02'")]
    [InlineData("School/Timetable/TimetableStartTimeNoLeadingZero", "'8:40'")]
    [InlineData("School/Timetable/TimetableStartTimeAfterSchool", "Value(18:00) must be between 08:00 and 17:00")]
    [InlineData("Rpg/Items/ItemPriceThousandsText", "'1,000'")]
    [InlineData("Rpg/Items/ItemPriceDecimalText", "'150.0'")]
    [InlineData("Rpg/Items/ItemIdOutsideBand", "Value(99999) must be between 10000 and 19999")]
    [InlineData("Rpg/Monsters/MonsterSkillUndeclared", "'Lightning'")]
    [InlineData("Rpg/Shop/ShopSaleStartWithoutOffset", "'2026-03-01 00:00:00'")]
    [InlineData("School/Cafeteria/CafeteriaMenuDoubleComma", "contains an empty value")]
    [InlineData("School/Cafeteria/CafeteriaMenuBlank", "contains 0 item(s)", "between 1 and 8")]
    [InlineData("School/Timetable/TimetableStartTimeWithSeconds", "1899-12-31 08:40:30")]
    [InlineData("School/Students/StudentBirthDateWithTime", "2010-04-17 10:30:00")]
    [InlineData("Library/Loans/LoanReceiptIdNotGuid", "'not-a-guid'")]
    public void WrongCellValue_FailsExtractionNamingTheValue(string brokenWorkbook, params string[] expected)
    {
        using var workspace = new ScenarioWorkspace();
        workspace.UseBroken(brokenWorkbook);

        ExtractionAssert.FailsWith(workspace.RunMain(), expected);
    }

    [Fact]
    public void TeacherMainSubjectWithSurroundingSpaces_WarnsOnceAndFailsEnum()
    {
        using var workspace = new ScenarioWorkspace();
        workspace.UseBroken("School/Teachers/TeacherSubjectWithSpaces");

        var result = workspace.RunMain();

        ExtractionAssert.FailsWith(result, "Invalid value ' Math '");
        var warning = Assert.Single(ExtractionAssert.WarningsExceptLoansStartCell(result));
        Assert.Contains("' Math '", warning, StringComparison.Ordinal);
    }
}
