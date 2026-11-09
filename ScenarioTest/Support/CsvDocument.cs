using System.Text;

namespace ScenarioTest.Support;

public sealed class CsvDocument
{
    private static readonly UTF8Encoding Utf8NoBom = new(false);

    private CsvDocument(List<string> headers, List<List<string>> rows)
    {
        Headers = headers;
        Rows = rows;
    }

    public List<string> Headers { get; }

    public List<List<string>> Rows { get; }

    public static CsvDocument Load(string path)
    {
        var records = Parse(File.ReadAllText(path, Utf8NoBom));
        if (records.Count == 0)
        {
            return new CsvDocument([], []);
        }

        return new CsvDocument(records[0], records.Skip(1).ToList());
    }

    public static void Edit(string path, Action<CsvDocument> edit)
    {
        var document = Load(path);
        edit(document);
        document.Save(path);
    }

    public void Save(string path)
    {
        var sb = new StringBuilder();
        AppendRecord(sb, Headers);
        foreach (var row in Rows)
        {
            AppendRecord(sb, row);
        }

        File.WriteAllText(path, sb.ToString(), Utf8NoBom);
    }

    public int ColumnIndex(string header)
    {
        var index = Headers.IndexOf(header);
        if (index < 0)
        {
            throw new ArgumentException(FormattableString.Invariant($"CSV 에 헤더 '{header}' 가 없다."));
        }

        return index;
    }

    public List<string> Row(string keyHeader, string keyValue)
    {
        var column = ColumnIndex(keyHeader);
        var row = Rows.FirstOrDefault(x => x[column] == keyValue);
        if (row is null)
        {
            throw new ArgumentException(FormattableString.Invariant($"CSV 에 {keyHeader} 가 '{keyValue}' 인 행이 없다."));
        }

        return row;
    }

    public void Set(string keyHeader, string keyValue, string header, string value)
    {
        Row(keyHeader, keyValue)[ColumnIndex(header)] = value;
    }

    public void RemoveColumn(string header)
    {
        var column = ColumnIndex(header);
        Headers.RemoveAt(column);
        foreach (var row in Rows)
        {
            row.RemoveAt(column);
        }
    }

    public void AddColumn(string header, string value)
    {
        Headers.Add(header);
        foreach (var row in Rows)
        {
            row.Add(value);
        }
    }

    public void AddRow(params string[] cells)
    {
        Rows.Add([.. cells]);
    }

    public void DuplicateRow(string keyHeader, string keyValue)
    {
        Rows.Add([.. Row(keyHeader, keyValue)]);
    }

    private static void AppendRecord(StringBuilder sb, List<string> cells)
    {
        sb.Append(string.Join(",", cells.Select(Quote)));
        sb.Append('\n');
    }

    private static string Quote(string cell)
    {
        var needsQuoting = cell.IndexOfAny([',', '"', '\n', '\r']) >= 0;
        if (!needsQuoting)
        {
            return cell;
        }

        return "\"" + cell.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    }

    private static List<List<string>> Parse(string text)
    {
        var records = new List<List<string>>();
        var record = new List<string>();
        var field = new StringBuilder();
        var inQuotes = false;
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (inQuotes)
            {
                if (c != '"')
                {
                    field.Append(c);
                    continue;
                }

                var escaped = i + 1 < text.Length && text[i + 1] == '"';
                if (escaped)
                {
                    field.Append('"');
                    i++;
                    continue;
                }

                inQuotes = false;
                continue;
            }

            switch (c)
            {
                case '"':
                    inQuotes = true;
                    break;
                case ',':
                    record.Add(field.ToString());
                    field.Clear();
                    break;
                case '\r':
                    break;
                case '\n':
                    record.Add(field.ToString());
                    field.Clear();
                    records.Add(record);
                    record = [];
                    break;
                default:
                    field.Append(c);
                    break;
            }
        }

        if (field.Length > 0 || record.Count > 0)
        {
            record.Add(field.ToString());
            records.Add(record);
        }

        return records;
    }
}
