using System.Globalization;
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
        var failures = new List<ExtractionException.Failure>();

        foreach (var (recordSchema, table) in extractedTableMap.SortedTables)
        {
            try
            {
                CheckTable(recordSchema, table);
            }
            catch (Exception e)
            {
                failures.Add(new(recordSchema, e));
                LogError(logger, recordSchema, e.Message, e);
            }
        }

        if (failures.Count > 0)
        {
            throw new ExtractionException(failures);
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
        var columnIndexes = FindKeyHeaderIndexes(table.Headers, columnName);
        if (columnIndexes.Count == 0)
        {
            throw new InvalidOperationException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.PrimaryKeyHeaderNotFound,
                columnName));
        }

        var duplicates = new List<string>();
        var seen = new HashSet<IReadOnlyList<string>>(KeyValueComparer.Instance);
        foreach (var row in table.Rows)
        {
            var keyValues = columnIndexes.Select(i => row.Data[i].Value).ToList();
            if (!seen.Add(keyValues))
            {
                duplicates.Add(string.Format(
                    CultureInfo.CurrentCulture,
                    Messages.Composite.PrimaryKeyDuplicateFound,
                    columnName,
                    FormatKeyValue(keyValues)));
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

    private static List<int> FindKeyHeaderIndexes(IReadOnlyList<string> headers, string name)
    {
        var prefix = name + ".";
        var indexes = new List<int>();
        for (var i = 0; i < headers.Count; i++)
        {
            var header = headers[i];
            if (string.Equals(header, name, StringComparison.Ordinal) || header.StartsWith(prefix, StringComparison.Ordinal))
            {
                indexes.Add(i);
            }
        }

        return indexes;
    }

    private static string FormatKeyValue(List<string> keyValues)
    {
        if (keyValues.Count == 1)
        {
            return keyValues[0];
        }

        return "(" + string.Join(", ", keyValues) + ")";
    }

    private sealed class KeyValueComparer : IEqualityComparer<IReadOnlyList<string>>
    {
        public static readonly KeyValueComparer Instance = new();

        public bool Equals(IReadOnlyList<string>? x, IReadOnlyList<string>? y)
        {
            if (x is null || y is null)
            {
                return x is null && y is null;
            }

            return x.SequenceEqual(y, StringComparer.Ordinal);
        }

        public int GetHashCode(IReadOnlyList<string> obj)
        {
            var hash = default(HashCode);
            foreach (var value in obj)
            {
                hash.Add(value, StringComparer.Ordinal);
            }

            return hash.ToHashCode();
        }
    }

    private static readonly Action<ILogger, RecordSchema, string, Exception?> LogError =
        LoggerMessage.Define<RecordSchema, string>(LogLevel.Error, new EventId(0, nameof(LogError)), "{RecordSchema}: {ErrorMessage}");
}
