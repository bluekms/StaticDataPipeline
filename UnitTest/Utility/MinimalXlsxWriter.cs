using ClosedXML.Excel;

namespace UnitTest.Utility;

internal static class MinimalXlsxWriter
{
    public static void Write(string filePath, string sheetName, List<string[]> rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet(sheetName);
        for (var r = 0; r < rows.Count; r++)
        {
            for (var c = 0; c < rows[r].Length; c++)
            {
                sheet.Cell(r + 1, c + 1).Value = rows[r][c];
            }
        }

        workbook.SaveAs(filePath);
    }
}
