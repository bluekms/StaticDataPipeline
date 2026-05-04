using ExcelColumnExtractor.NameObjects;
using Microsoft.Extensions.Logging;

namespace ExcelColumnExtractor.Scanners;

public static class SheetHeaderScanner
{
    public static IReadOnlyList<string> Scan(ExcelSheetName excelSheetName, string startCell, ILogger logger)
    {
        IReadOnlyList<string>? headerCells = null;
        void ProcessHeader(SheetHeader header)
        {
            headerCells = header.Cells;
        }

        var processor = new ExcelSheetProcessor(ProcessHeader);
        processor.Process(excelSheetName, startCell, logger);

        var headers = headerCells ?? [];
        LogTrace(logger, excelSheetName.FullName, string.Join(", ", headers), null);

        return headers;
    }

    private static readonly Action<ILogger, string, string, Exception?> LogTrace =
        LoggerMessage.Define<string, string>(
            LogLevel.Trace, new EventId(1, nameof(SheetHeaderScanner)), "{ExcelSheetName}: [{Headers}]");
}
