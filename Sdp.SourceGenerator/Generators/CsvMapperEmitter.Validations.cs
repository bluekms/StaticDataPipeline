using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Sdp.SourceGenerator.Generators;

internal static partial class CsvMapperEmitter
{
    private static void EmitNumericRangeHelper(
        StringBuilder sb,
        ParameterAnalysis param,
        ScalarKind kind,
        string indent,
        string ownerToken)
    {
        var typeKeyword = ScalarTypeKeyword(kind);
        var rawMin = SymbolDisplay.FormatPrimitive(param.Range!.Minimum!, quoteStrings: false, useHexadecimalNumbers: false);
        var rawMax = SymbolDisplay.FormatPrimitive(param.Range!.Maximum!, quoteStrings: false, useHexadecimalNumbers: false);

        var suffix = kind switch
        {
            ScalarKind.Decimal => "m",
            ScalarKind.Single => "f",
            ScalarKind.Double => "d",
            _ => string.Empty,
        };
        var lowerBound = rawMin + suffix;
        var upperBound = rawMax + suffix;

        sb.Append(indent).Append("private static ").Append(typeKeyword).Append(' ')
            .Append(HelperName("__ValidateRange_", ownerToken, param.Name)).Append('(').Append(typeKeyword).AppendLine(" value)");
        sb.Append(indent).AppendLine("{");
        sb.Append(indent).Append("    if (value < ").Append(lowerBound).Append(" || value > ").Append(upperBound).AppendLine(")");
        sb.Append(indent).AppendLine("    {");
        sb.Append(indent).Append("        throw new global::System.ArgumentOutOfRangeException(nameof(value), value, \"'");
        sb.Append(param.Name).Append("' must be within [").Append(rawMin).Append(", ").Append(rawMax).AppendLine("].\");");
        sb.Append(indent).AppendLine("    }");
        sb.AppendLine();
        sb.Append(indent).AppendLine("    return value;");
        sb.Append(indent).AppendLine("}");
    }

    private static void EmitTypedRangeHelper(
        StringBuilder sb,
        ParameterAnalysis param,
        ScalarKind kind,
        ITypeSymbol type,
        string indent,
        string ownerToken)
    {
        if (kind == ScalarKind.String)
        {
            EmitStringRangeHelper(sb, param, indent, ownerToken);
        }
        else if (kind == ScalarKind.Enum)
        {
            EmitEnumRangeHelper(sb, param, type, indent, ownerToken);
        }
        else
        {
            // DateTime/DateTimeOffset/TimeSpan/숫자 : min/max 문자열을 값 타입으로 한 번만 파싱해 비교한다.
            EmitParsedBoundRangeHelper(sb, param, kind, indent, ownerToken);
        }
    }

    private static void EmitParsedBoundRangeHelper(
        StringBuilder sb,
        ParameterAnalysis param,
        ScalarKind kind,
        string indent,
        string ownerToken)
    {
        var typeKeyword = ScalarTypeKeyword(kind);
        var minText = (string)param.Range!.Minimum!;
        var maxText = (string)param.Range!.Maximum!;
        var minLiteral = SymbolDisplay.FormatLiteral(minText, quote: true);
        var maxLiteral = SymbolDisplay.FormatLiteral(maxText, quote: true);
        var minExpr = EmitScalarParse(kind, minLiteral, param.DateTimeFormat, param.TimeSpanFormat);
        var maxExpr = EmitScalarParse(kind, maxLiteral, param.DateTimeFormat, param.TimeSpanFormat);
        var minField = HelperName("__RangeMin_", ownerToken, param.Name);
        var maxField = HelperName("__RangeMax_", ownerToken, param.Name);

        sb.Append(indent).Append("private static readonly ").Append(typeKeyword).Append(' ').Append(minField)
            .Append(" = ").Append(minExpr).AppendLine(";");
        sb.Append(indent).Append("private static readonly ").Append(typeKeyword).Append(' ').Append(maxField)
            .Append(" = ").Append(maxExpr).AppendLine(";");
        sb.AppendLine();
        sb.Append(indent).Append("private static ").Append(typeKeyword).Append(' ')
            .Append(HelperName("__ValidateRange_", ownerToken, param.Name)).Append('(').Append(typeKeyword).AppendLine(" value)");
        sb.Append(indent).AppendLine("{");
        sb.Append(indent).Append("    if (value < ").Append(minField).Append(" || value > ").Append(maxField).AppendLine(")");
        sb.Append(indent).AppendLine("    {");
        AppendRangeMessage(sb, indent, param, minText, maxText);
        sb.Append(indent).AppendLine("    }");
        sb.AppendLine();
        sb.Append(indent).AppendLine("    return value;");
        sb.Append(indent).AppendLine("}");
    }

