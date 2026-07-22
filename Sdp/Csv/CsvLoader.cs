using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using Sdp.Resources;

namespace Sdp.Csv;

public static class CsvLoader
{
    public static async Task<ImmutableArray<TRecord>> LoadAsync<TRecord>(
        string filePath,
        Func<CsvHeaderIndex, string[], TRecord> mapFromCsvRow)
    {
        var content = await File.ReadAllTextAsync(filePath);

        return Parse(content, mapFromCsvRow, filePath);
    }

    public static ImmutableArray<TRecord> Parse<TRecord>(
        string csvContent,
        Func<CsvHeaderIndex, string[], TRecord> mapFromCsvRow,
        string? filePath = null)
    {
        var rows = ParseCsvContent(csvContent, filePath);
        if (rows.Count == 0)
        {
            return [];
        }

        var headers = new CsvHeaderIndex(rows[0], filePath);
        var headerCellCount = rows[0].Length;

        var builder = ImmutableArray.CreateBuilder<TRecord>(rows.Count - 1);

        for (var i = 1; i < rows.Count; i++)
        {
            var values = rows[i];
            if (values.Length == 1 && string.IsNullOrWhiteSpace(values[0]))
            {
                continue;
            }

            try
            {
                // 필드가 부족한 경우
                if (values.Length < headerCellCount)
                {
                    throw new InvalidOperationException(string.Format(
                        CultureInfo.CurrentCulture,
                        Messages.Composite.CsvRowFieldCountShortage,
                        values.Length,
                        headerCellCount));
                }

                builder.Add(mapFromCsvRow(headers, values));
            }
            catch (Exception ex)
            {
                var location = filePath is not null
                    ? string.Format(
                        CultureInfo.CurrentCulture,
                        Messages.Composite.CsvRowWithFile,
                        Path.GetFileName(filePath),
                        i + 1)
                    : string.Format(
                        CultureInfo.CurrentCulture,
                        Messages.Composite.CsvRowWithoutFile,
                        i + 1);
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Messages.Composite.CsvRowParseError,
                        location,
                        ex.Message),
                    ex);
            }
        }

        builder.Capacity = builder.Count;

        return builder.MoveToImmutable();
    }

    private static List<string[]> ParseCsvContent(string content, string? filePath)
    {
        var rows = new List<string[]>();
        var fields = new List<string>();
        var field = new StringBuilder();
        var inQuotes = false;
        var i = 0;

        while (i < content.Length)
        {
            var c = content[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < content.Length && content[i + 1] == '"')
                    {
                        field.Append('"');
                        i += 2;
                    }
                    else
                    {
                        inQuotes = false;
                        i++;
                    }
                }
                else
                {
                    // 따옴표 안의 개행(CRLF 포함)은 RFC 4180 대로 원문 그대로 보존
                    field.Append(c);
                    i++;
                }
            }
            else
            {
                if (c == '"')
                {
                    inQuotes = true;
                    i++;
                }
                else if (c == ',')
                {
                    fields.Add(field.ToString());
                    field.Clear();
                    i++;
                }
                else if (c == '\r')
                {
                    fields.Add(field.ToString());
                    rows.Add(fields.ToArray());
                    fields.Clear();
                    field.Clear();
                    i++;
                    if (i < content.Length && content[i] == '\n')
                    {
                        i++;
                    }
                }
                else if (c == '\n')
                {
                    fields.Add(field.ToString());
                    rows.Add(fields.ToArray());
                    fields.Clear();
                    field.Clear();
                    i++;
                }
                else
                {
                    field.Append(c);
                    i++;
                }
            }
        }

        // 따옴표가 닫히지 않은 채 끝난 경우
        if (inQuotes)
        {
            var fileLabel = filePath is not null
                ? string.Format(
                    CultureInfo.CurrentCulture,
                    Messages.Composite.CsvFileLabel,
                    Path.GetFileName(filePath))
                : string.Empty;
            throw new InvalidOperationException(fileLabel + Messages.CsvUnterminatedQuote);
        }

        if (field.Length > 0 || fields.Count > 0)
        {
            fields.Add(field.ToString());
            rows.Add(fields.ToArray());
        }

        return rows;
    }
}
