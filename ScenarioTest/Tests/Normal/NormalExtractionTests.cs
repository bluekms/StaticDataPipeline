using ScenarioTest.Support;

namespace ScenarioTest.Tests.Normal;

[Collection(CliCollectionDefinition.Name)]
public class NormalExtractionTests(ExtractedCsvFixture fixture)
{
    private static readonly string[] MainCsvFiles =
    [
        "Library.Books.csv",
        "Library.Loans.csv",
        "Library.Reservations.csv",
        "Rpg.Attendance.csv",
        "Rpg.Buffs.csv",
        "Rpg.Currencies.csv",
        "Rpg.FreeRewards.csv",
        "Rpg.Gacha.csv",
        "Rpg.Items.csv",
        "Rpg.Monsters.csv",
        "Rpg.PremiumRewards.csv",
        "Rpg.Quests.csv",
        "Rpg.Rewards.csv",
        "Rpg.Season1Items.csv",
        "Rpg.SeasonPass.csv",
        "Rpg.SeasonRewards.csv",
        "Rpg.Shop.csv",
        "Rpg.Skills.csv",
        "School.Cafeteria.csv",
        "School.Calendar.csv",
        "School.ClassRooms.csv",
        "School.Clubs.csv",
        "School.Grades.csv",
        "School.Students.csv",
        "School.Teachers.csv",
        "School.Timetable.csv",
    ];

    [Fact]
    public void MainWorkbooksWithB3StartCell_MatchGolden_AndOnlyWarnAboutLoansStartCell()
    {
        var result = fixture.MainResult;

        ExtractionAssert.Succeeded(result);
        Assert.Empty(result.Errors);
        var warning = Assert.Single(result.Warnings);
        Assert.Contains("LoanRecord", warning, StringComparison.Ordinal);
        Assert.Contains("'A1'", warning, StringComparison.Ordinal);
        Assert.Contains(
            result.InformationLines,
            x => x.Contains("'Library.Reservations' has no data rows", StringComparison.Ordinal));
        AssertMainCsvMatchesGolden(fixture.CsvMain);
    }

    [Fact]
    public void ScienceWorkbookWithoutStartCellOption_MatchesGolden_WithoutWarnings()
    {
        var result = fixture.ScienceResult;

        ExtractionAssert.Succeeded(result);
        Assert.Empty(result.Errors);
        Assert.Empty(result.Warnings);
        Golden.AssertEquals("Science/Science.Elements.csv", Path.Combine(fixture.CsvScience, "Science.Elements.csv"));
    }

    [Fact]
    public void ExtractedCsv_IsUtf8WithoutBom_AndQuotesMultiLineTitle()
    {
        ExtractionAssert.Succeeded(fixture.MainResult);
        var bytes = File.ReadAllBytes(Path.Combine(fixture.CsvMain, "Library.Books.csv"));
        Assert.False(bytes is [0xEF, 0xBB, 0xBF, ..]);
        var books = CsvDocument.Load(Path.Combine(fixture.CsvMain, "Library.Books.csv"));
        Assert.Equal("도서관의\n밤", books.Row("Id", "3")[books.ColumnIndex("Title")]);
        Assert.Equal("\"나무\" 그리고, 숲", books.Row("Id", "1")[books.ColumnIndex("Title")]);
    }

    [Fact]
    public void SameVersionTwice_FailsWithDirectoryAlreadyHasFiles_UnlessForced()
    {
        using var workspace = new ScenarioWorkspace();

        var first = workspace.RunMain("--version", "2026-1");
        var second = workspace.RunMain("--version", "2026-1");
        var forced = workspace.RunMain("--version", "2026-1", "--force");

        ExtractionAssert.Succeeded(first);
        Assert.Equal(1, second.ExitCode);
        Assert.Contains(second.Errors, x => x.Contains("already exists and contains", StringComparison.Ordinal));
        ExtractionAssert.Succeeded(forced);
    }

    [Fact]
    public void ExcelLockFileOldXlsAndBackupSubfolder_AreIgnored()
    {
        using var workspace = new ScenarioWorkspace();
        File.WriteAllBytes(Path.Combine(workspace.ExcelMain, "~$School.xlsx"), []);
        File.WriteAllBytes(Path.Combine(workspace.ExcelMain, "old.xls"), []);
        var backup = Directory.CreateDirectory(Path.Combine(workspace.ExcelMain, "backup"));
        File.Copy(workspace.WorkbookPath("School"), Path.Combine(backup.FullName, "School.xlsx"));

        var result = workspace.RunMain();

        ExtractionAssert.Succeeded(result);
        AssertMainCsvMatchesGolden(workspace.CsvMain);
    }

    [Fact]
    public void WorkbookKeptOpenInExcel_IsStillExtracted()
    {
        using var workspace = new ScenarioWorkspace();

        // Excel 은 열어 둔 통합 문서를 쓰기 권한과 읽기 공유로 잡는다. FileShare.None 이면 어떤 프로세스도 읽을 수 없다.
        CliResult result;
        using (new FileStream(workspace.WorkbookPath("School"), FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
        {
            result = workspace.RunMain();
        }

        Assert.True(result.ExitCode == 0, result.AllText);
        AssertMainCsvMatchesGolden(workspace.CsvMain);
    }

    [Fact]
    public void FormulaAndPercentFormatInMemoColumn_AreNotReported()
    {
        using var workspace = new ScenarioWorkspace();
        workspace.UseBroken("School/Students/MemoColumnFormulaAndPercent");

        var result = workspace.RunMain();

        ExtractionAssert.Succeeded(result);
        var warning = Assert.Single(result.Warnings);
        Assert.Contains("LoanRecord", warning, StringComparison.Ordinal);
        Golden.AssertEquals("School/School.Students.csv", Path.Combine(workspace.CsvMain, "School.Students.csv"));
    }

    private static void AssertMainCsvMatchesGolden(string csvDirectory)
    {
        var actualFiles = Directory.GetFiles(csvDirectory, "*.csv")
            .Select(Path.GetFileName)
            .Order(StringComparer.Ordinal)
            .ToList();
        Assert.Equal(MainCsvFiles, actualFiles);

        foreach (var file in MainCsvFiles)
        {
            var workbook = file[..file.IndexOf('.', StringComparison.Ordinal)];
            Golden.AssertEquals(Path.Combine(workbook, file), Path.Combine(csvDirectory, file));
        }
    }
}
