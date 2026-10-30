using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;

namespace Sdp.SourceGenerator.Generators;

internal static partial class CsvMapperEmitter
{
    private static void EmitEmptyRowCheck(StringBuilder sb, ImmutableArray<ParameterAnalysis> parameters, string indent)
    {
        var columns = new List<string>();
        var prefixes = new List<string>();
        CollectCsvColumns(parameters, string.Empty, columns, prefixes);

        sb.AppendLine();
        sb.Append(indent).AppendLine("internal static bool IsEmptyCsvRow(global::Sdp.Csv.CsvHeaderIndex headers, string[] values)");
        sb.Append(indent).AppendLine("{");
        sb.Append(indent).AppendLine("    return global::Sdp.Csv.CsvLoader.IsEmptyRow(headers, values, __csvColumns, __csvColumnPrefixes);");
        sb.Append(indent).AppendLine("}");
        sb.AppendLine();
        EmitStringArray(sb, indent, "__csvColumns", columns);
        EmitStringArray(sb, indent, "__csvColumnPrefixes", prefixes);
    }

    private static void EmitStringArray(StringBuilder sb, string indent, string name, List<string> items)
    {
        var literals = items.Select(item => SymbolDisplay.FormatLiteral(item, quote: true));
        sb.Append(indent).Append("private static readonly string[] ").Append(name).Append(" = new string[] { ")
            .Append(string.Join(", ", literals)).AppendLine(" };");
    }

    private static void CollectCsvColumns(
        ImmutableArray<ParameterAnalysis> parameters,
        string basePath,
        List<string> columns,
        List<string> prefixes)
    {
        foreach (var param in parameters)
        {
            if (param.IsIgnored)
            {
                continue;
            }

            var path = basePath.Length == 0 ? param.ColumnName : basePath + "." + param.ColumnName;

            if (param.Collection is { } collection)
            {
                var isSingleColumn = collection.Kind is CollectionKind.SingleColumnImmutableArray or CollectionKind.SingleColumnFrozenSet;
                if (isSingleColumn)
                {
                    columns.Add(path);
                }
                else
                {
                    prefixes.Add(path + "[");
                }

                continue;
            }

            if (param.Nested is { } nested)
            {
                if (nested.IsTypeBrandingRecord)
                {
                    columns.Add(path);
                    columns.Add(path + "." + nested.Parameters[0].ColumnName);
                }
                else
                {
                    CollectCsvColumns(nested.Parameters, path, columns, prefixes);
                }

                continue;
            }

            columns.Add(path);
        }
    }
}
