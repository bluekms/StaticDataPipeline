namespace ExcelColumnExtractor.NameObjects;

public record ExcelSheetName(string ExcelPath, string SheetName, bool IsHidden = false)
{
    public string FullName => $"{Path.GetFileNameWithoutExtension(ExcelPath)}.{SheetName}";
}
