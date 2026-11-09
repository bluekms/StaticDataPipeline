using ScenarioTest.Support;

namespace ScenarioTest.Tests.Warnings;

[Collection(CliCollectionDefinition.Name)]
public class WarningOnlyTests
{
    [Fact]
    public void CurrencyCapWithThousandsSeparatorFormat_WarnsOnceAndKeepsStoredValue()
    {
        using var workspace = new ScenarioWorkspace();
        workspace.UseBroken("Rpg/Currencies/CurrencyCapThousandsFormat");

        var result = workspace.RunMain();

        ExtractionAssert.Succeeded(result);
        var warning = Assert.Single(ExtractionAssert.WarningsExceptLoansStartCell(result));
        Assert.Contains("'#,##0'", warning, StringComparison.Ordinal);
        Assert.Contains("'9999999999'", warning, StringComparison.Ordinal);
        Golden.AssertEquals("Rpg/Rpg.Currencies.csv", Path.Combine(workspace.CsvMain, "Rpg.Currencies.csv"));
    }

    [Fact]
    public void NicknameWithSurroundingSpaces_WarnsWithQuotedValueAndKeepsSpaces()
    {
        using var workspace = new ScenarioWorkspace();
        workspace.UseBroken("School/Students/NicknameWithSpaces");

        var result = workspace.RunMain();

        ExtractionAssert.Succeeded(result);
        var warning = Assert.Single(ExtractionAssert.WarningsExceptLoansStartCell(result));
        Assert.Contains("' 하늘이 '", warning, StringComparison.Ordinal);
        var students = CsvDocument.Load(Path.Combine(workspace.CsvMain, "School.Students.csv"));
        Assert.Equal(" 하늘이 ", students.Row("Id", "20260101")[students.ColumnIndex("Nickname")]);
    }

    [Fact]
    public void ReservationRowsWithOnlyBackgroundColor_AreTreatedAsNoData()
    {
        using var workspace = new ScenarioWorkspace();
        workspace.UseBroken("Library/Reservations/ReservationRowsBackgroundOnly");

        var result = workspace.RunMain();

        ExtractionAssert.Succeeded(result);
        Assert.Empty(ExtractionAssert.WarningsExceptLoansStartCell(result));
        Assert.Contains(
            result.InformationLines,
            x => x.Contains("'Library.Reservations' has no data rows", StringComparison.Ordinal));
        var reservations = CsvDocument.Load(Path.Combine(workspace.CsvMain, "Library.Reservations.csv"));
        Assert.Empty(reservations.Rows);
    }
}
