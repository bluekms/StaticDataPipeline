using System.Globalization;
using System.Text;

namespace Sdp.SourceGenerator.Generators;

internal static class ForeignKeyValidationEmitter
{
    public static void Emit(
        StringBuilder sb,
        StaticDataManagerAnalysis analysis,
        string tableSetFullyQualifiedName,
        string indent)
    {
        sb.Append(indent).Append("internal static void ValidateForeignKeys(")
            .Append(tableSetFullyQualifiedName).AppendLine(" tableSet)");
        sb.Append(indent).AppendLine("{");
        sb.Append(indent).AppendLine("    var __errors = new global::System.Collections.Generic.List<global::System.Exception>();");

        foreach (var recordFkInfo in analysis.RecordFkInfos)
        {
            var memberAccess = EmitHelper.EscapeIdentifier(recordFkInfo.TableSetMember);
            sb.AppendLine();
            sb.Append(indent).Append("    if (tableSet.").Append(memberAccess).AppendLine(" is not null)");
            sb.Append(indent).AppendLine("    {");
            sb.Append(indent).Append("        var __records = tableSet.").Append(memberAccess).AppendLine(".Records;");

            var fkParameters = new List<FkParameter>();
            CollectFkParameters(recordFkInfo.Scope, fkParameters);

            var targetSets = FkTargetSetEmitter.Emit(sb, recordFkInfo.TableSetMember, fkParameters, indent + "        ");

            foreach (var fkParameter in fkParameters)
            {
                // 읽지 않는 지역 변수는 생성 코드에 CS0219 를 내므로, 쓰는 분기가 있을 때만 선언한다.
                if (fkParameter.ForeignKey is not null)
                {
                    sb.Append(indent).Append("        var __fkMissingReported").Append(fkParameter.Index).AppendLine(" = false;");
                }

                foreach (var switchTargetMember in FkSwitchCondition.TargetMembers(fkParameter))
                {
                    var switchMissingReported = FkSwitchCondition.MissingReportedVariable(fkParameter, switchTargetMember);
                    sb.Append(indent).Append("        var ").Append(switchMissingReported).AppendLine(" = false;");
                }
            }

            foreach (var fkParameter in fkParameters)
            {
                FkSwitchCondition.EmitRuntimeParsedConditions(sb, fkParameter, indent + "        ");
            }

            sb.AppendLine();
            sb.Append(indent).AppendLine("        foreach (var __r in __records)");
            sb.Append(indent).AppendLine("        {");

            var rootContext = FkEmitContext.Root(recordFkInfo.RecordSymbol);
            EmitFkScope(sb, recordFkInfo.Scope, rootContext, targetSets, indent + "            ");

            sb.Append(indent).AppendLine("        }");
            sb.Append(indent).AppendLine("    }");
        }

        sb.AppendLine();
        sb.Append(indent).AppendLine("    if (__errors.Count > 0)");
        sb.Append(indent).AppendLine("    {");
        sb.Append(indent).AppendLine("        throw new global::System.AggregateException(global::Sdp.Manager.TableSetLoaderHelper.ForeignKeyValidationFailedMessage, __errors);");
        sb.Append(indent).AppendLine("    }");
        sb.Append(indent).AppendLine("}");
    }

    private static void CollectFkParameters(FkScope scope, List<FkParameter> fkParameters)
    {
        fkParameters.AddRange(scope.Parameters);

        foreach (var child in scope.Children)
        {
            CollectFkParameters(child.Scope, fkParameters);
        }
    }

    private static void EmitFkScope(
        StringBuilder sb,
        FkScope scope,
        FkEmitContext context,
        FkTargetSets targetSets,
        string indent)
    {
        foreach (var fkParameter in scope.Parameters)
        {
            var parameterCheck = new FkParameterCheckEmitter(sb, fkParameter, context, targetSets);
            parameterCheck.Emit(indent);
        }

        foreach (var child in scope.Children)
        {
            EmitFkChildScope(sb, child, context, targetSets, indent);
        }
    }

    private static void EmitFkChildScope(
        StringBuilder sb,
        FkChildScope child,
        FkEmitContext context,
        FkTargetSets targetSets,
        string indent)
    {
        var depth = (context.Depth + 1).ToString(CultureInfo.InvariantCulture);
        var instanceVariable = "__s" + depth;
        var memberAccess = context.InstanceVariable + "." + EmitHelper.EscapeIdentifier(child.PropertyName);
        var childContext = context.Enter(child.PropertyName, instanceVariable);

        switch (child.Kind)
        {
            case FkChildKind.Record:
            {
                sb.Append(indent).AppendLine("{");
                sb.Append(indent).Append("    var ").Append(instanceVariable).Append(" = ").Append(memberAccess).AppendLine(";");
                break;
            }

            case FkChildKind.ArrayElement:
            {
                var indexVariable = "__i" + depth;
                sb.Append(indent).Append("for (var ").Append(indexVariable).Append(" = 0; ")
                    .Append(indexVariable).Append(" < ").Append(memberAccess).Append(".Length; ")
                    .Append(indexVariable).AppendLine("++)");
                sb.Append(indent).AppendLine("{");
                sb.Append(indent).Append("    var ").Append(instanceVariable).Append(" = ")
                    .Append(memberAccess).Append("[").Append(indexVariable).AppendLine("];");
                childContext = childContext.WithElement("[{" + indexVariable + "}]");
                break;
            }

            case FkChildKind.SetElement:
            {
                sb.Append(indent).Append("foreach (var ").Append(instanceVariable).Append(" in ")
                    .Append(memberAccess).AppendLine(")");
                sb.Append(indent).AppendLine("{");
                break;
            }

            case FkChildKind.MapValue:
            {
                sb.Append(indent).Append("foreach (var ").Append(instanceVariable).Append(" in ")
                    .Append(memberAccess).AppendLine(".Values)");
                sb.Append(indent).AppendLine("{");
                childContext = childContext.WithElement(FkEmitContext.BuildKeyClause(child.RecordSymbol, instanceVariable));
                break;
            }
        }

        EmitFkScope(sb, child.Scope, childContext, targetSets, indent + "    ");

        sb.Append(indent).AppendLine("}");
    }
}
