using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Sdp.SourceGenerator.Generators;

// 문자열 → enum 변환 스위치 헬퍼 방출 (리플렉션 없는 Enum.Parse 대체)
internal static partial class CsvMapperEmitter
{
    private static void EmitEnumHelper(StringBuilder sb, ParameterAnalysis param, string indent, string ownerToken)
    {
        var enumType = (INamedTypeSymbol)TypeClassifier.UnwrapNullable(param.Type);
        EmitEnumSwitchHelper(sb, enumType, indent, HelperName("__MapColumn_", ownerToken, param.Name), param.IsKey);
    }

    private static void EmitEnumSwitchHelper(
        StringBuilder sb,
        INamedTypeSymbol enumType,
        string indent,
        string helperName,
        bool isKey)
    {
        var enumFullName = enumType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var underlyingKeyword = ScalarTypeKeyword(TypeClassifier.ClassifyScalar(enumType.EnumUnderlyingType!));
        var isUnsigned64 = enumType.EnumUnderlyingType!.SpecialType == SpecialType.System_UInt64;
        var accumulatorKeyword = isUnsigned64 ? "ulong" : "long";
        var accumulatorSuffix = isUnsigned64 ? "UL" : "L";
        var members = enumType
            .GetMembers()
            .OfType<IFieldSymbol>()
            .Where(f => f.IsConst)
            .ToList();

        sb.Append(indent).Append("private static ").Append(enumFullName).Append(' ')
            .Append(helperName).AppendLine("(string value)");
        sb.Append(indent).AppendLine("{");
        sb.Append(indent).AppendLine("    switch (value)");
        sb.Append(indent).AppendLine("    {");

        // 멤버명과 underlying 숫자값을 모두 받는다. 리플렉션(Enum.Parse) 없이 동작하도록 case 로 전개.
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
            // [Key] enum 은 정의되지 않은 숫자값/콤마 조합값도 허용한다(원본 Enum.Parse 동작 보존).
            // 멤버명·숫자 토큰을 ','로 분해해 underlying 값으로 OR 한 뒤 캐스팅한다.
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
            sb.Append(indent).Append("                        __acc |= (").Append(accumulatorKeyword).Append(")(")
                .Append(InvariantIntegerParse(underlyingKeyword, "__parts[__i].Trim()")).AppendLine(");");
            sb.Append(indent).AppendLine("                        break;");
            sb.Append(indent).AppendLine("                }");
            sb.Append(indent).AppendLine("            }");
            sb.AppendLine();
            sb.Append(indent).Append("            return (").Append(enumFullName).AppendLine(")__acc;");
            sb.Append(indent).AppendLine("        }");
        }
        else
        {
            sb.Append(indent).Append("            throw new global::System.ArgumentException($\"'{value}' is not a defined member of ")
                .Append(enumFullName).AppendLine(".\", nameof(value));");
        }

        sb.Append(indent).AppendLine("    }");
        sb.Append(indent).AppendLine("}");
    }

    private static string? EnumMemberNumericLiteral(object? constantValue)
    {
        // ulong 백킹 enum 의 long.MaxValue 초과 멤버는 Convert.ToInt64 가 오버플로하므로 분리해 포맷한다.
        return constantValue switch
        {
            null => null,
            ulong u => u.ToString(CultureInfo.InvariantCulture),
            _ => System.Convert.ToInt64(constantValue, CultureInfo.InvariantCulture)
                .ToString(CultureInfo.InvariantCulture),
        };
    }

    private static string EnumAccumulatorLiteral(object? constantValue, bool isUnsigned64)
    {
        // [Key] enum 폴백 누산기에 OR 할 멤버 리터럴. 부호 비트가 켜진 long 백킹 멤버(long.MinValue 등)는
        // 음수 십진 리터럴로 표기하면 컴파일되지 않으므로 2의 보수 비트 패턴을 unchecked 캐스트로 방출한다.
        if (isUnsigned64)
        {
            var bits = System.Convert.ToUInt64(constantValue, CultureInfo.InvariantCulture);
            return bits.ToString(CultureInfo.InvariantCulture) + "UL";
        }

        var value = System.Convert.ToInt64(constantValue, CultureInfo.InvariantCulture);
        if (value >= 0)
        {
            return value.ToString(CultureInfo.InvariantCulture) + "L";
        }

        var pattern = unchecked((ulong)value).ToString(CultureInfo.InvariantCulture);
        return "unchecked((long)" + pattern + "UL)";
    }
}
