using System.Text;
using ExcelColumnExtractor.Exceptions;
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
        ILogger logger)
    {
        var result = new Dictionary<RecordSchema, ExtractedTable>();

        var sb = new StringBuilder();
        foreach (var recordSchema in recordSchemaList)
        {
            try
            {
                var excelSheetName = sheetNameMap.Get(recordSchema);
                var sheetBody = SheetBodyScanner.Scan(excelSheetName, logger);
                var targetColumnData = requiredHeaderMap.Get(recordSchema);

                var filteredRows = sheetBody.Rows
                    .Select(row => new ExtractedRow(row.Data
                        .Where((_, index) => targetColumnData.SheetHeaderIndexSet.Contains(index))
                        .ToList()))
                    .Where(row => row.Data.Any(cell => !string.IsNullOrEmpty(cell.Value)))
                    .ToList();

                result.Add(recordSchema, new(targetColumnData.SheetHeaders, filteredRows));
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
