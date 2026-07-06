using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Sdp.SourceGenerator.Generators;

internal static partial class CsvMapperEmitter
{
    private static string EmitConversion(
        ParameterAnalysis param,
        string? basePathVar,
        string ownerToken,
        Dictionary<INamedTypeSymbol, string> nestedTokens)
    {
        if (param.IsIgnored)
        {
            return "default!";
        }

        var keyLiteral = SymbolDisplay.FormatLiteral(param.ColumnName, quote: true);
        var keyExpr = basePathVar is null
            ? keyLiteral
            : $"{basePathVar} + \".\" + {keyLiteral}";
        var valueExpr = $"values[headers[{keyExpr}]]";

        if (param.Collection is not null)
        {
            var helperPrefix = param.Collection.Kind switch
            {
                CollectionKind.ImmutableArray => "__MapArray_",
                CollectionKind.FrozenSet => "__MapSet_",
                CollectionKind.FrozenDictionary => "__MapDict_",
                CollectionKind.SingleColumnImmutableArray => "__MapSingleArray_",
                CollectionKind.SingleColumnFrozenSet => "__MapSingleSet_",
                _ => throw new InvalidOperationException(FormattableString.Invariant(
                    $"Unsupported collection kind: {param.Collection.Kind}")),
            };
            var basePathArg = basePathVar ?? "string.Empty";
            return $"{HelperName(helperPrefix, ownerToken, param.Name)}(headers, values, {basePathArg})";
        }

        if (param.Nested is { } nested)
        {
            return $"__MapNested_{nestedTokens[nested.Symbol]}(headers, values, {keyExpr})";
        }

        if (param.IsNullable && param.NullString is not null)
        {
            return $"{HelperName("__MapNullable_", ownerToken, param.Name)}({valueExpr})";
        }

        var baseConversion = EmitBaseConversion(param, valueExpr, ownerToken);
        return WrapValidations(param, baseConversion, ownerToken);
    }

    private static string WrapValidations(ParameterAnalysis param, string inner, string ownerToken)
    {
        if (param.Range is not null)
        {
            inner = $"{HelperName("__ValidateRange_", ownerToken, param.Name)}({inner})";
        }

        if (param.RegexPattern is not null)
        {
            inner = $"{HelperName("__ValidatePattern_", ownerToken, param.Name)}({inner})";
        }

        return inner;
    }

    private static string EmitBaseConversion(ParameterAnalysis param, string valueExpr, string ownerToken)
    {
        if (param.Kind == ScalarKind.Enum)
        {
            return $"{HelperName("__MapColumn_", ownerToken, param.Name)}({valueExpr})";
        }

        return EmitScalarParse(param.Kind, valueExpr, param.DateTimeFormat, param.TimeSpanFormat);
    }

    private static string EmitScalarParse(
        ScalarKind kind,
        string valueExpr,
        string? dateTimeFormat = null,
        string? timeSpanFormat = null)
    {
        return kind switch
        {
            ScalarKind.Bool => $"bool.Parse({valueExpr})",
            ScalarKind.Byte => InvariantIntegerParse("byte", valueExpr),
            ScalarKind.SByte => InvariantIntegerParse("sbyte", valueExpr),
            ScalarKind.Int16 => InvariantIntegerParse("short", valueExpr),
            ScalarKind.UInt16 => InvariantIntegerParse("ushort", valueExpr),
            ScalarKind.Int32 => InvariantIntegerParse("int", valueExpr),
            ScalarKind.UInt32 => InvariantIntegerParse("uint", valueExpr),
            ScalarKind.Int64 => InvariantIntegerParse("long", valueExpr),
            ScalarKind.UInt64 => InvariantIntegerParse("ulong", valueExpr),
            ScalarKind.Single => InvariantFloatParse("float", valueExpr),
            ScalarKind.Double => InvariantFloatParse("double", valueExpr),
            ScalarKind.Decimal => InvariantDecimalParse(valueExpr),
            ScalarKind.String => valueExpr,
            ScalarKind.Char => $"char.Parse({valueExpr})",
            ScalarKind.Guid => $"global::System.Guid.Parse({valueExpr}, global::System.Globalization.CultureInfo.InvariantCulture)",
            ScalarKind.DateTime => InvariantParseExact("global::System.DateTime", valueExpr, dateTimeFormat!),
            ScalarKind.DateTimeOffset => InvariantParseExact("global::System.DateTimeOffset", valueExpr, dateTimeFormat!),
            ScalarKind.DateOnly => InvariantParseExact("global::System.DateOnly", valueExpr, dateTimeFormat!),
            ScalarKind.TimeOnly => InvariantParseExact("global::System.TimeOnly", valueExpr, dateTimeFormat!),
            ScalarKind.TimeSpan => InvariantParseExact("global::System.TimeSpan", valueExpr, timeSpanFormat!),
            _ => valueExpr,
        };
    }

