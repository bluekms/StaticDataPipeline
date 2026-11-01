using System.Globalization;
using ExcelColumnExtractor.Exceptions;
using ExcelColumnExtractor.Mappings;
using ExcelColumnExtractor.NameObjects;
using ExcelColumnExtractor.Resources;
using ExcelColumnExtractor.Scanners;
using Microsoft.Extensions.Logging;
using SchemaInfoScanner.Schemata;
using SchemaInfoScanner.Schemata.TypedPropertySchemata;
using FormulaCellsBySheet = System.Collections.Generic.IReadOnlyDictionary<
    string,
    System.Collections.Generic.IReadOnlyList<ExcelColumnExtractor.Scanners.SheetFormulaScanner.FormulaCell>>;

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

        var failures = new List<ExtractionException.Failure>();
        var formulaCellsByWorkbook = new Dictionary<string, FormulaCellsBySheet>(StringComparer.Ordinal);
        foreach (var recordSchema in recordSchemaList)
        {
            try
            {
                var excelSheetName = sheetNameMap.Get(recordSchema);
                var targetColumnData = requiredHeaderMap.Get(recordSchema);
                var targetColumns = targetColumnData.Headers
                    .Select(header => header.SheetColumn)
                    .ToHashSet();
                var formulaCellsBySheet = GetFormulaCellsBySheet(excelSheetName.ExcelPath, formulaCellsByWorkbook);
                EnsureNoFormulaCells(excelSheetName, targetColumnData.StartCell, targetColumns, formulaCellsBySheet);
                var sheetBody = SheetBodyScanner.Scan(excelSheetName, targetColumnData.StartCell, targetColumns, logger);

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

                if (filteredRows.Count == 0)
                {
                    var emptyMsg = string.Format(
                        CultureInfo.CurrentCulture,
                        Messages.Composite.SheetHasNoDataRows,
                        excelSheetName.FullName);
                    LogInformation(logger, emptyMsg, null);
                }

                var headers = targetColumnData.Headers.Select(header => header.Header).ToList();
                result.Add(recordSchema, new(headers, filteredRows));
            }
            catch (Exception e)
            {
                failures.Add(new(recordSchema, e));
                LogError(logger, recordSchema, e.Message, e);
            }
        }

        return failures.Count > 0
            ? throw new ExtractionException(failures)
            : new(result);
    }

    private static FormulaCellsBySheet GetFormulaCellsBySheet(
        string excelPath,
        Dictionary<string, FormulaCellsBySheet> formulaCellsByWorkbook)
    {
        var cached = formulaCellsByWorkbook.GetValueOrDefault(excelPath);
        if (cached is not null)
        {
            return cached;
        }

        var scanned = SheetFormulaScanner.Scan(excelPath);
        formulaCellsByWorkbook.Add(excelPath, scanned);

        return scanned;
    }

    private static void EnsureNoFormulaCells(
        ExcelSheetName excelSheetName,
        string startCell,
        IReadOnlySet<int> targetColumns,
        FormulaCellsBySheet formulaCellsBySheet)
    {
        var sheetFormulaCells = formulaCellsBySheet.GetValueOrDefault(excelSheetName.SheetName);
        if (sheetFormulaCells is null)
        {
            throw new ArgumentException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.SheetNotFound,
                excelSheetName.FullName));
        }

        var startRow = ExcelSheetProcessor.ParseStartCell(startCell).Row;
        var formulaCells = sheetFormulaCells
            .Where(x => x.Row >= startRow)
            .Where(x => targetColumns.Contains(x.Column))
            .Select(x => x.Reference)
            .ToList();
        if (formulaCells.Count == 0)
        {
            return;
        }

        var msg = string.Format(
            CultureInfo.CurrentCulture,
            Messages.Composite.FormulaCellsFound,
            excelSheetName.FullName,
            string.Join(", ", formulaCells.Take(MaxReportedFormulaCells)),
            formulaCells.Count);

        throw new InvalidOperationException(msg);
    }

    private const int MaxReportedFormulaCells = 10;

    private static readonly Action<ILogger, RecordSchema, string, Exception?> LogError =
        LoggerMessage.Define<RecordSchema, string>(LogLevel.Error, new EventId(0, nameof(LogError)), "{RecordSchema}: {ErrorMessage}");

    private static readonly Action<ILogger, string, Exception?> LogInformation =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(2, nameof(LogInformation)), "{Message}");
}
