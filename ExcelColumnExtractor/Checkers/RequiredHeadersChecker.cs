using System.Globalization;
using System.Text;
using ExcelColumnExtractor.Exceptions;
using ExcelColumnExtractor.Extensions;
using ExcelColumnExtractor.Mappings;
using ExcelColumnExtractor.NameObjects;
using ExcelColumnExtractor.Resources;
using ExcelColumnExtractor.Scanners;
using Microsoft.Extensions.Logging;
using SchemaInfoScanner;
using SchemaInfoScanner.Catalogs;
using SchemaInfoScanner.Schemata;

namespace ExcelColumnExtractor.Checkers;

public static class RequiredHeadersChecker
{
    public sealed record RequiredHeader(string Header, int Index);

    public sealed record RequiredHeaderMapping(IReadOnlyList<RequiredHeader> Headers);

    public static RequiredHeaderMap Check(
        RecordSchemaCatalog recordSchemaCatalog,
        ExcelSheetNameMap sheetNameMap,
        string startCell,
        ILogger logger)
    {
        var result = new Dictionary<RecordSchema, RequiredHeaderMapping>(recordSchemaCatalog.StaticDataRecordSchemata.Count);

        var failures = new List<ExtractionException.Failure>();
        foreach (var recordSchema in recordSchemaCatalog.StaticDataRecordSchemata)
        {
            try
            {
                var excelSheetName = sheetNameMap.Get(recordSchema);
                var recordStartCell = StartCellResolver.Resolve(recordSchema, startCell);

                var targetColumnIndexSet = ResolveRequiredHeaderMapping(
                    recordSchema,
                    recordSchemaCatalog,
                    excelSheetName,
                    recordStartCell,
                    logger);

                result.Add(recordSchema, targetColumnIndexSet);
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

    private static RequiredHeaderMapping ResolveRequiredHeaderMapping(
        RecordSchema recordSchema,
        RecordSchemaCatalog recordSchemaCatalog,
        ExcelSheetName excelSheetName,
        string startCell,
        ILogger logger)
    {
        var sheetHeaders = SheetHeaderScanner.Scan(excelSheetName, startCell, logger);
        var standardHeaders = RecordFlattener.Flatten(
            recordSchema,
            recordSchemaCatalog,
            logger,
            sheetHeaders);

        return new(ComputeRequiredHeaders(standardHeaders, sheetHeaders));
    }

    private static List<RequiredHeader> ComputeRequiredHeaders(IReadOnlyList<string> standardHeaders, IReadOnlyList<string> sheetHeaders)
    {
        var requiredHeaders = new List<RequiredHeader>();
        var standardHeaderByIndex = new Dictionary<int, string>();
        foreach (var standardHeader in standardHeaders)
        {
            var index = IndexOf(sheetHeaders, standardHeader);

            // Single parameter record 하위 호환: "Id" -> "Id.Value" 폴백
            if (index is -1)
            {
                var fallbackHeader = $"{standardHeader}.Value";
                index = IndexOf(sheetHeaders, fallbackHeader);
            }

            if (index is -1)
            {
                var sb = new StringBuilder();
                sb.AppendLine(string.Format(
                    CultureInfo.CurrentCulture,
                    Messages.Composite.RequiredHeaderNotFound,
                    standardHeader));
                sb.AppendLine(string.Format(
                    CultureInfo.CurrentCulture,
                    Messages.Composite.SheetHeadersLine,
                    string.Join(", ", sheetHeaders)));
                sb.AppendLine(string.Format(
                    CultureInfo.CurrentCulture,
                    Messages.Composite.RecordHeadersLine,
                    string.Join(", ", standardHeaders)));

                throw new ArgumentException(sb.ToString());
            }

            var sheetHeader = sheetHeaders[index];
            if (!sheetHeader.Equals(standardHeader, StringComparison.OrdinalIgnoreCase) &&
                !sheetHeader.Equals($"{standardHeader}.Value", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(string.Format(
                    CultureInfo.CurrentCulture,
                    Messages.Composite.HeaderCaseSensitivity,
                    standardHeader));
            }

            if (!standardHeaderByIndex.TryAdd(index, standardHeader))
            {
                throw new ArgumentException(string.Format(
                    CultureInfo.CurrentCulture,
                    Messages.Composite.DuplicateRequiredHeader,
                    standardHeaderByIndex[index],
                    standardHeader,
                    sheetHeader));
            }

            requiredHeaders.Add(new(sheetHeader, index));
        }

        return requiredHeaders;
    }

    private static int IndexOf(IReadOnlyList<string> list, string value)
    {
        for (var i = 0; i < list.Count; i++)
        {
            if (string.Equals(list[i], value, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }

    private static readonly Action<ILogger, RecordSchema, string, Exception?> LogError =
        LoggerMessage.Define<RecordSchema, string>(LogLevel.Error, new EventId(0, nameof(LogError)), "{RecordSchema}: {ErrorMessage}");
}
