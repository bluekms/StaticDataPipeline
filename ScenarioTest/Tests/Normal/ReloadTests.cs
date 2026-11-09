using Microsoft.Extensions.Logging.Abstractions;
using ScenarioTest.Managers;
using ScenarioTest.Support;

namespace ScenarioTest.Tests.Normal;

[Collection(CliCollectionDefinition.Name)]
public class ReloadTests
{
    [Fact]
    public async Task TransferStudentAddedInNextVersion_NewSnapshotHasSeven_OldSnapshotKeepsSix()
    {
        using var workspace = new ScenarioWorkspace();
        var firstRun = workspace.RunMain("--version", "2026-1");
        ExtractionAssert.Succeeded(firstRun);
        var school = new SchoolStaticData(NullLogger.Instance);
        await school.LoadAsync(Path.Combine(workspace.CsvMain, "2026-1"));
        var before = school.Current;

        workspace.UseBroken("School/Students/TransferStudentAdded");
        var secondRun = workspace.RunMain("--version", "2026-2");
        ExtractionAssert.Succeeded(secondRun);
        await school.LoadAsync(Path.Combine(workspace.CsvMain, "2026-2"));

        Assert.Equal(7, school.Current.Tables.StudentTable!.Records.Length);
        Assert.Equal(6, before.Tables.StudentTable!.Records.Length);
        Assert.NotSame(before, school.Current);
    }
}
