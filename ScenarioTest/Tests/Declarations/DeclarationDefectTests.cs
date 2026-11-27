using ScenarioTest.Support;

namespace ScenarioTest.Tests.Declarations;

[Collection(CliCollectionDefinition.Name)]
public class DeclarationDefectTests
{
    [Fact]
    public void SameTypeDeclaredInTwoFiles_FailsWithCompileError()
    {
        using var workspace = new ScenarioWorkspace();
        var schoolTypes = Path.Combine(workspace.RecordsSchool, "SchoolTypes.cs");
        File.Copy(schoolTypes, Path.Combine(workspace.RecordsSchool, "SchoolTypesCopy.cs"));

        var result = workspace.RunSchool();

        ExtractionAssert.FailsWith(result, "CS0101", "already contains a definition for 'Weekday'");
        ExtractionAssert.HasStackTrace(result);
        Assert.False(Directory.Exists(workspace.CsvSchool));
    }
}
