using ExcelColumnExtractor.NameObjects;
using ExcelColumnExtractor.Resources;
using Microsoft.Extensions.Logging;
using SchemaInfoScanner.Schemata.TypedPropertySchemata;

namespace ExcelColumnExtractor.Scanners;

public class SheetBodyScanner
{
    public sealed record RowData(IReadOnlyList<CellData> Data);
    public sealed record BodyData(IReadOnlyList<RowData> Rows);

    public static BodyData Scan(ExcelSheetName excelSheetName, ILogger logger)
    {
        List<RowData> rows = [];
        void ProcessBody(SheetBodyRow row)
        {
            rows.Add(new(row.Cells));
        }

        var processor = new ExcelSheetProcessor(ProcessBody);
        processor.Process(excelSheetName, logger);

        LogTrace(logger, Messages.SheetScanned(excelSheetName.FullName, rows.Count), null);
        return new(rows.AsReadOnly());
    }

    private static readonly Action<ILogger, string, Exception?> LogTrace =
        LoggerMessage.Define<string>(
            LogLevel.Trace, new EventId(1, nameof(SheetBodyScanner)), "{Message}");
}
