using ScenarioTest.Support;

namespace ScenarioTest.Tests.ExtractionFailures;

[Collection(CliCollectionDefinition.Name)]
public class WorkbookMistakeTests
{
    [Fact]
    public void ElementsSheetRenamedToSingular_FailsWithSheetNotFound()
    {
        using var workspace = new ScenarioWorkspace();
        workspace.Mutate("Science", "Elements", sheet => sheet.Name = "Element");

        var result = workspace.RunScience();

        ExtractionAssert.FailsWith(result, "Elements");
        Assert.False(File.Exists(Path.Combine(workspace.CsvScience, "Science.Elements.csv")));
    }

    [Fact]
    public void ScienceWorkbookMissingFromFolder_FailsWithoutCsv()
    {
        using var workspace = new ScenarioWorkspace();
        MoveAway(workspace, workspace.WorkbookPath("Science"));

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
            MoveAway(workspace, file);
        }

        var result = workspace.RunMain();

        ExtractionAssert.FailsWith(result);
    }

    [Fact]
    public void ItemsSheetHidden_FailsWithSingleErrorAndNoWarning()
    {
        using var workspace = new ScenarioWorkspace();
        workspace.Mutate("Rpg", "Items", sheet => sheet.Hide());

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
        workspace.Mutate("Rpg", "Items", sheet =>
        {
            for (var i = 0; i < 9; i++)
            {
                var row = 7 + i;
                object?[] cells =
                [
                    10004 + i, FormattableString.Invariant($"강화석 {i + 1}단계"), "장비 강화 재료", "Common", null, 0, 999,
                    0, 0, 1.0, "material", null, null,
                ];
                for (var c = 0; c < cells.Length; c++)
                {
                    CellWriter.Write(sheet.Cell(row, SheetCells.TitledStartColumn + c), cells[c]);
                }
            }

            var priceColumn = SheetCells.Header(sheet, "Price").Address.ColumnNumber;
            for (var row = 4; row <= 15; row++)
            {
                sheet.Cell(row, priceColumn).FormulaA1 = "=100*2";
            }
        });

        var result = workspace.RunMain();

        ExtractionAssert.FailsWith(result, "has formulas in cells", "(12 cells)");
        var error = Assert.Single(result.Errors, x => x.Contains("has formulas in cells", StringComparison.Ordinal));
        var start = error.IndexOf("in cells ", StringComparison.Ordinal) + "in cells ".Length;
        var end = error.IndexOf(" (12 cells)", StringComparison.Ordinal);
        var addresses = error[start..end];
        Assert.Equal(10, addresses.Split(',').Length);
    }

    private static void MoveAway(ScenarioWorkspace workspace, string file)
    {
        var parking = Directory.CreateDirectory(Path.Combine(workspace.Root, "parking"));
        File.Move(file, Path.Combine(parking.FullName, Path.GetFileName(file)));
    }
}
