using System.Text;
using ExcelColumnExtractor.Aggregator;
using ExcelColumnExtractor.Mappings;
using SchemaInfoScanner.Extensions;
using Sdp.Attributes;

namespace ExcelColumnExtractor.Writers;

public static class CsvWriter
{
    private const int FlushThreshold = 1024 * 1024;
    private static readonly char[] SpecialChars = [',', '"', '\n', '\r'];
    private static readonly UTF8Encoding Utf8NoBom = new(false);

    public static void Write(
        string path,
        ExtractedTableMap extractedTableMap)
    {
        foreach (var (recordSchema, table) in extractedTableMap.SortedTables)
        {
            var excelFileName = recordSchema.GetAttributeValue<StaticDataRecordAttribute, string>(0);
            var sheetName = recordSchema.GetAttributeValue<StaticDataRecordAttribute, string>(1);
            var fileName = Path.Combine(path, $"{excelFileName}.{sheetName}.csv");
            var sb = new StringBuilder();

            // 헤더에도 데이터 행과 같은 escape를 적용한다. 헤더명에 특수문자(쉼표 등)가
            // 들어가면 escape 없는 헤더 행이 CSV 구조를 깨뜨린다.
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
    }

    private static string ConvertRowToCsv(BodyColumnAggregator.ExtractedRow row)
    {
        var sb = new StringBuilder();

        foreach (var cell in row.Data)
        {
            sb.Append(EscapeCell(cell.Value ?? string.Empty));
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
