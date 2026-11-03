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
        Assert.Equal(0, firstRun.ExitCode);
        var school = new SchoolStaticData(NullLogger.Instance);
        await school.LoadAsync(Path.Combine(workspace.CsvMain, "2026-1"));
        var before = school.Current;

        workspace.Mutate("School", "Students", sheet =>
        {
            var row = SheetCells.LastDataRow(sheet) + 1;
            object?[] cells =
            [
                20260105, "윤하린", "Female", new DateTime(2010, 5, 21), 102, 1, 2, "-", "-",
                "010-4432-1198", "4000123400070007", "false",
            ];
            for (var i = 0; i < cells.Length; i++)
            {
                CellWriter.Write(sheet.Cell(row, SheetCells.TitledStartColumn + i), cells[i]);
            }
        });
        var secondRun = workspace.RunMain("--version", "2026-2");
        Assert.Equal(0, secondRun.ExitCode);
        await school.LoadAsync(Path.Combine(workspace.CsvMain, "2026-2"));

        Assert.Equal(7, school.Current.Tables.StudentTable!.Records.Length);
        Assert.Equal(6, before.Tables.StudentTable!.Records.Length);
        Assert.NotSame(before, school.Current);
    }
}
