using System.Collections.Immutable;
using System.Text;

namespace Sdp.Csv;

internal static class CsvLoader
{
    public static ImmutableList<TRecord> Load<TRecord>(string filePath)
        where TRecord : notnull
    {
        var content = File.ReadAllText(filePath);
        return Parse<TRecord>(content);
    }

    public static async Task<ImmutableList<TRecord>> LoadAsync<TRecord>(string filePath)
        where TRecord : notnull
    {
        var content = await File.ReadAllTextAsync(filePath);
        return Parse<TRecord>(content);
    }

    public static ImmutableList<TRecord> Parse<TRecord>(string csvContent)
        where TRecord : notnull
    {
        var rows = ParseCsvContent(csvContent);
        if (rows.Count == 0)
        {
            return ImmutableList<TRecord>.Empty;
        }

        var headers = rows[0];
        var builder = ImmutableList.CreateBuilder<TRecord>();

        for (var i = 1; i < rows.Count; i++)
        {
            var values = rows[i];
            if (values.Length == 1 && string.IsNullOrWhiteSpace(values[0]))
            {
                continue;
            }

            var record = CsvRecordMapper.MapToRecord<TRecord>(headers, values);
            builder.Add(record);
        }

        return builder.ToImmutable();
    }

    private static List<string[]> ParseCsvContent(string content)
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
                else if (c == '\r')
                {
                    field.Append('\n');
                    i++;
                    if (i < content.Length && content[i] == '\n')
                    {
                        i++;
                    }
                }
                else
                {
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

        if (field.Length > 0 || fields.Count > 0)
        {
            fields.Add(field.ToString());
            rows.Add(fields.ToArray());
        }

        return rows;
    }
}
