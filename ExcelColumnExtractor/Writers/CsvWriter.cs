using System.Globalization;
using System.Text;
using ExcelColumnExtractor.Aggregator;
using ExcelColumnExtractor.Mappings;
using SchemaInfoScanner.Extensions;
using SchemaInfoScanner.Schemata;
using SchemaInfoScanner.Schemata.TypedPropertySchemata;
using Sdp.Attributes;

namespace ExcelColumnExtractor.Writers;

public static class CsvWriter
{
    private const int FlushThreshold = 1024 * 1024;
    private static readonly char[] SpecialChars = [',', '"', '\n', '\r'];
    private static readonly UTF8Encoding Utf8NoBom = new(false);

    private sealed record MergedRow(string Address, string[] Cells);

    public static void Write(
        string path,
        ExtractedTableMap extractedTableMap)
    {
        var tablesByFile = extractedTableMap.SortedTables
            .GroupBy(x => CsvFileName(x.Key), StringComparer.Ordinal);

        foreach (var group in tablesByFile)
        {
            var tables = group.Select(x => x.Value).ToList();
            var table = tables.Count == 1 ? tables[0] : MergeSharedSheetTables(tables);
            WriteTable(Path.Combine(path, group.Key), table);
        }
    }

    private static string CsvFileName(RecordSchema recordSchema)
    {
        var excelFileName = recordSchema.GetAttributeValue<StaticDataRecordAttribute, string>(0);
        var sheetName = recordSchema.GetAttributeValue<StaticDataRecordAttribute, string>(1);

        return $"{excelFileName}.{sheetName}.csv";
    }

    private static BodyColumnAggregator.ExtractedTable MergeSharedSheetTables(List<BodyColumnAggregator.ExtractedTable> tables)
    {
        var headers = new List<string>();
        var headerIndexes = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var header in tables.SelectMany(table => table.Headers))
        {
            if (headerIndexes.TryAdd(header, headers.Count))
            {
                headers.Add(header);
            }
        }

        var rowsByNumber = new SortedDictionary<int, MergedRow>();
        foreach (var table in tables)
        {
            foreach (var row in table.Rows)
            {
                var rowNumber = RowNumber(row.Data[0].Address);
                if (!rowsByNumber.TryGetValue(rowNumber, out var merged))
                {
                    var cells = new string[headers.Count];
                    Array.Fill(cells, string.Empty);
                    merged = new MergedRow(row.Data[0].Address, cells);
                    rowsByNumber.Add(rowNumber, merged);
                }

                for (var i = 0; i < row.Data.Count; i++)
                {
                    merged.Cells[headerIndexes[table.Headers[i]]] = row.Data[i].Value;
                }
            }
        }

        var rows = rowsByNumber.Values
            .Select(merged => new BodyColumnAggregator.ExtractedRow(ToCells(merged, headers)))
            .ToList();

        return new BodyColumnAggregator.ExtractedTable(headers, rows);
    }

    private static List<CellData> ToCells(MergedRow merged, List<string> headers)
    {
        return merged.Cells
            .Select((value, i) => new CellData(merged.Address, value, headers[i]))
            .ToList();
    }

    private static int RowNumber(string address)
    {
        var start = 0;
        while (start < address.Length && char.IsLetter(address[start]))
        {
            start++;
        }

        return int.Parse(address.AsSpan(start), NumberStyles.None, CultureInfo.InvariantCulture);
    }

    private static void WriteTable(string fileName, BodyColumnAggregator.ExtractedTable table)
    {
        var sb = new StringBuilder();

        sb.AppendLine(string.Join(",", table.Headers.Select(EscapeCell)));

        using var writer = new StreamWriter(fileName, false, Utf8NoBom);
        foreach (var row in table.Rows)
        {
            if (sb.Length > FlushThreshold)
            {
                writer.Write(sb.ToString());
                sb.Clear();
            }

            sb.AppendLine(ConvertRowToCsv(row));
        }

        if (sb.Length > 0)
        {
            writer.Write(sb.ToString());
        }
    }

    private static string ConvertRowToCsv(BodyColumnAggregator.ExtractedRow row)
    {
        var sb = new StringBuilder();

        foreach (var cell in row.Data)
        {
            sb.Append(EscapeCell(cell.Value));
            sb.Append(',');
        }

        if (sb.Length > 0)
        {
            sb.Length -= 1;
        }

        return sb.ToString();
    }

    private static string EscapeCell(string value)
    {
        if (value.IndexOfAny(SpecialChars) == -1)
        {
            return value;
        }

        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
