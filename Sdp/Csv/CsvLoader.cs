using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using Sdp.Resources;

namespace Sdp.Csv;

public static class CsvLoader
{
    public static async Task<ImmutableArray<TRecord>> LoadAsync<TRecord>(
        string filePath,
        Func<CsvHeaderIndex, string[], TRecord> mapFromCsvRow,
        Func<CsvHeaderIndex, string[], bool>? isEmptyRow = null)
    {
        var content = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);

        return Parse(content, mapFromCsvRow, filePath, isEmptyRow);
    }

    public static ImmutableArray<TRecord> Parse<TRecord>(
        string csvContent,
        Func<CsvHeaderIndex, string[], TRecord> mapFromCsvRow,
        string? filePath = null,
        Func<CsvHeaderIndex, string[], bool>? isEmptyRow = null)
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

                if (isEmptyRow is not null && isEmptyRow(headers, values))
                {
                    continue;
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

    // 추출기(BodyColumnAggregator)가 record 의 열이 모두 빈 행을 버리는 규칙과 같아야 한다.
    public static bool IsEmptyRow(CsvHeaderIndex headers, string[] values, string[] columns, string[] columnPrefixes)
    {
        var anyColumnPresent = false;
        foreach (var column in columns)
        {
            if (!headers.Contains(column))
            {
                continue;
            }

            anyColumnPresent = true;
            if (values[headers[column]].Length > 0)
            {
                return false;
            }
        }

        foreach (var prefix in columnPrefixes)
        {
            foreach (var name in headers.ColumnNames)
            {
                if (!name.StartsWith(prefix, StringComparison.Ordinal))
                {
                    continue;
                }

                anyColumnPresent = true;
                if (values[headers[name]].Length > 0)
                {
                    return false;
                }
            }
        }

        return anyColumnPresent;
    }

    private static List<string[]> ParseCsvContent(string content, string? filePath)
    {
        var rows = new List<string[]>();
        var fields = new List<string>();
        var field = new StringBuilder();
        var inQuotes = false;
        var rowHasQuotedField = false;
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
                        if (i < content.Length && content[i] is not (',' or '\r' or '\n'))
                        {
                            throw new InvalidOperationException(FileLabel(filePath) + string.Format(
                                CultureInfo.CurrentCulture,
                                Messages.Composite.CsvTextAfterClosingQuote,
                                rows.Count + 1));
                        }
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
                    if (field.Length > 0)
                    {
                        throw new InvalidOperationException(FileLabel(filePath) + string.Format(
                            CultureInfo.CurrentCulture,
                            Messages.Composite.CsvQuoteInUnquotedField,
                            rows.Count + 1));
                    }

                    inQuotes = true;
                    rowHasQuotedField = true;
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
                    rowHasQuotedField = false;
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
                    rowHasQuotedField = false;
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
            throw new InvalidOperationException(FileLabel(filePath) + Messages.CsvUnterminatedQuote);
        }

        if (field.Length > 0 || fields.Count > 0 || rowHasQuotedField)
        {
            fields.Add(field.ToString());
            rows.Add(fields.ToArray());
        }

        return rows;
    }

    private static string FileLabel(string? filePath)
    {
        if (filePath is null)
        {
            return string.Empty;
        }

        return string.Format(CultureInfo.CurrentCulture, Messages.Composite.CsvFileLabel, Path.GetFileName(filePath));
    }
}
