using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Globalization;
using Sdp.Resources;

namespace Sdp.Csv;

public sealed class CsvHeaderIndex
{
    private readonly FrozenDictionary<string, int> indexByName;

    public CsvHeaderIndex(IReadOnlyList<string> headerRow, string? filePath = null)
    {
        var builder = new Dictionary<string, int>(headerRow.Count);
        for (var i = 0; i < headerRow.Count; i++)
        {
            if (headerRow[i].Length == 0)
            {
                continue;
            }

            if (!builder.TryAdd(headerRow[i], i))
            {
                var firstIndex = builder[headerRow[i]];
                var fileLabel = filePath is not null
                    ? string.Format(
                        CultureInfo.CurrentCulture,
                        Messages.Composite.CsvFileLabel,
                        Path.GetFileName(filePath))
                    : string.Empty;
                throw new InvalidOperationException(fileLabel + string.Format(
                    CultureInfo.CurrentCulture,
                    Messages.Composite.CsvDuplicateHeader,
                    headerRow[i],
                    i + 1,
                    firstIndex + 1));
            }
        }

        indexByName = builder.ToFrozenDictionary();
    }

    public int this[string columnName]
    {
        get
        {
            var found = indexByName.TryGetValue(columnName, out var index);
            if (!found)
            {
                throw new InvalidOperationException(string.Format(
                    CultureInfo.CurrentCulture,
                    Messages.Composite.CsvHeaderNotFound,
                    columnName));
            }

            return index;
        }
    }

    public ImmutableArray<string> ColumnNames => indexByName.Keys;

    public bool Contains(string columnName)
    {
        return indexByName.ContainsKey(columnName);
    }
}
