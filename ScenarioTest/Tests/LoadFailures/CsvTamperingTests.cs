using ScenarioTest.Support;

namespace ScenarioTest.Tests.LoadFailures;

[Collection(CliCollectionDefinition.Name)]
public class CsvTamperingTests(ExtractedCsvFixture fixture)
{
    [Fact]
    public async Task GradesCsvWithSixthScoreColumns_FailsAsExceedingDeclaredLength()
    {
        using var csv = fixture.CopyMainCsv();
        csv.Edit("School.Grades.csv", x =>
        {
            x.AddColumn("Scores[5].Subject", "Music");
            x.AddColumn("Scores[5].Score", "90");
            x.AddColumn("Scores[5].Rank", "A");
        });

        var exception = await LoadAssert.FailsWithAggregate(Load.School(csv.Root), LoadAssert.TablesFailed);

        LoadAssert.ContainsAll(exception, "exceeds declared length 5");
    }

    [Fact]
    public async Task StudentsCsvWithoutPhoneColumn_FailsWithMissingHeader()
    {
        using var csv = fixture.CopyMainCsv();
        csv.Edit("School.Students.csv", x => x.RemoveColumn("Phone"));

        var exception = await LoadAssert.FailsWithAggregate(Load.School(csv.Root), LoadAssert.TablesFailed);

        LoadAssert.ContainsAll(exception, "Header 'Phone' not found in CSV.");
    }

    [Theory]
    [InlineData("School.Clubs.csv")]
    [InlineData("Rpg.Items.csv")]
    public async Task CsvFileMissing_FailsTableLoadEvenForNullableTable(string csvFile)
    {
        using var csv = fixture.CopyMainCsv();
        File.Delete(csv.FileOf(csvFile));

        var exception = await LoadAssert.FailsWithAggregate(Load.ForCsv(csv.Root, csvFile), LoadAssert.TablesFailed);

        LoadAssert.ContainsAll(exception, csvFile);
    }

    [Fact]
    public async Task MonsterSkillsCsvHandEditedWithUnnamedSkillNumber_FailsEnumParse()
    {
        using var csv = fixture.CopyMainCsv();
        csv.Edit("Rpg.Monsters.csv", x => x.Set("Id", "BossDragon", "Skills", "Fireball,-,1003"));

        var exception = await LoadAssert.FailsWithAggregate(Load.Rpg(csv.Root), LoadAssert.TablesFailed);

        LoadAssert.ContainsAll(exception, "'1003'", "SkillId");
    }

    [Fact]
    public async Task StudentsCsvEmptied_FailsGradeForeignKeysAgainstEmptyTable()
    {
        using var csv = fixture.CopyMainCsv();
        csv.Edit("School.Students.csv", x => x.Rows.Clear());

        var exception = await LoadAssert.FailsWithAggregate(Load.School(csv.Root), LoadAssert.FkFailed);

        LoadAssert.ContainsAll(exception, "GradeRecord", "StudentTable.Id");
    }

    [Fact]
    public async Task TwoElementsWithSameSymbol_PassExtraction_AndFailSymbolIndexOnLoad()
    {
        using var csv = fixture.CopyScienceCsv();
        csv.Edit("Science.Elements.csv", x => x.Set("Number", "79", "Symbol", "Fe"));

        await LoadAssert.FailsWithSingle(Load.Science(csv.Root), LoadAssert.TablesFailed, "Duplicate key 'Fe'", "ElementRecord");
    }
}
