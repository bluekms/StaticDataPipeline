using ScenarioTest.Support;

namespace ScenarioTest.Tests.ExtractionFailures;

[Collection(CliCollectionDefinition.Name)]
public class DuplicateKeyMistakeTests
{
    [Fact]
    public void StudentIdCopiedToAnotherStudent_FailsWithPrimaryKeyDuplicate()
    {
        using var workspace = new ScenarioWorkspace();
        workspace.Mutate("School", "Students", sheet => SheetCells.Find(sheet, "20260104", "Id").Value = 20260101);

        var result = workspace.RunMain();

        ExtractionAssert.FailsWith(result, "Primary key 'Id' has duplicate value '20260101'");
    }

    [Fact]
    public void TimetableSameClassDayAndPeriodTwice_FailsWithCompositeKeyDuplicate()
    {
        using var workspace = new ScenarioWorkspace();
        workspace.Mutate("School", "Timetable", sheet => sheet.Cell("D7").Value = 1);

        var result = workspace.RunMain();

        ExtractionAssert.FailsWith(result, "Primary key", "duplicate value");
    }
}