    private static void EmitNullableScalarHelper(StringBuilder sb, ParameterAnalysis param, string indent, string ownerToken)
    {
        var returnType = NullableScalarTypeName(param);
        var nullLiteral = SymbolDisplay.FormatLiteral(param.NullString!, quote: true);
        var conversion = WrapValidations(param, EmitBaseConversion(param, "value", ownerToken), ownerToken);

        sb.Append(indent).Append("private static ").Append(returnType).Append(' ')
            .Append(HelperName("__MapNullable_", ownerToken, param.Name)).AppendLine("(string value)");
        sb.Append(indent).AppendLine("{");
        sb.Append(indent).Append("    if (value == ").Append(nullLiteral).AppendLine(")");
        sb.Append(indent).AppendLine("    {");
        sb.Append(indent).AppendLine("        return null;");
        sb.Append(indent).AppendLine("    }");
        sb.AppendLine();
        sb.Append(indent).Append("    return ").Append(conversion).AppendLine(";");
        sb.Append(indent).AppendLine("}");
    }

    private static string NullableScalarTypeName(ParameterAnalysis param)
    {
        if (param.Kind == ScalarKind.Enum)
        {
            var enumName = TypeClassifier.UnwrapNullable(param.Type).ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            return enumName + "?";
        }

        return ScalarTypeKeyword(param.Kind) + "?";
    }

    private static string ScalarTypeKeyword(ScalarKind kind)
    {
        return kind switch
        {
            ScalarKind.Bool => "bool",
            ScalarKind.Byte => "byte",
            ScalarKind.SByte => "sbyte",
            ScalarKind.Int16 => "short",
            ScalarKind.UInt16 => "ushort",
            ScalarKind.Int32 => "int",
            ScalarKind.UInt32 => "uint",
            ScalarKind.Int64 => "long",
            ScalarKind.UInt64 => "ulong",
            ScalarKind.Single => "float",
            ScalarKind.Double => "double",
            ScalarKind.Decimal => "decimal",
            ScalarKind.String => "string",
            ScalarKind.Char => "char",
            ScalarKind.Guid => "global::System.Guid",
            ScalarKind.DateTime => "global::System.DateTime",
            ScalarKind.DateTimeOffset => "global::System.DateTimeOffset",
            ScalarKind.DateOnly => "global::System.DateOnly",
            ScalarKind.TimeOnly => "global::System.TimeOnly",
            ScalarKind.TimeSpan => "global::System.TimeSpan",
            _ => throw new InvalidOperationException(FormattableString.Invariant(
                $"Unsupported scalar kind: {kind}")),
        };
    }

    private static string InvariantIntegerParse(string typeKeyword, string valueExpr)
        => $"{typeKeyword}.Parse({valueExpr}, global::System.Globalization.NumberStyles.Integer, global::System.Globalization.CultureInfo.InvariantCulture)";

    private static string InvariantFloatParse(string typeKeyword, string valueExpr)
        => $"{typeKeyword}.Parse({valueExpr}, global::System.Globalization.NumberStyles.Float | global::System.Globalization.NumberStyles.AllowThousands, global::System.Globalization.CultureInfo.InvariantCulture)";

    private static string InvariantDecimalParse(string valueExpr)
        => $"decimal.Parse({valueExpr}, global::System.Globalization.NumberStyles.Number, global::System.Globalization.CultureInfo.InvariantCulture)";

    private static string InvariantParseExact(string typeName, string valueExpr, string format)
    {
        var formatLiteral = SymbolDisplay.FormatLiteral(format, quote: true);
        return $"{typeName}.ParseExact({valueExpr}, {formatLiteral}, global::System.Globalization.CultureInfo.InvariantCulture)";
    }
}
