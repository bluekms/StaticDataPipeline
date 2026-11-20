using ScenarioTest.Support;

namespace ScenarioTest.Tests.LoadFailures;

[Collection(CliCollectionDefinition.Name)]
public class CsvTamperingTests(ExtractedCsvFixture fixture)
{
    [Fact]
    public async Task StudentsCsvEmptied_FailsGradeForeignKeysAgainstEmptyTable()
    {
        using var csv = fixture.CopySchoolCsv();
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
