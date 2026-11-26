using ScenarioTest.Support;

namespace ScenarioTest.Tests.Normal;

[Collection(CliCollectionDefinition.Name)]
public class NormalExtractionTests(ExtractedCsvFixture fixture)
{
    private static readonly string[] SchoolCsvFiles =
    [
        "Library.BookTalks.csv",
        "Library.Books.csv",
        "Library.Recommendations.csv",
        "School.Cafeteria.csv",
        "School.Calendar.csv",
        "School.ClassRooms.csv",
        "School.Clubs.csv",
        "School.Grades.csv",
        "School.Students.csv",
        "School.Teachers.csv",
        "School.Timetable.csv",
    ];

    private static readonly string[] RpgCsvFiles =
    [
        "Rpg.Attendance.csv",
        "Rpg.Buffs.csv",
        "Rpg.Currencies.csv",
        "Rpg.FreeRewards.csv",
        "Rpg.Gacha.csv",
        "Rpg.Items.csv",
        "Rpg.Monsters.csv",
        "Rpg.OldSeasonItems.csv",
        "Rpg.PremiumRewards.csv",
        "Rpg.Quests.csv",
        "Rpg.Rewards.csv",
        "Rpg.SeasonItems.csv",
        "Rpg.SeasonPass.csv",
        "Rpg.SeasonRewards.csv",
        "Rpg.Shop.csv",
        "Rpg.Skills.csv",
        "Rpg.Stages.csv",
    ];

    [Fact]
    public void SchoolAndLibraryWorkbooksWithB3StartCell_MatchGolden_WithoutWarnings()
    {
        var result = fixture.SchoolResult;

        ExtractionAssert.Succeeded(result);
        Assert.Empty(result.Errors);
        Assert.Empty(result.Warnings);
        Assert.Contains(
            result.InformationLines,
            x => x.Contains("'Library.Recommendations' has no data rows", StringComparison.Ordinal));
        AssertCsvMatchesGolden("School", SchoolCsvFiles, fixture.CsvSchool);
    }

    [Fact]
    public void RpgWorkbookWithB3StartCell_MatchesGolden_WithoutWarnings()
    {
        var result = fixture.RpgResult;

        ExtractionAssert.Succeeded(result);
        Assert.Empty(result.Errors);
        Assert.Empty(result.Warnings);
        AssertCsvMatchesGolden("Rpg", RpgCsvFiles, fixture.CsvRpg);
    }

    [Fact]
    public void ScienceWorkbookWithoutStartCellOption_MatchesGolden_WithoutWarnings()
    {
        var result = fixture.ScienceResult;

        ExtractionAssert.Succeeded(result);
        Assert.Empty(result.Errors);
        Assert.Empty(result.Warnings);
        var elementsCsv = Path.Combine(fixture.CsvScience, "Science.Elements.csv");
        Golden.AssertEquals("Science", "Science.Elements.csv", elementsCsv);
    }

    [Fact]
    public void ExtractedCsv_IsUtf8WithoutBom_AndQuotesMultiLineTitle()
    {
        ExtractionAssert.Succeeded(fixture.SchoolResult);
        var bytes = File.ReadAllBytes(Path.Combine(fixture.CsvSchool, "Library.Books.csv"));
        Assert.False(bytes is [0xEF, 0xBB, 0xBF, ..]);
        var books = CsvDocument.Load(Path.Combine(fixture.CsvSchool, "Library.Books.csv"));
        Assert.Equal("도서관의\n밤", books.Row("Id", "3")[books.ColumnIndex("Title")]);
        Assert.Equal("\"나무\" 그리고, 숲", books.Row("Id", "1")[books.ColumnIndex("Title")]);
    }

    [Fact]
    public void SameVersionTwice_FailsWithDirectoryAlreadyHasFiles_UnlessForced()
    {
        using var workspace = new ScenarioWorkspace();

        var first = workspace.RunSchool("--version", "2026-1");
        var second = workspace.RunSchool("--version", "2026-1");
        var forced = workspace.RunSchool("--version", "2026-1", "--force");

        ExtractionAssert.Succeeded(first);
        Assert.Equal(1, second.ExitCode);
        Assert.Contains(second.Errors, x => x.Contains("already exists and contains", StringComparison.Ordinal));
        ExtractionAssert.Succeeded(forced);
    }

    [Fact]
    public void ExcelLockFileOldXlsAndBackupSubfolder_AreIgnored()
    {
        using var workspace = new ScenarioWorkspace();
        File.WriteAllBytes(Path.Combine(workspace.ExcelSchool, "~$School.xlsx"), []);
        File.WriteAllBytes(Path.Combine(workspace.ExcelSchool, "old.xls"), []);
        var backup = Directory.CreateDirectory(Path.Combine(workspace.ExcelSchool, "backup"));
        File.Copy(workspace.WorkbookPath("School"), Path.Combine(backup.FullName, "School.xlsx"));

        var result = workspace.RunSchool();

        ExtractionAssert.Succeeded(result);
        AssertCsvMatchesGolden("School", SchoolCsvFiles, workspace.CsvSchool);
    }

    [Fact]
    public void WorkbookKeptOpenInExcel_IsStillExtracted()
    {
        using var workspace = new ScenarioWorkspace();

        // Excel 은 열어 둔 통합 문서를 쓰기 권한과 읽기 공유로 잡는다. FileShare.None 이면 어떤 프로세스도 읽을 수 없다.
        CliResult result;
        using (new FileStream(workspace.WorkbookPath("School"), FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
        {
            result = workspace.RunSchool();
        }

        Assert.True(result.ExitCode == 0, result.AllText);
        AssertCsvMatchesGolden("School", SchoolCsvFiles, workspace.CsvSchool);
    }

    [Fact]
    public void BclEnumWithoutNamespace_IsExtracted_WhenRecordFolderHasGlobalUsing()
    {
        using var workspace = new ScenarioWorkspace();
        var clubRecord = Path.Combine(workspace.RecordsSchool, "ClubRecord.cs");
        var source = File.ReadAllText(clubRecord);
        var sourceWithoutNamespace = source.Replace("System.DayOfWeek", "DayOfWeek", StringComparison.Ordinal);
        Assert.NotEqual(source, sourceWithoutNamespace);
        File.WriteAllText(clubRecord, sourceWithoutNamespace);
        File.WriteAllText(Path.Combine(workspace.RecordsSchool, "GlobalUsings.cs"), "global using System;");

        var result = workspace.RunSchool();

        ExtractionAssert.Succeeded(result);
        AssertCsvMatchesGolden("School", SchoolCsvFiles, workspace.CsvSchool);
    }

    [Fact]
    public void FormulaAndPercentFormatInMemoColumn_AreNotReported()
    {
        using var workspace = new ScenarioWorkspace();
        workspace.UseBroken("School/Students/MemoColumnFormulaAndPercent");

        var result = workspace.RunSchool();

        ExtractionAssert.Succeeded(result);
        Assert.Empty(result.Warnings);
        Golden.AssertEquals("School", "School.Students.csv", Path.Combine(workspace.CsvSchool, "School.Students.csv"));
    }

    private static void AssertCsvMatchesGolden(string feature, string[] expectedFiles, string csvDirectory)
    {
        var actualFiles = Directory.GetFiles(csvDirectory, "*.csv")
            .Select(Path.GetFileName)
            .Order(StringComparer.Ordinal)
            .ToList();
        Assert.Equal(expectedFiles, actualFiles);

        foreach (var file in expectedFiles)
        {
            Golden.AssertEquals(feature, file, Path.Combine(csvDirectory, file));
        }
    }
}