    private static void EmitStringRangeHelper(
        StringBuilder sb,
        ParameterAnalysis param,
        string indent,
        string ownerToken)
    {
        var minText = (string)param.Range!.Minimum!;
        var maxText = (string)param.Range!.Maximum!;
        var minLiteral = SymbolDisplay.FormatLiteral(minText, quote: true);
        var maxLiteral = SymbolDisplay.FormatLiteral(maxText, quote: true);

        sb.Append(indent).Append("private static string ")
            .Append(HelperName("__ValidateRange_", ownerToken, param.Name)).AppendLine("(string value)");
        sb.Append(indent).AppendLine("{");
        sb.Append(indent).Append("    if (string.CompareOrdinal(value, ").Append(minLiteral)
            .Append(") < 0 || string.CompareOrdinal(value, ").Append(maxLiteral).AppendLine(") > 0)");
        sb.Append(indent).AppendLine("    {");
        AppendRangeMessage(sb, indent, param, minText, maxText);
        sb.Append(indent).AppendLine("    }");
        sb.AppendLine();
        sb.Append(indent).AppendLine("    return value;");
        sb.Append(indent).AppendLine("}");
    }

    private static void EmitEnumRangeHelper(
        StringBuilder sb,
        ParameterAnalysis param,
        ITypeSymbol type,
        string indent,
        string ownerToken)
    {
        var enumType = (INamedTypeSymbol)TypeClassifier.UnwrapNullable(type);
        var enumFullName = enumType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var isUnsigned64 = enumType.EnumUnderlyingType!.SpecialType == SpecialType.System_UInt64;
        var accumulatorKeyword = isUnsigned64 ? "ulong" : "long";
        var accumulatorSuffix = isUnsigned64 ? "UL" : "L";
        var minExpr = EnumBoundExpr(enumType, enumFullName, param.Range!.Minimum, accumulatorKeyword, accumulatorSuffix);
        var maxExpr = EnumBoundExpr(enumType, enumFullName, param.Range!.Maximum, accumulatorKeyword, accumulatorSuffix);
        var minText = param.Range!.Minimum?.ToString() ?? string.Empty;
        var maxText = param.Range!.Maximum?.ToString() ?? string.Empty;

        sb.Append(indent).Append("private static ").Append(enumFullName).Append(' ')
            .Append(HelperName("__ValidateRange_", ownerToken, param.Name)).Append('(').Append(enumFullName).AppendLine(" value)");
        sb.Append(indent).AppendLine("{");
        sb.Append(indent).Append("    var __v = (").Append(accumulatorKeyword).AppendLine(")value;");
        sb.Append(indent).Append("    if (__v < ").Append(minExpr).Append(" || __v > ").Append(maxExpr).AppendLine(")");
        sb.Append(indent).AppendLine("    {");
        AppendRangeMessage(sb, indent, param, minText, maxText);
        sb.Append(indent).AppendLine("    }");
        sb.AppendLine();
        sb.Append(indent).AppendLine("    return value;");
        sb.Append(indent).AppendLine("}");
    }

    private static void AppendRangeMessage(
        StringBuilder sb,
        string indent,
        ParameterAnalysis param,
        string minText,
        string maxText)
    {
        var message = "'" + param.Name + "' must be within [" + minText + ", " + maxText + "].";
        var messageLiteral = SymbolDisplay.FormatLiteral(message, quote: true);
        sb.Append(indent).Append("        throw new global::System.ArgumentOutOfRangeException(nameof(value), value, ");
        sb.Append(messageLiteral).AppendLine(");");
    }

    private static string EnumBoundExpr(
        INamedTypeSymbol enumType,
        string enumFullName,
        object? bound,
        string accumulatorKeyword,
        string accumulatorSuffix)
    {
        var text = bound as string ?? bound?.ToString() ?? "0";
        var isMember = enumType.GetMembers()
            .OfType<IFieldSymbol>()
            .Any(f => f.IsConst && f.Name == text);
        if (isMember)
        {
            return "(" + accumulatorKeyword + ")" + enumFullName + "." + text;
        }

        return text + accumulatorSuffix;
    }

    private static void EmitRegexHelper(
        StringBuilder sb,
        ParameterAnalysis param,
        string indent,
        string ownerToken)
    {
        var patternLiteral = SymbolDisplay.FormatLiteral(param.RegexPattern!, quote: true);
        var patternField = HelperName("__Pattern_", ownerToken, param.Name);
        sb.Append(indent).Append("private static readonly global::System.Text.RegularExpressions.Regex ").Append(patternField).AppendLine(" =");
        sb.Append(indent).Append("    new global::System.Text.RegularExpressions.Regex(").Append(patternLiteral).AppendLine(");");
        sb.AppendLine();
        sb.Append(indent).Append("private static string ").Append(HelperName("__ValidatePattern_", ownerToken, param.Name)).AppendLine("(string value)");
        sb.Append(indent).AppendLine("{");
        sb.Append(indent).Append("    if (!").Append(patternField).AppendLine(".IsMatch(value))");
        sb.Append(indent).AppendLine("    {");
        sb.Append(indent).Append("        throw new global::System.ArgumentException($\"'{value}' does not match pattern for '");
        sb.Append(param.Name).AppendLine("'.\", nameof(value));");
        sb.Append(indent).AppendLine("    }");
        sb.AppendLine();
        sb.Append(indent).AppendLine("    return value;");
        sb.Append(indent).AppendLine("}");
    }
}
