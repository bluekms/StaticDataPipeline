namespace Sdp.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public sealed class StaticDataRecordAttribute(
    string excelFileName,
    string sheetName)
    : Attribute
{
    public string ExcelFileName { get; } = excelFileName;
    public string SheetName { get; } = sheetName;
}
