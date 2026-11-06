using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Sdp.SourceGenerator.Generators;

internal static partial class TableSetLoaderEmitter
{
    private static void EmitValidateForeignKeys(
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

            var fkSetVariables = EmitFkTargetSets(sb, recordFkInfo.TableSetMember, fkParameters, indent + "        ");

            foreach (var fkParameter in fkParameters)
            {
                sb.Append(indent).Append("        var __fkMissingReported").Append(fkParameter.Index).AppendLine(" = false;");
            }

            foreach (var fkParameter in fkParameters)
            {
                EmitRuntimeParsedSwitchConditions(sb, fkParameter, indent + "        ");
            }

            sb.AppendLine();
            sb.Append(indent).AppendLine("        foreach (var __r in __records)");
            sb.Append(indent).AppendLine("        {");

            var rootContext = FkEmitContext.Root(recordFkInfo.RecordSymbol);
            EmitFkScope(sb, recordFkInfo.Scope, rootContext, fkSetVariables, indent + "            ");

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
        Dictionary<FkTargetSetKey, string> fkSetVariables,
        string indent)
    {
        foreach (var fkParameter in scope.Parameters)
        {
            EmitFkParameterCheck(sb, fkParameter, context, fkSetVariables, indent);
        }

        foreach (var child in scope.Children)
        {
            EmitFkChildScope(sb, child, context, fkSetVariables, indent);
        }
    }

