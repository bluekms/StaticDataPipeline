using ScenarioTest.Support;

namespace ScenarioTest.Tests.ExtractionFailures;

[Collection(CliCollectionDefinition.Name)]
public class WorkbookMistakeTests
{
    [Fact]
    public void ElementsSheetRenamedToSingular_FailsWithSheetNotFound()
    {
        using var workspace = new ScenarioWorkspace();
        workspace.UseBroken("Science/Elements/ElementsSheetRenamed");

        var result = workspace.RunScience();

        ExtractionAssert.FailsWith(result, "Elements");
        Assert.False(File.Exists(Path.Combine(workspace.CsvScience, "Science.Elements.csv")));
    }

    [Fact]
    public void ScienceWorkbookMissingFromFolder_FailsWithoutCsv()
    {
        using var workspace = new ScenarioWorkspace();
        File.Delete(workspace.WorkbookPath("Science"));

        var result = workspace.RunScience();

        ExtractionAssert.FailsWith(result);
        Assert.False(File.Exists(Path.Combine(workspace.CsvScience, "Science.Elements.csv")));
    }

    [Fact]
    public void ExcelFolderEmptied_Fails()
    {
        using var workspace = new ScenarioWorkspace();
        foreach (var file in Directory.GetFiles(workspace.ExcelMain))
        {
            File.Delete(file);
        }

        var result = workspace.RunMain();

        ExtractionAssert.FailsWith(result);
    }

    [Fact]
    public void ItemsSheetHidden_FailsWithSingleErrorAndNoWarning()
    {
        using var workspace = new ScenarioWorkspace();
        workspace.UseBroken("Rpg/Items/ItemsSheetHidden");

        var result = workspace.RunMain();

        ExtractionAssert.FailsWith(result, "is hidden", "Items");
        Assert.Single(result.Errors);
        Assert.Empty(ExtractionAssert.WarningsExceptLoansStartCell(result));
        Assert.False(File.Exists(Path.Combine(workspace.CsvMain, "Rpg.Items.csv")));
    }

    [Fact]
    public void ItemPricesComputedByFormulaInTwelveRows_FailsListingTenAddressesAndCount()
    {
        using var workspace = new ScenarioWorkspace();
        workspace.UseBroken("Rpg/Items/ItemPricesByFormula");

        var result = workspace.RunMain();

        ExtractionAssert.FailsWith(result, "has formulas in cells", "(12 cells)");
        var error = Assert.Single(result.Errors, x => x.Contains("has formulas in cells", StringComparison.Ordinal));
        var start = error.IndexOf("in cells ", StringComparison.Ordinal) + "in cells ".Length;
        var end = error.IndexOf(" (12 cells)", StringComparison.Ordinal);
        Assert.True(end > start, error);
        var addresses = error[start..end];
        Assert.Equal(10, addresses.Split(',').Length);
    }
}
