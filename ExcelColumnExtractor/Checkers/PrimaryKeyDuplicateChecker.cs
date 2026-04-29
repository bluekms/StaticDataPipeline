using System.Globalization;
using System.Text;
using ExcelColumnExtractor.Aggregator;
using ExcelColumnExtractor.Exceptions;
using ExcelColumnExtractor.Mappings;
using ExcelColumnExtractor.Resources;
using Microsoft.Extensions.Logging;
using SchemaInfoScanner.Extensions;
using SchemaInfoScanner.Schemata;
using Sdp.Attributes;

namespace ExcelColumnExtractor.Checkers;

public static class PrimaryKeyDuplicateChecker
{
    public static void Check(ExtractedTableMap extractedTableMap, ILogger<Program> logger)
    {
        var sb = new StringBuilder();

        foreach (var (recordSchema, table) in extractedTableMap.SortedTables)
        {
            try
            {
                CheckTable(recordSchema, table);
            }
            catch (Exception e)
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"{recordSchema}: {e.Message}");
                LogError(logger, recordSchema, e.Message, e);
            }
        }

        if (sb.Length > 0)
        {
            throw new DataBodyCheckerException(sb.ToString());
        }
    }

    private static void CheckTable(RecordSchema recordSchema, BodyColumnAggregator.ExtractedTable table)
    {
        var keyProperty = recordSchema.PropertySchemata
            .FirstOrDefault(p => p.HasAttribute<KeyAttribute>());

        if (keyProperty is null)
        {
            return;
        }

        var columnName = ResolveColumnName(keyProperty);
        var columnIndex = FindHeaderIndex(table.Headers, columnName);
        if (columnIndex < 0)
        {
            throw new InvalidOperationException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.PrimaryKeyHeaderNotFound,
                columnName));
        }

        var duplicates = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var row in table.Rows)
        {
            var value = row.Data[columnIndex].Value;
            if (!seen.Add(value))
            {
                duplicates.Add(string.Format(
                    CultureInfo.CurrentCulture,
                    Messages.Composite.PrimaryKeyDuplicateFound,
                    columnName,
                    value));
            }
        }

        if (duplicates.Count > 0)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, duplicates));
        }
    }

    private static string ResolveColumnName(PropertySchemaBase keyProperty)
    {
        if (keyProperty.TryGetAttributeValue<ColumnNameAttribute, string>(out var columnName))
        {
            return columnName;
        }

        return keyProperty.PropertyName.Name;
    }

    private static int FindHeaderIndex(IReadOnlyList<string> headers, string name)
    {
        var index = IndexOf(headers, name);
        if (index >= 0)
        {
            return index;
        }

        return IndexOf(headers, FormattableString.Invariant($"{name}.Value"));
    }

    private static int IndexOf(IReadOnlyList<string> headers, string name)
    {
        for (var i = 0; i < headers.Count; i++)
        {
            if (string.Equals(headers[i], name, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }

    private static readonly Action<ILogger, RecordSchema, string, Exception?> LogError =
        LoggerMessage.Define<RecordSchema, string>(LogLevel.Error, new EventId(0, nameof(LogError)), "{RecordSchema}: {ErrorMessage}");
}
