using ClosedXML.Excel;

namespace ScenarioTest.Support;

public static class SheetCells
{
    public const int TitledHeaderRow = 3;

    public const int TitledStartColumn = 2;

    public static IXLCell Header(IXLWorksheet sheet, string header, int headerRow = TitledHeaderRow)
    {
        var cell = sheet.Row(headerRow)
            .CellsUsed()
            .FirstOrDefault(x => x.GetString() == header);

        if (cell is null)
        {
            throw new ArgumentException(FormattableString.Invariant($"Header '{header}' not found in {sheet.Name}."));
        }

        return cell;
    }

    public static IXLCell Find(IXLWorksheet sheet, string key, string header, int headerRow = TitledHeaderRow)
    {
        var row = RowOf(sheet, key, headerRow);
        var column = Header(sheet, header, headerRow).Address.ColumnNumber;

        return sheet.Cell(row, column);
    }

    public static int RowOf(IXLWorksheet sheet, string key, int headerRow = TitledHeaderRow)
    {
        var keyColumn = sheet.Row(headerRow).FirstCellUsed().Address.ColumnNumber;
        var cell = sheet.Column(keyColumn)
            .CellsUsed()
            .Where(x => x.Address.RowNumber > headerRow)
            .FirstOrDefault(x => x.GetString() == key);

        if (cell is null)
        {
            throw new ArgumentException(FormattableString.Invariant($"Row '{key}' not found in {sheet.Name}."));
        }

        return cell.Address.RowNumber;
    }

    public static int LastDataRow(IXLWorksheet sheet)
        => sheet.LastRowUsed()!.RowNumber();
}
