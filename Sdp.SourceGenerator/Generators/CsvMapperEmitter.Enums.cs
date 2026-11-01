using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Sdp.SourceGenerator.Generators;

internal static partial class CsvMapperEmitter
{
    // Enum.Parse(TryParse) 는 AOT 빌드 시점에 리플렉션이 제거되어 동작하지 않으므로 switch 문으로 전개
    private static void EmitEnumHelper(
        StringBuilder sb,
        ParameterAnalysis param,
        string indent,
        string ownerToken)
    {
        var enumType = (INamedTypeSymbol)TypeClassifier.UnwrapNullable(param.Type);
        EmitEnumSwitchHelper(
            sb,
            enumType,
            indent,
            HelperName("__MapColumn_", ownerToken, param.Name),
            param.IsKey);
    }

    private static void EmitEnumSwitchHelper(
        StringBuilder sb,
        INamedTypeSymbol enumType,
        string indent,
        string helperName,
        bool isKey)
    {
        var enumFullName = enumType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var underlyingKind = TypeClassifier.ClassifyScalar(enumType.EnumUnderlyingType!);
        var isUnsigned64 = enumType.EnumUnderlyingType!.SpecialType == SpecialType.System_UInt64;
        var accumulatorKeyword = isUnsigned64 ? "ulong" : "long";
        var accumulatorSuffix = isUnsigned64 ? "UL" : "L";
        var members = enumType
            .GetMembers()
            .OfType<IFieldSymbol>()
            .Where(static f => f.IsConst)
            .ToList();

        sb.Append(indent).Append("private static ").Append(enumFullName).Append(' ')
            .Append(helperName).AppendLine("(string value, string column)");
        sb.Append(indent).AppendLine("{");
        sb.Append(indent).AppendLine("    switch (value)");
        sb.Append(indent).AppendLine("    {");

        var seenNumeric = new HashSet<string>();
        foreach (var member in members)
        {
            sb.Append(indent).Append("        case \"").Append(member.Name).AppendLine("\":");
            var numeric = EnumMemberNumericLiteral(member.ConstantValue);
            if (numeric is not null && seenNumeric.Add(numeric))
            {
                sb.Append(indent).Append("        case \"").Append(numeric).AppendLine("\":");
            }

            sb.Append(indent).Append("            return ").Append(enumFullName).Append('.').Append(member.Name).AppendLine(";");
        }

        sb.Append(indent).AppendLine("        default:");
        if (isKey)
        {
            sb.Append(indent).AppendLine("        {");
            sb.Append(indent).Append("            var __acc = 0").Append(accumulatorSuffix).AppendLine(";");
            sb.Append(indent).AppendLine("            var __parts = value.Split(',');");
            sb.Append(indent).AppendLine("            for (var __i = 0; __i < __parts.Length; __i++)");
            sb.Append(indent).AppendLine("            {");
            sb.Append(indent).AppendLine("                switch (__parts[__i].Trim())");
            sb.Append(indent).AppendLine("                {");
            foreach (var member in members)
            {
                var accumulatorLiteral = EnumAccumulatorLiteral(member.ConstantValue, isUnsigned64);
                sb.Append(indent).Append("                    case \"").Append(member.Name).AppendLine("\":");
                sb.Append(indent).Append("                        __acc |= ").Append(accumulatorLiteral).AppendLine(";");
                sb.Append(indent).AppendLine("                        break;");
            }

            sb.Append(indent).AppendLine("                    default:");
            sb.Append(indent).Append("                        __acc |= (").Append(accumulatorKeyword).Append(")(global::Sdp.Csv.CsvMapperHelper.")
                .Append(CellParseMethod(underlyingKind)).AppendLine("(__parts[__i].Trim(), column));");
            sb.Append(indent).AppendLine("                        break;");
            sb.Append(indent).AppendLine("                }");
            sb.Append(indent).AppendLine("            }");
            sb.AppendLine();
            sb.Append(indent).Append("            return (").Append(enumFullName).AppendLine(")__acc;");
            sb.Append(indent).AppendLine("        }");
        }
        else
        {
            sb.Append(indent).Append("            throw global::Sdp.Csv.CsvMapperHelper.UndefinedEnumMember(value, ")
                .Append(SymbolDisplay.FormatLiteral(enumType.ToDisplayString(), quote: true)).AppendLine(", column);");
        }

        sb.Append(indent).AppendLine("    }");
        sb.Append(indent).AppendLine("}");
    }

    private static string? EnumMemberNumericLiteral(object? constantValue)
    {
        // 컴파일 에러
        if (constantValue is null)
        {
            return null;
        }

        if (constantValue is ulong unsignedValue)
        {
            return unsignedValue.ToString(CultureInfo.InvariantCulture);
        }

        var value = Convert.ToInt64(constantValue, CultureInfo.InvariantCulture);

        return value.ToString(CultureInfo.InvariantCulture);
    }

    private static string EnumAccumulatorLiteral(object? constantValue, bool isUnsigned64)
    {
        if (isUnsigned64)
        {
            var bits = Convert.ToUInt64(constantValue, CultureInfo.InvariantCulture);

            return bits.ToString(CultureInfo.InvariantCulture) + "UL";
        }

        var value = Convert.ToInt64(constantValue, CultureInfo.InvariantCulture);
        if (value >= 0)
        {
            return value.ToString(CultureInfo.InvariantCulture) + "L";
        }

        var pattern = unchecked((ulong)value).ToString(CultureInfo.InvariantCulture);

        return "unchecked((long)" + pattern + "UL)";
    }
}
