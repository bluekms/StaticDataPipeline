using ClosedXML.Excel;

namespace ScenarioTest.Support;

public static class CellWriter
{
    public static void Write(IXLCell cell, object? value)
    {
        switch (value)
        {
            case null:
                cell.Clear(XLClearOptions.Contents);
                break;
            case string text:
                cell.Value = text;
                break;
            case DateTime date:
                cell.Value = date;
                break;
            case TimeOnly time:
                cell.Value = time.ToTimeSpan().TotalDays;
                cell.Style.NumberFormat.Format = "HH:mm";
                break;
            case int number:
                cell.Value = number;
                break;
            case long number:
                cell.Value = number;
                break;
            case double number:
                cell.Value = number;
                break;
            case decimal number:
                cell.Value = number;
                break;
            default:
                throw new ArgumentException(FormattableString.Invariant(
                    $"Unsupported cell value type {value.GetType().Name}."));
        }
    }
}
