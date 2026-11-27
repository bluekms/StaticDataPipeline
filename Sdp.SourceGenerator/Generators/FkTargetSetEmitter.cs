using System.Text;
using Microsoft.CodeAnalysis;

namespace Sdp.SourceGenerator.Generators;

internal static class FkTargetSetEmitter
{
    public static FkTargetSets Emit(
        StringBuilder sb,
        string tableSetMember,
        List<FkParameter> fkParameters,
        string indent)
    {
        var targetSets = new FkTargetSets();

        foreach (var fkParameter in fkParameters)
        {
            var propertyTypeFullyQualifiedName = fkParameter.ValueType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            foreach (var branch in fkParameter.Branches)
            {
                if (targetSets.Contains(branch, propertyTypeFullyQualifiedName))
                {
                    continue;
                }

                var setVariable = targetSets.Add(branch, propertyTypeFullyQualifiedName);

                if (branch.TableSetMember == tableSetMember)
                {
                    EmitSelfReferenceFkTargetSet(sb, branch, propertyTypeFullyQualifiedName, setVariable, indent);
                    continue;
                }

                var targetMemberAccess = EmitHelper.EscapeIdentifier(branch.TableSetMember);
                sb.Append(indent).Append("global::System.Collections.Generic.HashSet<").Append(propertyTypeFullyQualifiedName)
                    .Append(">? ").Append(setVariable).AppendLine(" = null;");
                sb.Append(indent).Append("if (tableSet.").Append(targetMemberAccess).AppendLine(" is not null)");
                sb.Append(indent).AppendLine("{");
                sb.Append(indent).Append("    ").Append(setVariable)
                    .Append(" = new global::System.Collections.Generic.HashSet<").Append(propertyTypeFullyQualifiedName).AppendLine(">();");
                sb.Append(indent).Append("    foreach (var __t in tableSet.").Append(targetMemberAccess).AppendLine(".Records)");
                sb.Append(indent).AppendLine("    {");
                sb.Append(indent).Append("        ").Append(setVariable).Append(".Add(")
                    .Append(BuildTargetValueAccess(branch)).AppendLine(");");
                sb.Append(indent).AppendLine("    }");
                sb.Append(indent).AppendLine("}");
                sb.AppendLine();
            }
        }

        return targetSets;
    }

    private static void EmitSelfReferenceFkTargetSet(
        StringBuilder sb,
        FkBranch branch,
        string propertyTypeFullyQualifiedName,
        string setVariable,
        string indent)
    {
        sb.Append(indent).Append("var ").Append(setVariable)
            .Append(" = new global::System.Collections.Generic.HashSet<").Append(propertyTypeFullyQualifiedName).AppendLine(">();");
        sb.Append(indent).AppendLine("foreach (var __t in __records)");
        sb.Append(indent).AppendLine("{");
        sb.Append(indent).Append("    ").Append(setVariable).Append(".Add(")
            .Append(BuildTargetValueAccess(branch)).AppendLine(");");
        sb.Append(indent).AppendLine("}");
        sb.AppendLine();
    }

    private static string BuildTargetValueAccess(FkBranch branch)
    {
        var access = "__t." + EmitHelper.EscapeIdentifier(branch.TargetColumn);
        if (!branch.IsTargetNullable)
        {
            if (branch.TargetUnwrapMember is { } unwrapMember)
            {
                access += "." + EmitHelper.EscapeIdentifier(unwrapMember);
            }

            return access;
        }

        // 대상 열이 nullable 이고 값이 참조 타입이면 HashSet<T>.Add 가 CS8604 경고를 낸다.
        // null 이 집합에 들어가도 FK 값이 null 인 행은 검사를 건너뛰므로 결과는 같다.
        if (branch.TargetUnwrapMember is { } nullableUnwrapMember)
        {
            return "(" + access + "?." + EmitHelper.EscapeIdentifier(nullableUnwrapMember) + ")!";
        }

        return access + "!";
    }
}
