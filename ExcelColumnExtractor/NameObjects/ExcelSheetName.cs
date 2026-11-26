namespace ExcelColumnExtractor.NameObjects;

public record ExcelSheetName(string ExcelPath, string SheetName, bool IsHidden = false)
{
    public string WorkbookName { get; } = Path.GetFileNameWithoutExtension(ExcelPath);

    public string FullName => $"{WorkbookName}.{SheetName}";
}
