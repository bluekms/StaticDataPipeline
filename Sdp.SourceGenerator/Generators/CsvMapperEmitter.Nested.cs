using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Sdp.SourceGenerator.Generators;

// 중첩 record 조립 헬퍼(__MapNested_)와 컬렉션 원소 record/enum 준비 방출
internal static partial class CsvMapperEmitter
{
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

        // single-param scalar record(type branding) — basePath 자체를 헤더로 직접 매핑.
        if (info.Parameters.Length == 1
            && info.Parameters[0].Nested is null
            && info.Parameters[0].Collection is null
            && info.Parameters[0].Kind != ScalarKind.Unsupported)
        {
            var only = info.Parameters[0];

            // basePath 헤더가 있으면 그것을(브랜딩), 없으면 basePath + "." + 컬럼명으로 폴백한다.
            // 깊은 중첩(예: Middle.Inner.Value)과 Id.Value 형태 하위호환을 모두 처리.
            var columnNameLiteral = SymbolDisplay.FormatLiteral(only.ColumnName, quote: true);
            sb.Append(indent).Append("    var __key = headers.Contains(basePath) ? basePath : basePath + \".\" + ")
                .Append(columnNameLiteral).AppendLine(";");
            sb.Append(indent).AppendLine("    var __raw = values[headers[__key]];");

            // enum 이면 __MapColumn_ 헬퍼 호출이 나간다. 그 헬퍼는 EmitHelpersRecursive 가 같은 토큰으로 방출한다.
            // NullString 분기는 EmitConversion 과 동일하게 __MapNullable_ 헬퍼를 경유한다.
            string conversion;
            if (only.IsNullable && only.NullString is not null)
            {
                conversion = HelperName("__MapNullable_", token, only.Name) + "(__raw)";
            }
            else
            {
                conversion = WrapValidations(only, EmitBaseConversion(only, "__raw", token), token);
            }

            sb.Append(indent).Append("    return new ").Append(typeFullName).Append('(').Append(conversion).AppendLine(");");
            sb.Append(indent).AppendLine("}");
            return;
        }

        sb.Append(indent).Append("    return new ").Append(typeFullName).AppendLine("(");

        for (var i = 0; i < info.Parameters.Length; i++)
        {
            var param = info.Parameters[i];
            sb.Append(indent).Append("        ");
            sb.Append(GeneratorEmitHelper.EscapeIdentifier(param.Name)).Append(": ");
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

    private static void EmitCollectionElementNestedHelpers(
        StringBuilder sb,
        ParameterAnalysis param,
        HashSet<INamedTypeSymbol> emittedHelperTypes,
        string indent,
        Dictionary<INamedTypeSymbol, string> nestedTokens)
    {
        if (param.Collection?.ElementNested is { } elementNested && emittedHelperTypes.Add(elementNested.Symbol))
        {
            EmitNestedHelper(sb, elementNested, indent, nestedTokens);
            sb.AppendLine();
            EmitHelpersRecursive(sb, elementNested.Parameters, emittedHelperTypes, indent, nestedTokens[elementNested.Symbol], nestedTokens);
            sb.AppendLine();
        }
    }

    // 컬렉션 원소가 enum 인 경우, 원소 타입별로 한 번만 enum 파싱 스위치 헬퍼를 방출한다.
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

        EmitEnumSwitchHelper(sb, enumType, indent, "__MapElementEnum_" + nestedTokens[enumType], isKey: false);
        sb.AppendLine();
    }
}
