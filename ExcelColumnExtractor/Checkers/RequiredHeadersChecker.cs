using System.Globalization;
using System.Text;
using ExcelColumnExtractor.Mappings;
using ExcelColumnExtractor.NameObjects;
using ExcelColumnExtractor.Scanners;
using Microsoft.Extensions.Logging;
using SchemaInfoScanner;
using SchemaInfoScanner.Catalogs;
using SchemaInfoScanner.Schemata;

namespace ExcelColumnExtractor.Checkers;

public static class RequiredHeadersChecker
{
    public sealed record RequiredHeaderMapping(IReadOnlyList<string> SheetHeaders, IReadOnlySet<int> SheetHeaderIndexSet);

    public static RequiredHeaderMap Check(
        RecordSchemaCatalog recordSchemaCatalog,
        ExcelSheetNameMap sheetNameMap,
        ILogger logger)
    {
        var result = new Dictionary<RecordSchema, RequiredHeaderMapping>(recordSchemaCatalog.StaticDataRecordSchemata.Count);

        var sb = new StringBuilder();
        foreach (var recordSchema in recordSchemaCatalog.StaticDataRecordSchemata)
        {
            try
            {
                var excelSheetName = sheetNameMap.Get(recordSchema);

                var targetColumnIndexSet = ResolveRequiredHeaderMapping(
                    recordSchema,
                    recordSchemaCatalog,
                    excelSheetName,
                    logger);

                result.Add(recordSchema, targetColumnIndexSet);
            }
            catch (Exception e)
            {
                var msg = $"{recordSchema.RecordName.FullName}: {e.Message}";
                sb.AppendLine(msg);
                LogError(logger, recordSchema, msg, e);
            }
        }

        return sb.Length > 0
            ? throw new AggregateException(sb.ToString())
            : new(result);
    }

    private static RequiredHeaderMapping ResolveRequiredHeaderMapping(
        RecordSchema recordSchema,
        RecordSchemaCatalog recordSchemaCatalog,
        ExcelSheetName excelSheetName,
        ILogger logger)
    {
        var sheetHeaders = SheetHeaderScanner.Scan(excelSheetName, logger);
        var standardHeaders = RecordFlattener.Flatten(
            recordSchema,
            recordSchemaCatalog,
            logger);

        var targetColumnIndexSet = ComputeRequiredHeaderIndices(standardHeaders, sheetHeaders);
        var requiredSheetHeaders = targetColumnIndexSet.Select(index => sheetHeaders[index]).ToList();
        if (requiredSheetHeaders.Count != targetColumnIndexSet.Count)
        {
            var sb = new StringBuilder();
            sb.AppendLine(CultureInfo.InvariantCulture, $"Header and index count mismatch.");
            sb.AppendLine(CultureInfo.InvariantCulture, $"Headers: {{ {string.Join(", ", requiredSheetHeaders)} }}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"IndexSet: {{ {string.Join(", ", targetColumnIndexSet)} }}");

            throw new ArgumentException(sb.ToString());
        }

        return new(requiredSheetHeaders, targetColumnIndexSet);
    }

    private static HashSet<int> ComputeRequiredHeaderIndices(IReadOnlyList<string> standardHeaders, IReadOnlyList<string> sheetHeaders)
    {
        var requiredHeaderIndices = new HashSet<int>();
        foreach (var standardHeader in standardHeaders)
        {
            var index = CaseInsensitiveIndexOf(sheetHeaders, standardHeader);

            // Single parameter record 하위 호환: "Id" -> "Id.Value" 폴백
            if (index is -1)
            {
                var fallbackHeader = $"{standardHeader}.Value";
                index = CaseInsensitiveIndexOf(sheetHeaders, fallbackHeader);
            }

            if (index is -1)
            {
                var sb = new StringBuilder();
                sb.AppendLine(CultureInfo.InvariantCulture, $"Header not found: {standardHeader}");
                sb.AppendLine(CultureInfo.InvariantCulture, $"SheetHeaders: {{ {string.Join(", ", sheetHeaders)} }}");
                sb.AppendLine(CultureInfo.InvariantCulture, $"RecordHeaders: {{ {string.Join(", ", standardHeaders)} }}");

                throw new ArgumentException(sb.ToString());
            }

            var sheetHeader = sheetHeaders[index];
            if (!sheetHeader.Equals(standardHeader, StringComparison.OrdinalIgnoreCase) &&
                !sheetHeader.Equals($"{standardHeader}.Value", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"Header case sensitivity: {standardHeader}");
            }

            requiredHeaderIndices.Add(index);
        }

        return requiredHeaderIndices;
    }

    private static int CaseInsensitiveIndexOf(IReadOnlyList<string> list, string value)
    {
        for (var i = 0; i < list.Count; i++)
        {
            if (list[i].Equals(value, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    private static readonly Action<ILogger, RecordSchema, string, Exception?> LogError =
        LoggerMessage.Define<RecordSchema, string>(LogLevel.Error, new EventId(0, nameof(LogError)), "{RecordSchema}: {ErrorMessage}");
}
