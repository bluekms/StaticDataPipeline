using Microsoft.Extensions.Logging.Abstractions;
using ScenarioTest.Managers;
using ScenarioTest.Support;

namespace ScenarioTest.Tests.LoadFailures;

[Collection(CliCollectionDefinition.Name)]
public class IndexMistakeTests(ExtractedCsvFixture fixture)
{
    [Fact]
    public async Task TwoElementsWithSameSymbol_PassExtraction_AndFailSymbolIndexOnLoad()
    {
        using var culture = CultureScope.Invariant();
        using var csv = fixture.CopyScienceCsv();
        CsvDocument.Edit(csv.FileOf("Science.Elements.csv"), x => x.Set("Number", "79", "Symbol", "Fe"));
        var science = new ScienceStaticData(NullLogger.Instance);

        var exception = await LoadAssert.FailsWithAggregate(() => science.LoadAsync(csv.Root), LoadAssert.TablesFailed);

        var inner = Assert.Single(exception.InnerExceptions);
        LoadAssert.ContainsAll(inner, "Duplicate key 'Fe'", "ElementRecord");
    }
}
