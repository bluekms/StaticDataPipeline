using System.Text;
using ExcelColumnExtractor.Exceptions;
using ExcelColumnExtractor.Extensions;
using ExcelColumnExtractor.Mappings;
using ExcelColumnExtractor.Scanners;
using Microsoft.Extensions.Logging;
using SchemaInfoScanner.Schemata;
using SchemaInfoScanner.Schemata.TypedPropertySchemata;

namespace ExcelColumnExtractor.Aggregator;

public static class BodyColumnAggregator
{
    public sealed record ExtractedRow(IReadOnlyList<CellData> Data);

    public sealed record ExtractedTable(IReadOnlyList<string> Headers, IReadOnlyList<ExtractedRow> Rows);

    public static ExtractedTableMap Aggregate(
        IReadOnlyList<RecordSchema> recordSchemaList,
        ExcelSheetNameMap sheetNameMap,
        RequiredHeaderMap requiredHeaderMap,
        string startCell,
        ILogger logger)
    {
        var result = new Dictionary<RecordSchema, ExtractedTable>();

        var sb = new StringBuilder();
        foreach (var recordSchema in recordSchemaList)
        {
            try
            {
                var excelSheetName = sheetNameMap.Get(recordSchema);
                var recordStartCell = StartCellResolver.Resolve(recordSchema, startCell);
                var sheetBody = SheetBodyScanner.Scan(excelSheetName, recordStartCell, logger);
                var targetColumnData = requiredHeaderMap.Get(recordSchema);

                var filteredRows = sheetBody.Rows
                    .Select(row =>
                    {
                        var cells = targetColumnData.Headers
                            .Select(header => row.Data[header.Index] with { Header = header.Header })
                            .ToList();

                        return new ExtractedRow(cells);
                    })
                    .Where(row => row.Data.Any(cell => !string.IsNullOrEmpty(cell.Value)))
                    .ToList();

                var headers = targetColumnData.Headers.Select(header => header.Header).ToList();
                result.Add(recordSchema, new(headers, filteredRows));
            }
            catch (Exception e)
            {
                sb.AppendLine(e.Message);
                LogError(logger, recordSchema, e.Message, e);
            }
        }

        return sb.Length > 0
            ? throw new BodyColumnAggregatorException(sb.ToString())
            : new(result);
    }

    private static readonly Action<ILogger, RecordSchema, string, Exception?> LogError =
        LoggerMessage.Define<RecordSchema, string>(LogLevel.Error, new EventId(0, nameof(LogError)), "{RecordSchema}: {ErrorMessage}");
}
