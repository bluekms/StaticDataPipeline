using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Sdp.SourceGenerator.Generators;

internal static class KeyDuplicateCheckEmitter
{
    public static void Emit(StringBuilder sb, INamedTypeSymbol recordSymbol, string fileNameLiteral, string indent)
    {
        var keyParameter = KeyParameterResolver.Find(recordSymbol);
        if (keyParameter is null)
        {
            return;
        }

        // nullable [Key] 는 선언 검사가 막는 결함이다. HashSet<T> 에 null 을 넣는 생성 코드가 경고를 내지 않게 건너뛴다.
        if (TypeClassifier.IsNullable(keyParameter.Type))
        {
            return;
        }

        var keyTypeName = keyParameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var keyAccess = "__r." + EmitHelper.EscapeIdentifier(keyParameter.Name);
        var recordNameLiteral = SymbolDisplay.FormatLiteral(recordSymbol.Name, quote: true);
        var keyDisplay = KeyParameterResolver.BuildValueDisplay(keyParameter, "__r");

        sb.Append(indent).Append("var __keys = new global::System.Collections.Generic.HashSet<").Append(keyTypeName).AppendLine(">();");
        sb.Append(indent).AppendLine("foreach (var __r in records)");
        sb.Append(indent).AppendLine("{");
        sb.Append(indent).Append("    if (!__keys.Add(").Append(keyAccess).AppendLine("))");
        sb.Append(indent).AppendLine("    {");
        sb.Append(indent).AppendLine("        throw global::Sdp.Manager.TableSetLoaderHelper.DuplicateKeyError(");
        sb.Append(indent).Append("            ").Append(fileNameLiteral).Append(", ").Append(recordNameLiteral).AppendLine(",");
        sb.Append(indent).Append("            global::System.FormattableString.Invariant($\"").Append(keyDisplay).AppendLine("\"));");
        sb.Append(indent).AppendLine("    }");
        sb.Append(indent).AppendLine("}");
        sb.AppendLine();
    }
}
