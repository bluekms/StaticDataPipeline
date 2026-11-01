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
            sb.Append(indent).Append("            return (").Append(enumFullName).Append(")global::Sdp.Csv.CsvMapperHelper.")
                .Append(CellParseMethod(underlyingKind)).AppendLine("(value, column);");
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
}
