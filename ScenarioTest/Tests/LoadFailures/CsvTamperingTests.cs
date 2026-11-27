using ScenarioTest.Support;

namespace ScenarioTest.Tests.LoadFailures;

[Collection(CliCollectionDefinition.Name)]
public class CsvTamperingTests(ExtractedCsvFixture fixture)
{
    [Fact]
    public async Task QuestKillTargetHandEditedToUnknownMonster_FailsNamingFileRowAndColumn()
    {
        using var csv = fixture.CopyRpgCsv();
        csv.Edit("Rpg.Quests.csv", x => x.Set("Id", "2", "Kill.Monster", "Orc"));

        await LoadAssert.FailsWithSingle(
            Load.Rpg(csv.Root),
            LoadAssert.TablesFailed,
            "[Rpg.Quests.csv] CSV row 3 (QuestRecord)",
            "'Orc' is not a defined member of",
            "MonsterId",
            "column 'Kill.Monster'");
    }

    [Fact]
    public async Task StudentsCsvEmptied_FailsGradeForeignKeysAgainstEmptyTable()
    {
        using var csv = fixture.CopySchoolCsv();
        csv.Edit("School.Students.csv", x => x.Rows.Clear());

        var exception = await LoadAssert.FailsWithAggregate(Load.School(csv.Root), LoadAssert.FkFailed);

        LoadAssert.ContainsAll(exception, "GradeRecord", "StudentTable.Id");
    }

    [Fact]
    public async Task GradeRowCopiedForSameStudent_FailsKeyDuplicateOfTableWithoutIndex()
    {
        using var csv = fixture.CopySchoolCsv();
        csv.Edit("School.Grades.csv", x => x.DuplicateRow("StudentId.Value", "20260101"));

        await LoadAssert.FailsWithSingle(
            Load.School(csv.Root),
            LoadAssert.TablesFailed,
            "[School.Grades.csv] Duplicate key '20260101' in GradeRecord.");
    }

    [Fact]
    public async Task ItemRowCopied_FailsKeyDuplicateOnce_EvenThoughItemTableHasItsOwnIndex()
    {
        using var csv = fixture.CopyRpgCsv();
        csv.Edit("Rpg.Items.csv", x => x.DuplicateRow("Id", "10001"));

        await LoadAssert.FailsWithSingle(
            Load.Rpg(csv.Root),
            LoadAssert.TablesFailed,
            "[Rpg.Items.csv] Duplicate key '10001' in ItemRecord.");
    }

    [Fact]
    public async Task TwoElementsWithSameSymbol_FailSymbolIndexOnLoad()
    {
        using var csv = fixture.CopyScienceCsv();
        csv.Edit("Science.Elements.csv", x => x.Set("Number", "79", "Symbol", "Fe"));

        await LoadAssert.FailsWithSingle(
            Load.Science(csv.Root),
            LoadAssert.TablesFailed,
            "Duplicate key 'Fe'",
            "ElementRecord");
    }
}
