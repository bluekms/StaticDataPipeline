using ScenarioTest.Support;

namespace ScenarioTest.Tests.ExtractionFailures;

[Collection(CliCollectionDefinition.Name)]
public class HeaderMistakeTests
{
    [Fact]
    public void StudentsPhoneColumnDeleted_FailsWithHeaderNotFound()
    {
        using var workspace = new ScenarioWorkspace();
        workspace.Mutate("School", "Students", sheet => SheetCells.Header(sheet, "Phone").WorksheetColumn().Delete());

        var result = workspace.RunMain();

        ExtractionAssert.FailsWith(result, "Header not found: Phone");
        Assert.False(File.Exists(Path.Combine(workspace.CsvMain, "School.Students.csv")));
    }

    [Fact]
    public void StudentsPhoneHeaderTypedLowercase_FailsWithCaseMismatch()
    {
        using var workspace = new ScenarioWorkspace();
        workspace.Mutate("School", "Students", sheet => SheetCells.Header(sheet, "Phone").Value = "phone");

        var result = workspace.RunMain();

        ExtractionAssert.FailsWith(result, "Record header: 'Phone', sheet header: 'phone'");
    }

    [Fact]
    public void ItemsTagsFirstColumnDeleted_FailsWithIndexedHeadersNotContiguous()
    {
        using var workspace = new ScenarioWorkspace();
        workspace.Mutate("Rpg", "Items", sheet => SheetCells.Header(sheet, "Tags[0]").WorksheetColumn().Delete());

        var result = workspace.RunMain();

        ExtractionAssert.FailsWith(result, "Indexed headers for 'Tags' must run contiguously from [0]");
    }

    [Fact]
    public void GradesHasBothStudentIdAndStudentIdValueHeaders_FailsWithBrandedHeaderBothForms()
    {
        using var workspace = new ScenarioWorkspace();
        workspace.Mutate("School", "Grades", sheet =>
        {
            var lastColumn = sheet.LastColumnUsed()!.ColumnNumber() + 1;
            var valueColumn = SheetCells.Header(sheet, "StudentId.Value").Address.ColumnNumber;
            sheet.Column(valueColumn).CopyTo(sheet.Column(lastColumn));
            sheet.Cell(SheetCells.TitledHeaderRow, lastColumn).Value = "StudentId";
        });

        var result = workspace.RunMain();

        ExtractionAssert.FailsWith(result, "Both 'StudentId' and 'StudentId.Value' exist");
    }

    [Fact]
    public void MonstersSpawnMapHeaderShortenedToSpawn_FailsWithHeaderNotFound()
    {
        using var workspace = new ScenarioWorkspace();
        workspace.Mutate("Rpg", "Monsters", sheet => SheetCells.Header(sheet, "Spawn.Map").Value = "Spawn");

        var result = workspace.RunMain();

        ExtractionAssert.FailsWith(result, "Header not found: Spawn.Map");
    }

    [Fact]
    public void StudentsPhoneColumnCopiedTwice_Fails()
    {
        using var workspace = new ScenarioWorkspace();
        workspace.Mutate("School", "Students", sheet =>
        {
            var lastColumn = sheet.LastColumnUsed()!.ColumnNumber() + 1;
            var phoneColumn = SheetCells.Header(sheet, "Phone").Address.ColumnNumber;
            sheet.Column(phoneColumn).CopyTo(sheet.Column(lastColumn));
            sheet.Cell(SheetCells.RowOf(sheet, "20260101"), lastColumn).Value = "010-9999-0000";
        });

        var result = workspace.RunMain();

        ExtractionAssert.FailsWith(result, "Phone");
    }
}
