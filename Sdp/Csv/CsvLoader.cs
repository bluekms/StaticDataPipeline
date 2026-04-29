using System.Collections.Immutable;
using System.Globalization;
using System.Reflection;
using System.Text;
using Sdp.Resources;

namespace Sdp.Csv;

internal static class CsvLoader
{
    private static readonly MethodInfo BuildImmutableListGenericMethod = typeof(CsvLoader)
        .GetMethod(nameof(BuildImmutableListTyped), BindingFlags.NonPublic | BindingFlags.Static)!;

    public static async Task<object> LoadAsync(string filePath, Type recordType)
    {
        var content = await File.ReadAllTextAsync(filePath);
        return Parse(content, recordType, filePath);
    }

    public static async Task<ImmutableList<TRecord>> LoadAsync<TRecord>(string filePath)
        where TRecord : notnull
        => (ImmutableList<TRecord>)await LoadAsync(filePath, typeof(TRecord));

    private static object Parse(string csvContent, Type recordType, string? filePath = null)
    {
        var rows = ParseCsvContent(csvContent);
        if (rows.Count == 0)
        {
            return BuildEmptyList(recordType);
        }

        var headers = rows[0];
        var records = new List<object>(rows.Count - 1);

        for (var i = 1; i < rows.Count; i++)
        {
            var values = rows[i];
            if (values.Length == 1 && string.IsNullOrWhiteSpace(values[0]))
            {
                continue;
            }

            try
            {
                var record = CsvRecordMapper.MapToRecord(recordType, headers, values);
                records.Add(record);
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

        return BuildImmutableList(recordType, records);
    }

    public static ImmutableList<TRecord> Parse<TRecord>(string csvContent, string? filePath = null)
        where TRecord : notnull
        => (ImmutableList<TRecord>)Parse(csvContent, typeof(TRecord), filePath);

    private static object BuildImmutableList(Type recordType, List<object> records)
        => BuildImmutableListGenericMethod.MakeGenericMethod(recordType).Invoke(null, [records])!;

    private static object BuildEmptyList(Type recordType)
    => BuildImmutableListGenericMethod.MakeGenericMethod(recordType).Invoke(null, [new List<object>()])!;

    private static ImmutableList<T> BuildImmutableListTyped<T>(List<object> records)
        where T : notnull
        => records.Cast<T>().ToImmutableList();

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
