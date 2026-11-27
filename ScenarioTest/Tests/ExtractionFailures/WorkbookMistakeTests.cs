using ScenarioTest.Support;

namespace ScenarioTest.Tests.ExtractionFailures;

[Collection(CliCollectionDefinition.Name)]
public class WorkbookMistakeTests
{
    [Fact]
    public void ElementsSheetRenamedToSingular_FailsNamingMissingSheetAndSheetsInWorkbook()
    {
        using var workspace = new ScenarioWorkspace();
        workspace.UseBroken("Science/Elements/ElementsSheetRenamed");

        var result = workspace.RunScience();

        ExtractionAssert.FailsWith(
            result,
            "Sheet 'Elements' not found in workbook 'Science'. Sheets in the workbook: Element");
        ExtractionAssert.HasNoStackTrace(result);
        Assert.False(File.Exists(Path.Combine(workspace.CsvScience, "Science.Elements.csv")));
    }

    [Fact]
    public void ScienceWorkbookMissingFromFolder_FailsNamingWorkbook_WithoutCsv()
    {
        using var workspace = new ScenarioWorkspace();
        File.Delete(workspace.WorkbookPath("Science"));

        var result = workspace.RunScience();

        ExtractionAssert.FailsWith(result, "Workbook 'Science' not found.");
        ExtractionAssert.HasNoStackTrace(result);
        Assert.False(File.Exists(Path.Combine(workspace.CsvScience, "Science.Elements.csv")));
    }

    [Fact]
    public void ScienceWorkbookFileNameInLowerCase_FailsNamingTheFileAndCaseRule_WithoutCsv()
    {
        using var workspace = new ScenarioWorkspace();
        var workbookPath = workspace.WorkbookPath("Science");
        var lowerCasePath = Path.Combine(Path.GetDirectoryName(workbookPath)!, "science.xlsx");
        File.Move(workbookPath, lowerCasePath);

        var result = workspace.RunScience();

        var expected = "Workbook name case does not match. Record workbook: 'Science', file: 'science.xlsx'. "
            + "Workbook names are case-sensitive.";
        ExtractionAssert.FailsWith(result, expected);
        ExtractionAssert.HasNoStackTrace(result);
        Assert.False(File.Exists(Path.Combine(workspace.CsvScience, "Science.Elements.csv")));
        Assert.False(File.Exists(Path.Combine(workspace.CsvScience, "science.Elements.csv")));
    }

    [Fact]
    public void SchoolWorkbookFileNameInLowerCase_IsReportedOnceForAllRecordsReadingIt()
    {
        using var workspace = new ScenarioWorkspace();
        var workbookPath = workspace.WorkbookPath("School");
        var lowerCasePath = Path.Combine(Path.GetDirectoryName(workbookPath)!, "school.xlsx");
        File.Move(workbookPath, lowerCasePath);

        var result = workspace.RunSchool();

        var expected = "Workbook name case does not match. Record workbook: 'School', file: 'school.xlsx'. "
            + "Workbook names are case-sensitive.";
        ExtractionAssert.FailsWith(result, expected);
        Assert.Single(result.Errors);
        ExtractionAssert.HasNoStackTrace(result);
    }

    [Fact]
    public void ExcelFolderEmptied_FailsNamingEachMissingWorkbookOnce()
    {
        using var workspace = new ScenarioWorkspace();
        foreach (var file in Directory.GetFiles(workspace.ExcelSchool))
        {
            File.Delete(file);
        }

        var result = workspace.RunSchool();

        ExtractionAssert.FailsWith(result, "Workbook 'School' not found.", "Workbook 'Library' not found.");
        Assert.Equal(2, result.Errors.Count);
        ExtractionAssert.HasNoStackTrace(result);
    }

    [Fact]
    public void ItemsSheetHidden_FailsWithSingleErrorAndNoWarning()
    {
        using var workspace = new ScenarioWorkspace();
        workspace.UseBroken("Rpg/Items/ItemsSheetHidden");

        var result = workspace.RunRpg();

        ExtractionAssert.FailsWith(result, "is hidden", "Items");
        Assert.Single(result.Errors);
        Assert.Empty(result.Warnings);
        Assert.False(File.Exists(Path.Combine(workspace.CsvRpg, "Rpg.Items.csv")));
    }

    [Fact]
    public void ItemPricesComputedByFormulaInTwelveRows_FailsListingTenAddressesAndCount()
    {
        using var workspace = new ScenarioWorkspace();
        workspace.UseBroken("Rpg/Items/ItemPricesByFormula");

        var result = workspace.RunRpg();

        ExtractionAssert.FailsWith(result, "has formulas in cells", "(12 cells)");
        var error = Assert.Single(result.Errors, x => x.Contains("has formulas in cells", StringComparison.Ordinal));
        var start = error.IndexOf("in cells ", StringComparison.Ordinal) + "in cells ".Length;
        var end = error.IndexOf(" (12 cells)", StringComparison.Ordinal);
        Assert.True(end > start, error);
        var addresses = error[start..end];
        Assert.Equal(10, addresses.Split(',').Length);
    }
}
