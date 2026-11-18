using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Sdp.SourceGenerator.Generators;

internal static partial class CsvMapperEmitter
{
    private const string NullableBrandingHelperPrefix = "__MapNullableBranding_";

    private static void EmitNestedHelper(
        StringBuilder sb,
        NestedRecordInfo info,
        string indent,
        Dictionary<INamedTypeSymbol, string> nestedTokens)
    {
        var typeFullName = info.Symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var token = nestedTokens[info.Symbol];
        sb.Append(indent).Append("private static ").Append(typeFullName).Append(" __MapNested_").Append(token).AppendLine("(");
        sb.Append(indent).AppendLine("    global::Sdp.Csv.CsvHeaderIndex headers,");
        sb.Append(indent).AppendLine("    string[] values,");
        sb.Append(indent).AppendLine("    string basePath)");
        sb.Append(indent).AppendLine("{");

        if (info.IsTypeBrandingRecord)
        {
            EmitBrandingHelperBody(sb, info, indent, token, nullString: null);

            return;
        }

        sb.Append(indent).Append("    return new ").Append(typeFullName).AppendLine("(");

        for (var i = 0; i < info.Parameters.Length; i++)
        {
            var param = info.Parameters[i];
            sb.Append(indent).Append("        ");
            sb.Append(EmitHelper.EscapeIdentifier(param.Name)).Append(": ");
            sb.Append(EmitConversion(param, basePathVar: "basePath", ownerToken: token, nestedTokens));

            if (i < info.Parameters.Length - 1)
            {
                sb.Append(',');
            }

            sb.AppendLine();
        }

        sb.Append(indent).AppendLine("    );");
        sb.Append(indent).AppendLine("}");
    }

    private static void EmitNullableBrandingHelper(
        StringBuilder sb,
        ParameterAnalysis param,
        NestedRecordInfo info,
        string indent,
        string ownerToken,
        Dictionary<INamedTypeSymbol, string> nestedTokens)
    {
        var typeFullName = info.Symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        sb.Append(indent).Append("private static ").Append(typeFullName).Append("? ")
            .Append(HelperName(NullableBrandingHelperPrefix, ownerToken, param.Name)).AppendLine("(");
        sb.Append(indent).AppendLine("    global::Sdp.Csv.CsvHeaderIndex headers,");
        sb.Append(indent).AppendLine("    string[] values,");
        sb.Append(indent).AppendLine("    string basePath)");
        sb.Append(indent).AppendLine("{");

        EmitBrandingHelperBody(sb, info, indent, nestedTokens[info.Symbol], param.NullString);
    }

    private static void EmitBrandingHelperBody(
        StringBuilder sb,
        NestedRecordInfo info,
        string indent,
        string token,
        string? nullString)
    {
        var typeFullName = info.Symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var only = info.Parameters[0];
        var columnNameLiteral = SymbolDisplay.FormatLiteral(only.ColumnName, quote: true);
        sb.Append(indent).Append("    var __key = headers.Contains(basePath) ? basePath : basePath + \".\" + ")
            .Append(columnNameLiteral).AppendLine(";");
        sb.Append(indent).AppendLine("    var __raw = values[headers[__key]];");

        if (nullString is not null)
        {
            var nullLiteral = SymbolDisplay.FormatLiteral(nullString, quote: true);
            sb.Append(indent).Append("    if (__raw == ").Append(nullLiteral).AppendLine(")");
            sb.Append(indent).AppendLine("    {");
            sb.Append(indent).AppendLine("        return null;");
            sb.Append(indent).AppendLine("    }");
            sb.AppendLine();
        }

        var conversion = EmitScalarConversion(only, "__raw", "__key", token);
        sb.Append(indent).Append("    return new ").Append(typeFullName).Append('(').Append(conversion).AppendLine(");");
        sb.Append(indent).AppendLine("}");
    }

    private static void EmitCollectionElementNullableBrandingHelper(
        StringBuilder sb,
        ParameterAnalysis param,
        string indent,
        string ownerToken,
        Dictionary<INamedTypeSymbol, string> nestedTokens)
    {
        if (param.Collection is not { ElementNested: { } elementNested } collection)
        {
            return;
        }

        if (!TypeClassifier.IsNullable(collection.ElementType))
        {
            return;
        }

        EmitNullableBrandingHelper(sb, param, elementNested, indent, ownerToken, nestedTokens);
        sb.AppendLine();
    }

    private static void EmitCollectionElementNestedHelpers(
        StringBuilder sb,
        ParameterAnalysis param,
        HashSet<INamedTypeSymbol> emittedHelperTypes,
        string indent,
        Dictionary<INamedTypeSymbol, string> nestedTokens)
    {
        if (param.Collection?.ElementNested is not { } elementNested)
        {
            return;
        }

        if (!emittedHelperTypes.Add(elementNested.Symbol))
        {
            return;
        }

        EmitNestedHelper(sb, elementNested, indent, nestedTokens);
        sb.AppendLine();

        EmitHelpersRecursive(
            sb,
            elementNested.Parameters,
            emittedHelperTypes,
            indent,
            nestedTokens[elementNested.Symbol],
            nestedTokens);

        sb.AppendLine();
    }

    private static void EmitCollectionElementEnumHelper(
        StringBuilder sb,
        ParameterAnalysis param,
        HashSet<INamedTypeSymbol> emittedHelperTypes,
        string indent,
        Dictionary<INamedTypeSymbol, string> nestedTokens)
    {
        if (param.Collection is not { ElementKind: ScalarKind.Enum, ElementNested: null } info)
        {
            return;
        }

        var enumType = (INamedTypeSymbol)TypeClassifier.UnwrapNullable(info.ElementType);
        if (!emittedHelperTypes.Add(enumType))
        {
            return;
        }

        EmitEnumSwitchHelper(
            sb,
            enumType,
            indent,
            "__MapElementEnum_" + nestedTokens[enumType],
            isKey: false);

        sb.AppendLine();
    }
}
