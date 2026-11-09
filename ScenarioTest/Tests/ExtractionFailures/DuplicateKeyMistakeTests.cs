using ScenarioTest.Support;

namespace ScenarioTest.Tests.ExtractionFailures;

[Collection(CliCollectionDefinition.Name)]
public class DuplicateKeyMistakeTests
{
    [Theory]
    [InlineData("School/Students/StudentIdCopied", "Primary key 'Id' has duplicate value '20260101'")]
    [InlineData("School/Timetable/TimetableSlotDuplicated", "Primary key", "duplicate value")]
    public void DuplicatePrimaryKey_FailsExtraction(string brokenWorkbook, params string[] expected)
    {
        using var workspace = new ScenarioWorkspace();
        workspace.UseBroken(brokenWorkbook);

        ExtractionAssert.FailsWith(workspace.RunMain(), expected);
    }
}
