using ScenarioTest.Support;

namespace ScenarioTest.Tests.Warnings;

[Collection(CliCollectionDefinition.Name)]
public class WarningOnlyTests
{
    [Fact]
    public void CurrencyCapWithThousandsSeparatorFormat_WarnsOnceAndKeepsStoredValue()
    {
        using var workspace = new ScenarioWorkspace();
        workspace.Mutate("Rpg", "Currencies", sheet =>
        {
            var capColumn = SheetCells.Header(sheet, "Cap").Address.ColumnNumber;
            sheet.Cell(4, capColumn).Style.NumberFormat.Format = "#,##0";
            sheet.Cell(5, capColumn).Style.NumberFormat.Format = "#,##0";
        });

        var result = workspace.RunMain();

        Assert.Equal(0, result.ExitCode);
        var warning = Assert.Single(ExtractionAssert.WarningsExceptLoansStartCell(result));
        Assert.Contains("'#,##0'", warning, StringComparison.Ordinal);
        Assert.Contains("'9999999999'", warning, StringComparison.Ordinal);
        Golden.AssertEquals("Rpg/Rpg.Currencies.csv", Path.Combine(workspace.CsvMain, "Rpg.Currencies.csv"));
    }

    [Fact]
    public void NicknameWithSurroundingSpaces_WarnsWithQuotedValueAndKeepsSpaces()
    {
        using var workspace = new ScenarioWorkspace();
        workspace.Mutate("School", "Students", sheet => SheetCells.Find(sheet, "20260101", "Nickname").Value = " 하늘이 ");

        var result = workspace.RunMain();

        Assert.Equal(0, result.ExitCode);
        var warning = Assert.Single(ExtractionAssert.WarningsExceptLoansStartCell(result));
        Assert.Contains("' 하늘이 '", warning, StringComparison.Ordinal);
        var students = CsvDocument.Load(Path.Combine(workspace.CsvMain, "School.Students.csv"));
        Assert.Equal(" 하늘이 ", students.Row("Id", "20260101")[students.Column("Nickname")]);
    }

    [Fact]
    public void ReservationRowsWithOnlyBackgroundColor_AreTreatedAsNoData()
    {
        using var workspace = new ScenarioWorkspace();
        workspace.Mutate("Library", "Reservations", sheet =>
        {
            sheet.Range("B4:E12").Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightYellow;
        });

        var result = workspace.RunMain();

        Assert.Equal(0, result.ExitCode);
        Assert.Empty(ExtractionAssert.WarningsExceptLoansStartCell(result));
        Assert.Contains(
            result.InformationLines,
            x => x.Contains("'Library.Reservations' has no data rows", StringComparison.Ordinal));
        var reservations = CsvDocument.Load(Path.Combine(workspace.CsvMain, "Library.Reservations.csv"));
        Assert.Empty(reservations.Rows);
    }
}
