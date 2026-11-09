using ScenarioTest.Support;

namespace ScenarioTest.Tests.ExtractionFailures;

[Collection(CliCollectionDefinition.Name)]
public class HeaderMistakeTests
{
    [Theory]
    [InlineData("School/Students/PhoneHeaderLowercase", "Record header: 'Phone', sheet header: 'phone'")]
    [InlineData("Rpg/Items/TagsFirstColumnDeleted", "Indexed headers for 'Tags' must run contiguously from [0]")]
    [InlineData("School/Grades/GradesBothStudentIdForms", "Both 'StudentId' and 'StudentId.Value' exist")]
    [InlineData("Rpg/Monsters/SpawnMapHeaderShortened", "Header not found: Spawn.Map")]
    [InlineData("School/Students/PhoneColumnCopiedTwice", "Phone")]
    public void WrongHeader_FailsExtraction(string brokenWorkbook, params string[] expected)
    {
        using var workspace = new ScenarioWorkspace();
        workspace.UseBroken(brokenWorkbook);

        ExtractionAssert.FailsWith(workspace.RunMain(), expected);
    }

    [Fact]
    public void StudentsPhoneColumnDeleted_FailsWithHeaderNotFound_AndWritesNoCsv()
    {
        using var workspace = new ScenarioWorkspace();
        workspace.UseBroken("School/Students/PhoneColumnDeleted");

        var result = workspace.RunMain();

        ExtractionAssert.FailsWith(result, "Header not found: Phone");
        Assert.False(File.Exists(Path.Combine(workspace.CsvMain, "School.Students.csv")));
    }
}
