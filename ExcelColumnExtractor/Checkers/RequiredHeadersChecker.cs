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
    public sealed record RequiredHeader(string Header, int Index, int SheetColumn, string? DateTimeFormat);

    public sealed record RequiredHeaderMapping(string StartCell, IReadOnlyList<RequiredHeader> Headers);

    public static RequiredHeaderMap Check(
        RecordSchemaCatalog recordSchemaCatalog,
        ExcelSheetNameMap sheetNameMap,
        string? startCell,
        ILogger logger)
    {
        var result = new Dictionary<RecordSchema, RequiredHeaderMapping>(recordSchemaCatalog.StaticDataRecordSchemata.Count);

        var failures = new List<ExtractionException.Failure>();
        foreach (var recordSchema in recordSchemaCatalog.StaticDataRecordSchemata)
        {
            try
            {
                var excelSheetName = sheetNameMap.Get(recordSchema);
                EnsureSheetVisible(excelSheetName);

                var recordStartCell = StartCellResolver.Resolve(recordSchema, startCell, logger);

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

    private static void EnsureSheetVisible(ExcelSheetName excelSheetName)
    {
        if (!excelSheetName.IsHidden)
        {
            return;
        }

        var msg = string.Format(
            CultureInfo.CurrentCulture,
            Messages.Composite.SheetIsHidden,
            excelSheetName.FullName);

        throw new ArgumentException(msg);
    }

    private static RequiredHeaderMapping ResolveRequiredHeaderMapping(
        RecordSchema recordSchema,
        RecordSchemaCatalog recordSchemaCatalog,
        ExcelSheetName excelSheetName,
        string startCell,
        ILogger logger)
    {
        var sheetHeaders = SheetHeaderScanner.Scan(excelSheetName, startCell, logger);
        var standardHeaders = RecordFlattener.FlattenHeaders(
            recordSchema,
            recordSchemaCatalog,
            logger,
            sheetHeaders);

        var startColumn = ExcelSheetProcessor.ParseStartCell(startCell).Column;

        return new(startCell, ComputeRequiredHeaders(standardHeaders, sheetHeaders, startColumn));
    }

    private static List<RequiredHeader> ComputeRequiredHeaders(
        IReadOnlyList<RecordFlattener.FlattenedHeader> standardHeaders,
        IReadOnlyList<string> sheetHeaders,
        int startColumn)
    {
        var requiredHeaders = new List<RequiredHeader>();
        var standardHeaderByIndex = new Dictionary<int, string>();
        foreach (var flattenedHeader in standardHeaders)
        {
            var standardHeader = flattenedHeader.Name;
            var index = IndexOf(sheetHeaders, standardHeader);

            // Single parameter record 하위 호환: "Id" -> "Id.Value" 폴백
            var fallbackHeader = $"{standardHeader}.Value";
            if (flattenedHeader.FromSingleValueRecord)
            {
                var fallbackIndex = IndexOf(sheetHeaders, fallbackHeader);
                if (index is not -1 && fallbackIndex is not -1)
                {
                    var bothFormsMsg = string.Format(
                        CultureInfo.CurrentCulture,
                        Messages.Composite.BrandedHeaderBothForms,
                        standardHeader,
                        fallbackHeader);

                    throw new ArgumentException(bothFormsMsg);
                }

                if (index is -1)
                {
                    index = fallbackIndex;
                }
            }

            if (index is -1)
            {
                var caseMismatchHeader = FindIgnoringCase(sheetHeaders, flattenedHeader, fallbackHeader);
                if (caseMismatchHeader is not null)
                {
                    throw new ArgumentException(string.Format(
                        CultureInfo.CurrentCulture,
                        Messages.Composite.HeaderCaseSensitivity,
                        standardHeader,
                        caseMismatchHeader));
                }

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
                    string.Join(", ", standardHeaders.Select(x => x.Name))));

                throw new ArgumentException(sb.ToString());
            }

            var sheetHeader = sheetHeaders[index];

            if (!standardHeaderByIndex.TryAdd(index, standardHeader))
            {
                throw new ArgumentException(string.Format(
                    CultureInfo.CurrentCulture,
                    Messages.Composite.DuplicateRequiredHeader,
                    standardHeaderByIndex[index],
                    standardHeader,
                    sheetHeader));
            }

            requiredHeaders.Add(new(sheetHeader, index, startColumn + index, flattenedHeader.DateTimeFormat));
        }

        return requiredHeaders;
    }

    private static string? FindIgnoringCase(
        IReadOnlyList<string> sheetHeaders,
        RecordFlattener.FlattenedHeader flattenedHeader,
        string fallbackHeader)
    {
        foreach (var sheetHeader in sheetHeaders)
        {
            if (string.Equals(sheetHeader, flattenedHeader.Name, StringComparison.OrdinalIgnoreCase))
            {
                return sheetHeader;
            }

            if (flattenedHeader.FromSingleValueRecord &&
                string.Equals(sheetHeader, fallbackHeader, StringComparison.OrdinalIgnoreCase))
            {
                return sheetHeader;
            }
        }

        return null;
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
