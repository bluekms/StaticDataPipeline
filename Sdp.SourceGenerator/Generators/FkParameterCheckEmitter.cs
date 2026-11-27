using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Sdp.SourceGenerator.Generators;

internal sealed class FkParameterCheckEmitter(
    StringBuilder sb,
    FkParameter fkParameter,
    FkEmitContext context,
    FkTargetSets targetSets)
{
    private readonly string valueTypeName = fkParameter.ValueType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    private readonly string recordDisplayExpression = context.BuildDisplayExpression();
    private readonly string propertyNameLiteral = SymbolDisplay.FormatLiteral(fkParameter.PropertyName, quote: true);
    private readonly string elementNameExpression = BuildElementNameExpression(fkParameter);
    private readonly string valueDisplay = BuildValueDisplay(fkParameter);
    private readonly string sourceLiteral = SymbolDisplay.FormatLiteral(context.StaticPath + "." + fkParameter.PropertyName, quote: true);
    private readonly string missingReportedVariable = "__fkMissingReported" + fkParameter.Index.ToString(CultureInfo.InvariantCulture);
    private readonly string instanceAccess = context.InstanceVariable + ".";

    public void Emit(string indent)
    {
        var memberAccess = instanceAccess + EmitHelper.EscapeIdentifier(fkParameter.PropertyName);
        var isNullable = TypeClassifier.IsNullable(fkParameter.ValueType);

        sb.Append(indent).AppendLine("{");
        var body = indent;
        switch (fkParameter.Collection)
        {
            case FkCollectionKind.Array:
            {
                sb.Append(indent).Append("    for (var __vi = 0; __vi < ").Append(memberAccess)
                    .AppendLine(".Length; __vi++)");
                sb.Append(indent).AppendLine("    {");
                sb.Append(indent).Append("        var __v = ").Append(memberAccess).AppendLine("[__vi];");
                body = indent + "    ";
                break;
            }

            case FkCollectionKind.Set:
            {
                sb.Append(indent).Append("    foreach (var __v in ").Append(memberAccess).AppendLine(")");
                sb.Append(indent).AppendLine("    {");
                body = indent + "    ";
                break;
            }

            default:
            {
                sb.Append(indent).Append("    var __v = ").Append(memberAccess).AppendLine(";");
                break;
            }
        }

        var nullGuardIndent = body;
        if (isNullable)
        {
            sb.Append(nullGuardIndent).AppendLine("    if (__v is not null)");
            sb.Append(nullGuardIndent).AppendLine("    {");
            body = nullGuardIndent + "    ";
        }

        // [ForeignKey] 는 파라미터에 하나만 붙는다. 둘 이상이면 컴파일러가 CS0579 로 빌드를 멈춘다.
        var foreignKeyBranch = fkParameter.Branches.FirstOrDefault(static branch => !branch.IsSwitch);
        if (foreignKeyBranch is not null)
        {
            EmitForeignKeyBranch(foreignKeyBranch, body);
        }

        var switchBranches = FkSwitchCondition.SelectSwitchBranches(fkParameter);
        if (switchBranches.Count > 0)
        {
            EmitSwitchBranches(switchBranches, body);
        }

        if (isNullable)
        {
            sb.Append(nullGuardIndent).AppendLine("    }");
        }

        if (fkParameter.Collection != FkCollectionKind.None)
        {
            sb.Append(indent).AppendLine("    }");
        }

        sb.Append(indent).AppendLine("}");
    }

    private static string BuildElementNameExpression(FkParameter fkParameter)
    {
        return fkParameter.Collection == FkCollectionKind.Array
            ? "global::System.FormattableString.Invariant($\"" + fkParameter.PropertyName + "[{__vi}]\")"
            : SymbolDisplay.FormatLiteral(fkParameter.PropertyName, quote: true);
    }

    // 브랜딩 record 는 ToString() 이 "ItemId { Value = 3 }" 이라 안쪽 값만 찍는다.
    private static string BuildValueDisplay(FkParameter fkParameter)
    {
        if (fkParameter.ValueDisplayMember is not { } valueDisplayMember)
        {
            return "{__v}";
        }

        var isNullableStructBranding = TypeClassifier.IsNullableValueType(fkParameter.ValueType);
        var brandingAccess = isNullableStructBranding ? "__v." + nameof(Nullable<int>.Value) : "__v";

        return "{" + brandingAccess + "." + EmitHelper.EscapeIdentifier(valueDisplayMember) + "}";
    }

    private void EmitForeignKeyBranch(FkBranch branch, string body)
    {
        var setVariable = targetSets.Find(branch, valueTypeName);

        sb.Append(body).Append("    if (").Append(setVariable).AppendLine(" is null)");
        sb.Append(body).AppendLine("    {");
        AppendTargetNotLoadedError(body + "        ", missingReportedVariable, branch.TableSetMember);
        sb.Append(body).AppendLine("    }");
        sb.Append(body).Append("    else if (!").Append(setVariable).AppendLine(".Contains(__v))");
        sb.Append(body).AppendLine("    {");
        AppendValueNotFoundError(body + "        ", branch.TableSetMember + "." + branch.TargetColumn);
        sb.Append(body).AppendLine("    }");
    }

    private void EmitSwitchBranches(List<FkBranch> switchBranches, string body)
    {
        var conditionColumn = switchBranches[0].ConditionColumn!;
        var conditionColumnAccess = EmitHelper.EscapeIdentifier(conditionColumn);

        sb.Append(body).AppendLine("    var __matched = false;");

        var conditionAccess = instanceAccess + conditionColumnAccess;
        var conditionDisplay = FkSwitchCondition.BuildDisplay(conditionAccess, switchBranches[0]);
        var conditionColumnLiteral = SymbolDisplay.FormatLiteral(conditionColumn, quote: true);

        for (var branchIndex = 0; branchIndex < switchBranches.Count; branchIndex++)
        {
            var branch = switchBranches[branchIndex];
            var conditionOperand = FkSwitchCondition.BuildOperand(fkParameter, branch, branchIndex);
            var conditionExpression = conditionAccess + " == " + conditionOperand;

            var setVariable = targetSets.Find(branch, valueTypeName);
            sb.Append(body).Append("    if (!__matched && ").Append(conditionExpression).AppendLine(")");
            sb.Append(body).AppendLine("    {");
            sb.Append(body).AppendLine("        __matched = true;");
            sb.Append(body).Append("        if (").Append(setVariable).AppendLine(" is not null)");
            sb.Append(body).AppendLine("        {");
            sb.Append(body).Append("            if (!").Append(setVariable).AppendLine(".Contains(__v))");
            sb.Append(body).AppendLine("            {");
            AppendSwitchValueNotFoundError(body + "                ", branch, conditionColumnLiteral, conditionDisplay);
            sb.Append(body).AppendLine("            }");
            sb.Append(body).AppendLine("        }");

            var switchMissingReported = FkSwitchCondition.MissingReportedVariable(fkParameter, branch.TableSetMember);
            sb.Append(body).AppendLine("        else");
            sb.Append(body).AppendLine("        {");
            AppendTargetNotLoadedError(body + "            ", switchMissingReported, branch.TableSetMember);
            sb.Append(body).AppendLine("        }");
            sb.Append(body).AppendLine("    }");
        }

        sb.Append(body).AppendLine("    if (!__matched)");
        sb.Append(body).AppendLine("    {");
        sb.Append(body).AppendLine("        __errors.Add(global::Sdp.Manager.TableSetLoaderHelper.SwitchFkConditionNotMatchedError(");
        sb.Append(body).Append("            ").Append(recordDisplayExpression).Append(", ").Append(propertyNameLiteral).AppendLine(",");
        sb.Append(body).Append("            ").Append(conditionColumnLiteral)
            .Append(", global::System.FormattableString.Invariant($\"").Append(conditionDisplay).AppendLine("\")));");
        sb.Append(body).AppendLine("    }");
    }

    private void AppendTargetNotLoadedError(string lineIndent, string reportedVariable, string targetMember)
    {
        var targetLiteral = SymbolDisplay.FormatLiteral(targetMember, quote: true);
        sb.Append(lineIndent).Append("if (!").Append(reportedVariable).AppendLine(")");
        sb.Append(lineIndent).AppendLine("{");
        sb.Append(lineIndent).Append("    ").Append(reportedVariable).AppendLine(" = true;");
        sb.Append(lineIndent).Append("    __errors.Add(global::Sdp.Manager.TableSetLoaderHelper.FkTargetNotLoadedError(")
            .Append(sourceLiteral).Append(", ").Append(targetLiteral).AppendLine("));");
        sb.Append(lineIndent).AppendLine("}");
    }

    private void AppendValueNotFoundError(string lineIndent, string targets)
    {
        var targetsLiteral = SymbolDisplay.FormatLiteral(targets, quote: true);
        sb.Append(lineIndent).Append("__errors.Add(global::Sdp.Manager.TableSetLoaderHelper.FkValueNotFoundError(")
            .AppendLine();
        sb.Append(lineIndent).Append("    ").Append(recordDisplayExpression).Append(", ").Append(elementNameExpression).AppendLine(",");
        sb.Append(lineIndent).Append("    global::System.FormattableString.Invariant($\"").Append(valueDisplay).Append("\"), ")
            .Append(targetsLiteral).AppendLine("));");
    }

    private void AppendSwitchValueNotFoundError(
        string lineIndent,
        FkBranch branch,
        string conditionColumnLiteral,
        string conditionDisplay)
    {
        var branchTargetsLiteral = SymbolDisplay.FormatLiteral(branch.TableSetMember + "." + branch.TargetColumn, quote: true);
        sb.Append(lineIndent).AppendLine("__errors.Add(global::Sdp.Manager.TableSetLoaderHelper.SwitchFkValueNotFoundError(");
        sb.Append(lineIndent).Append("    ").Append(recordDisplayExpression).Append(", ").Append(elementNameExpression).AppendLine(",");
        sb.Append(lineIndent).Append("    global::System.FormattableString.Invariant($\"").Append(valueDisplay).Append("\"), ")
            .Append(branchTargetsLiteral).AppendLine(",");
        sb.Append(lineIndent).Append("    ").Append(conditionColumnLiteral)
            .Append(", global::System.FormattableString.Invariant($\"").Append(conditionDisplay).AppendLine("\")));");
    }
}