    private static void EmitFkChildScope(
        StringBuilder sb,
        FkChildScope child,
        FkEmitContext context,
        Dictionary<FkTargetSetKey, string> fkSetVariables,
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
                childContext = childContext.WithElement(BuildKeyClause(child.RecordSymbol, instanceVariable));
                break;
            }
        }

        EmitFkScope(sb, child.Scope, childContext, fkSetVariables, indent + "    ");

        sb.Append(indent).AppendLine("}");
    }

    private static void EmitRuntimeParsedSwitchConditions(
        StringBuilder sb,
        FkParameter fkParameter,
        string indent)
    {
        var switchBranches = fkParameter.Branches
            .Where(static branch => branch.IsSwitch)
            .ToList();
        for (var branchIndex = 0; branchIndex < switchBranches.Count; branchIndex++)
        {
            var branch = switchBranches[branchIndex];
            if (!branch.ConditionParsesAtRuntime)
            {
                continue;
            }

            sb.Append(indent).Append("var ").Append(SwitchConditionVariable(fkParameter.Index, branchIndex))
                .Append(" = ").Append(branch.ConditionLiteral!).AppendLine(";");
        }
    }

    private static string SwitchConditionVariable(int parameterIndex, int branchIndex)
    {
        return "__switchCondition" + parameterIndex.ToString(CultureInfo.InvariantCulture)
            + "_" + branchIndex.ToString(CultureInfo.InvariantCulture);
    }

    private sealed record FkTargetSetKey(string TableSetMember, string TargetColumn, string PropertyTypeName);

    // DisplayBody 는 오류 메시지에 찍는 인스턴스 경로의 보간 문자열 본문이고(MonsterRecord[Id={__r.Id}].Drops[{__i1}]),
    // StaticPath 는 인스턴스와 무관한 선언 경로다(MonsterRecord.Drops).
    private sealed record FkEmitContext(
        string InstanceVariable,
        string DisplayBody,
        bool DisplayHasInterpolation,
        string StaticPath,
        int Depth)
    {
        public static FkEmitContext Root(INamedTypeSymbol recordSymbol)
        {
            var keyClause = BuildKeyClause(recordSymbol, "__r");

            return new FkEmitContext(
                "__r",
                recordSymbol.Name + keyClause,
                keyClause.Length > 0,
                recordSymbol.Name,
                Depth: 0);
        }

        public FkEmitContext Enter(string propertyName, string instanceVariable)
        {
            return new FkEmitContext(
                instanceVariable,
                DisplayBody + "." + propertyName,
                DisplayHasInterpolation,
                StaticPath + "." + propertyName,
                Depth + 1);
        }

        public FkEmitContext WithElement(string elementClause)
        {
            return this with
            {
                DisplayBody = DisplayBody + elementClause,
                DisplayHasInterpolation = DisplayHasInterpolation || elementClause.Length > 0,
            };
        }

        public string BuildDisplayExpression()
        {
            return DisplayHasInterpolation
                ? "global::System.FormattableString.Invariant($\"" + DisplayBody + "\")"
                : SymbolDisplay.FormatLiteral(DisplayBody, quote: true);
        }
    }

    private static Dictionary<FkTargetSetKey, string> EmitFkTargetSets(
        StringBuilder sb,
        string tableSetMember,
        List<FkParameter> fkParameters,
        string indent)
    {
        var setVariables = new Dictionary<FkTargetSetKey, string>();

        foreach (var fkParameter in fkParameters)
        {
            var propertyTypeFullyQualifiedName = fkParameter.ValueType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            foreach (var branch in fkParameter.Branches)
            {
                var setKey = new FkTargetSetKey(branch.TableSetMember, branch.TargetColumn, propertyTypeFullyQualifiedName);
                if (setVariables.ContainsKey(setKey))
                {
                    continue;
                }

                var setVariable = "__fkSet" + setVariables.Count.ToString(CultureInfo.InvariantCulture);
                setVariables[setKey] = setVariable;

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
                sb.Append(indent).Append("        ").Append(setVariable).Append(".Add(__t.")
                    .Append(BuildTargetValueAccess(branch)).AppendLine(");");
                sb.Append(indent).AppendLine("    }");
                sb.Append(indent).AppendLine("}");
                sb.AppendLine();
            }
        }

        return setVariables;
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
        sb.Append(indent).Append("    ").Append(setVariable).Append(".Add(__t.")
            .Append(BuildTargetValueAccess(branch)).AppendLine(");");
        sb.Append(indent).AppendLine("}");
        sb.AppendLine();
    }

    private static string BuildTargetValueAccess(FkBranch branch)
    {
        var access = EmitHelper.EscapeIdentifier(branch.TargetColumn);
        if (branch.TargetUnwrapMember is { } targetUnwrapMember)
        {
            access += "." + EmitHelper.EscapeIdentifier(targetUnwrapMember);
        }

        return access;
    }

    private static void EmitFkParameterCheck(
        StringBuilder sb,
        FkParameter fkParameter,
        FkEmitContext context,
        Dictionary<FkTargetSetKey, string> fkSetVariables,
        string indent)
    {
        var propertyTypeFullyQualifiedName = fkParameter.ValueType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var recordDisplayExpr = context.BuildDisplayExpression();
        var propertyNameLiteral = SymbolDisplay.FormatLiteral(fkParameter.PropertyName, quote: true);
        var elementNameExpression = fkParameter.Collection == FkCollectionKind.Array
            ? "global::System.FormattableString.Invariant($\"" + fkParameter.PropertyName + "[{__vi}]\")"
            : propertyNameLiteral;
        var valueDisplay = BuildValueDisplay(fkParameter);
        var sourceLiteral = SymbolDisplay.FormatLiteral(context.StaticPath + "." + fkParameter.PropertyName, quote: true);
        var missingReportedVariable = "__fkMissingReported" + fkParameter.Index.ToString(CultureInfo.InvariantCulture);
        var instanceAccess = context.InstanceVariable + ".";
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

        sb.Append(body).AppendLine("    var __ok = false;");

        var fkBranches = new List<FkBranch>();
        var switchBranches = new List<FkBranch>();
        foreach (var branch in fkParameter.Branches)
        {
            if (branch.IsSwitch)
            {
                switchBranches.Add(branch);
            }
            else
            {
                fkBranches.Add(branch);
            }
        }

        void AppendTargetNotLoadedError(string lineIndent, string targetMembers)
        {
            var targetLiteral = SymbolDisplay.FormatLiteral(targetMembers, quote: true);
            sb.Append(lineIndent).Append("if (!").Append(missingReportedVariable).AppendLine(")");
            sb.Append(lineIndent).AppendLine("{");
            sb.Append(lineIndent).Append("    ").Append(missingReportedVariable).AppendLine(" = true;");
            sb.Append(lineIndent).Append("    __errors.Add(global::Sdp.Manager.TableSetLoaderHelper.FkTargetNotLoadedError(")
                .Append(sourceLiteral).Append(", ").Append(targetLiteral).AppendLine("));");
            sb.Append(lineIndent).AppendLine("}");
        }

        void AppendValueNotFoundError(string lineIndent, string valueBody, string targets)
        {
            var targetsLiteral = SymbolDisplay.FormatLiteral(targets, quote: true);
            sb.Append(lineIndent).Append("__errors.Add(global::Sdp.Manager.TableSetLoaderHelper.FkValueNotFoundError(")
                .AppendLine();
            sb.Append(lineIndent).Append("    ").Append(recordDisplayExpr).Append(", ").Append(elementNameExpression).AppendLine(",");
            sb.Append(lineIndent).Append("    global::System.FormattableString.Invariant($\"").Append(valueBody).Append("\"), ")
                .Append(targetsLiteral).AppendLine("));");
        }

        if (fkBranches.Count > 0)
        {
            var parameterSetVariables = new List<string>();
            var targetMemberBySetVariable = new Dictionary<string, string>();
            foreach (var branch in fkBranches)
            {
                var setKey = new FkTargetSetKey(branch.TableSetMember, branch.TargetColumn, propertyTypeFullyQualifiedName);
                var setVariable = fkSetVariables[setKey];
                if (!parameterSetVariables.Contains(setVariable))
                {
                    parameterSetVariables.Add(setVariable);
                    targetMemberBySetVariable[setVariable] = branch.TableSetMember;
                }

                sb.Append(body).Append("    if (!__ok && ").Append(setVariable).Append(" is not null && ")
                    .Append(setVariable).AppendLine(".Contains(__v))");
                sb.Append(body).AppendLine("    {");
                sb.Append(body).AppendLine("        __ok = true;");
                sb.Append(body).AppendLine("    }");
            }

            var fkTargets = string.Join(", ", fkBranches.Select(static branch => branch.TableSetMember + "." + branch.TargetColumn).Distinct());
            var allTargetsLoadedCondition = string.Join(
                " && ", parameterSetVariables.Select(static setVariable => setVariable + " is not null"));

            sb.Append(body).AppendLine("    if (!__ok)");
            sb.Append(body).AppendLine("    {");
            sb.Append(body).Append("        if (").Append(allTargetsLoadedCondition).AppendLine(")");
            sb.Append(body).AppendLine("        {");
            AppendValueNotFoundError(body + "            ", valueDisplay, fkTargets);
            sb.Append(body).AppendLine("        }");

            sb.Append(body).AppendLine("        else");
            sb.Append(body).AppendLine("        {");
            foreach (var setVariable in parameterSetVariables)
            {
                sb.Append(body).Append("            if (").Append(setVariable).AppendLine(" is null)");
                sb.Append(body).AppendLine("            {");
                AppendTargetNotLoadedError(body + "                ", targetMemberBySetVariable[setVariable]);
                sb.Append(body).AppendLine("            }");
            }

            sb.Append(body).AppendLine("        }");

            sb.Append(body).AppendLine("    }");
        }

        if (switchBranches.Count > 0)
        {
            var conditionColumn = switchBranches[0].ConditionColumn!;
            var conditionColumnAccess = EmitHelper.EscapeIdentifier(conditionColumn);

            sb.Append(body).AppendLine("    var __matched = false;");
            sb.Append(body).AppendLine("    var __switchTargetLoaded = false;");

            var conditionAccess = instanceAccess + conditionColumnAccess;
            var conditionDisplay = BuildConditionDisplay(conditionAccess, switchBranches[0]);
            var conditionColumnLiteral = SymbolDisplay.FormatLiteral(conditionColumn, quote: true);

            for (var branchIndex = 0; branchIndex < switchBranches.Count; branchIndex++)
            {
                var branch = switchBranches[branchIndex];
                var conditionOperand = branch.ConditionParsesAtRuntime
                    ? SwitchConditionVariable(fkParameter.Index, branchIndex)
                    : branch.ConditionLiteral!;
                var conditionExpression = conditionAccess + " == " + conditionOperand;

                var setKey = new FkTargetSetKey(branch.TableSetMember, branch.TargetColumn, propertyTypeFullyQualifiedName);
                var setVariable = fkSetVariables[setKey];
                sb.Append(body).Append("    if (!__matched && ").Append(conditionExpression).AppendLine(")");
                sb.Append(body).AppendLine("    {");
                sb.Append(body).AppendLine("        __matched = true;");
                sb.Append(body).Append("        if (").Append(setVariable).AppendLine(" is not null)");
                sb.Append(body).AppendLine("        {");
                sb.Append(body).AppendLine("            __switchTargetLoaded = true;");
                sb.Append(body).Append("            if (").Append(setVariable).AppendLine(".Contains(__v))");
                sb.Append(body).AppendLine("            {");
                sb.Append(body).AppendLine("                __ok = true;");
                sb.Append(body).AppendLine("            }");
                sb.Append(body).AppendLine("            else");
                sb.Append(body).AppendLine("            {");

                var branchTargetsLiteral = SymbolDisplay.FormatLiteral(branch.TableSetMember + "." + branch.TargetColumn, quote: true);
                var errorIndent = body + "                ";
                sb.Append(errorIndent).AppendLine("__errors.Add(global::Sdp.Manager.TableSetLoaderHelper.SwitchFkValueNotFoundError(");
                sb.Append(errorIndent).Append("    ").Append(recordDisplayExpr).Append(", ").Append(elementNameExpression).AppendLine(",");
                sb.Append(errorIndent).Append("    global::System.FormattableString.Invariant($\"").Append(valueDisplay).Append("\"), ")
                    .Append(branchTargetsLiteral).AppendLine(",");
                sb.Append(errorIndent).Append("    ").Append(conditionColumnLiteral)
                    .Append(", global::System.FormattableString.Invariant($\"").Append(conditionDisplay).AppendLine("\")));");
                sb.Append(body).AppendLine("            }");
                sb.Append(body).AppendLine("        }");
                sb.Append(body).AppendLine("    }");
            }

            var switchTargetMembers = string.Join(", ", switchBranches.Select(static branch => branch.TableSetMember).Distinct());

            sb.Append(body).AppendLine("    if (!__matched)");
            sb.Append(body).AppendLine("    {");
            sb.Append(body).AppendLine("        __errors.Add(global::Sdp.Manager.TableSetLoaderHelper.SwitchFkConditionNotMatchedError(");
            sb.Append(body).Append("            ").Append(recordDisplayExpr).Append(", ").Append(propertyNameLiteral).AppendLine(",");
            sb.Append(body).Append("            ").Append(conditionColumnLiteral)
                .Append(", global::System.FormattableString.Invariant($\"").Append(conditionDisplay).AppendLine("\")));");
            sb.Append(body).AppendLine("    }");
            sb.Append(body).AppendLine("    else if (!__ok && !__switchTargetLoaded)");
            sb.Append(body).AppendLine("    {");
            AppendTargetNotLoadedError(body + "        ", switchTargetMembers);
            sb.Append(body).AppendLine("    }");
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

    // bool 은 ToString() 이 "True" 라서 [SwitchForeignKey] 에 적는 표기(true, false)로 직접 찍는다.
    private static string BuildConditionDisplay(string conditionAccess, FkBranch branch)
    {
        switch (branch.ConditionDisplayKind)
        {
            case FkConditionDisplayKind.Boolean:
                return "{(" + conditionAccess + " ? \"true\" : \"false\")}";
            case FkConditionDisplayKind.NullableBoolean:
                return "{(" + conditionAccess + " == true ? \"true\" : "
                    + conditionAccess + " == false ? \"false\" : \"\")}";
        }

        if (branch.ConditionDisplayFormat is null)
        {
            return "{" + conditionAccess + "}";
        }

        return "{" + conditionAccess + ":" + EscapeInterpolationFormat(branch.ConditionDisplayFormat) + "}";
    }

    // 브랜딩 record 는 ToString() 이 "ItemId { Value = 3 }" 이라 안쪽 값만 찍는다.
    private static string BuildValueDisplay(FkParameter fkParameter)
    {
        if (fkParameter.ValueDisplayMember is not { } valueDisplayMember)
        {
            return "{__v}";
        }

        return "{__v." + EmitHelper.EscapeIdentifier(valueDisplayMember) + "}";
    }

    private static string EscapeInterpolationFormat(string format)
    {
        return format
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("{", "{{")
            .Replace("}", "}}");
    }

    private static IParameterSymbol? FindKeyParameter(INamedTypeSymbol record)
    {
        var primaryConstructor = SymbolResolver.FindPrimaryConstructor(record);
        if (primaryConstructor is null)
        {
            return null;
        }

        foreach (var param in primaryConstructor.Parameters)
        {
            foreach (var attr in param.GetAttributes())
            {
                if (attr.AttributeClass?.Name == "KeyAttribute"
                    && attr.AttributeClass.ContainingNamespace?.ToDisplayString() == "Sdp.Attributes")
                {
                    return param;
                }
            }
        }

        return null;
    }

    private static string BuildKeyClause(INamedTypeSymbol record, string instanceVariable)
    {
        var keyParameter = FindKeyParameter(record);
        if (keyParameter is null)
        {
            return string.Empty;
        }

        return "[" + keyParameter.Name + "=" + BuildKeyValueAccess(keyParameter, instanceVariable) + "]";
    }

    private static string BuildKeyValueAccess(IParameterSymbol keyParameter, string instanceVariable)
    {
        var access = instanceVariable + "." + EmitHelper.EscapeIdentifier(keyParameter.Name);
        var valueDisplayMember = FkTargetResolver.ResolveValueDisplayMember(keyParameter.Type);
        if (valueDisplayMember is not null)
        {
            access += "." + EmitHelper.EscapeIdentifier(valueDisplayMember);
        }

        return "{" + access + "}";
    }
}
