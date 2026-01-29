using System.Text;
using Eds.Attributes;
using ExcelColumnExtractor.Aggregator;
using ExcelColumnExtractor.Mappings;
using SchemaInfoScanner.Extensions;

namespace ExcelColumnExtractor.Writers;

public static class CsvWriter
{
    private const int FlushThreshold = 1024 * 1024;
    private static readonly char[] SpecialChars = [',', '"', '\n', '\r'];

    public static void Write(
        string path,
        Encoding encoding,
        ExtractedTableMap extractedTableMap)
    {
        foreach (var (recordSchema, table) in extractedTableMap.SortedTables)
        {
            var excelFileName = recordSchema.GetAttributeValue<StaticDataRecordAttribute, string>(0);
            var sheetName = recordSchema.GetAttributeValue<StaticDataRecordAttribute, string>(1);
            var fileName = Path.Combine(path, $"{excelFileName}.{sheetName}.csv");
            var sb = new StringBuilder();
            sb.AppendLine(string.Join(",", table.Headers));

            using var writer = new StreamWriter(fileName, false, encoding);
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
    }

    private static string ConvertRowToCsv(BodyColumnAggregator.ExtractedRow row)
    {
        var sb = new StringBuilder();

        foreach (var cell in row.Data)
        {
            var value = cell.Value ?? string.Empty;

            if (value.IndexOfAny(SpecialChars) != -1)
            {
                sb.Append('"');
                sb.Append(value.Replace("\"", "\"\""));
                sb.Append('"');
            }
            else
            {
                sb.Append(value);
            }

            sb.Append(',');
        }

        if (sb.Length > 0)
        {
            sb.Length -= 1;
        }

        return sb.ToString();
    }
}
