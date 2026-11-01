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

            var fkSetVariables = EmitFkTargetSets(sb, recordFkInfo, indent + "        ");

            for (var parameterIndex = 0; parameterIndex < recordFkInfo.Parameters.Length; parameterIndex++)
            {
                sb.Append(indent).Append("        var __fkMissingReported").Append(parameterIndex).AppendLine(" = false;");
            }

            for (var parameterIndex = 0; parameterIndex < recordFkInfo.Parameters.Length; parameterIndex++)
            {
                EmitRuntimeParsedSwitchConditions(sb, recordFkInfo.Parameters[parameterIndex], parameterIndex, indent + "        ");
            }

            sb.AppendLine();
            sb.Append(indent).Append("        foreach (var __r in tableSet.").Append(memberAccess).AppendLine(".Records)");
            sb.Append(indent).AppendLine("        {");

            for (var parameterIndex = 0; parameterIndex < recordFkInfo.Parameters.Length; parameterIndex++)
            {
                EmitFkParameterCheck(sb, recordFkInfo.Parameters[parameterIndex], parameterIndex, recordFkInfo, fkSetVariables, indent + "            ");
            }

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

    private static void EmitRuntimeParsedSwitchConditions(
        StringBuilder sb,
        FkParameter fkParameter,
        int parameterIndex,
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

            sb.Append(indent).Append("var ").Append(SwitchConditionVariable(parameterIndex, branchIndex))
                .Append(" = ").Append(branch.ConditionLiteral!).AppendLine(";");
        }
    }

    private static string SwitchConditionVariable(int parameterIndex, int branchIndex)
    {
        return "__switchCondition" + parameterIndex.ToString(CultureInfo.InvariantCulture)
            + "_" + branchIndex.ToString(CultureInfo.InvariantCulture);
    }

    private sealed record FkTargetSetKey(string TableSetMember, string TargetColumn, string PropertyTypeName);

    private static Dictionary<FkTargetSetKey, string> EmitFkTargetSets(
        StringBuilder sb,
        RecordFkInfo recordFkInfo,
        string indent)
    {
        var setVariables = new Dictionary<FkTargetSetKey, string>();

        foreach (var fkParameter in recordFkInfo.Parameters)
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
                    .Append(EmitHelper.EscapeIdentifier(branch.TargetColumn)).AppendLine(");");
                sb.Append(indent).AppendLine("    }");
                sb.Append(indent).AppendLine("}");
                sb.AppendLine();
            }
        }

        return setVariables;
    }

    private static void EmitFkParameterCheck(
        StringBuilder sb,
        FkParameter fkParameter,
        int parameterIndex,
        RecordFkInfo recordFkInfo,
        Dictionary<FkTargetSetKey, string> fkSetVariables,
        string indent)
    {
        var propertyTypeFullyQualifiedName = fkParameter.ValueType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var keyParameter = FindKeyParameter(recordFkInfo.RecordSymbol);
        var keyClause = keyParameter is null
            ? string.Empty
            : "[" + keyParameter.Name + "=" + BuildKeyValueAccess(keyParameter) + "]";
        var recordDisplayExpr = keyClause.Length == 0
            ? SymbolDisplay.FormatLiteral(recordFkInfo.RecordSymbol.Name, quote: true)
            : "global::System.FormattableString.Invariant($\"" + recordFkInfo.RecordSymbol.Name + keyClause + "\")";
        var propertyNameLiteral = SymbolDisplay.FormatLiteral(fkParameter.PropertyName, quote: true);
        var sourceLiteral = SymbolDisplay.FormatLiteral(
            recordFkInfo.RecordSymbol.Name + "." + fkParameter.PropertyName, quote: true);
        var missingReportedVariable = "__fkMissingReported" + parameterIndex.ToString(CultureInfo.InvariantCulture);

        var isNullable = TypeClassifier.IsNullable(fkParameter.ValueType);

        sb.Append(indent).AppendLine("{");
        var body = indent;
        if (fkParameter.IsCollection)
        {
            sb.Append(indent).Append("    foreach (var __v in __r.")
                .Append(EmitHelper.EscapeIdentifier(fkParameter.PropertyName)).AppendLine(")");
            sb.Append(indent).AppendLine("    {");
            body = indent + "    ";
        }
        else
        {
            sb.Append(indent).Append("    var __v = __r.")
                .Append(EmitHelper.EscapeIdentifier(fkParameter.PropertyName)).AppendLine(";");
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
            sb.Append(lineIndent).Append("    ").Append(recordDisplayExpr).Append(", ").Append(propertyNameLiteral).AppendLine(",");
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
            AppendValueNotFoundError(body + "            ", "{__v}", fkTargets);
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

            var conditionDisplay = "{__r." + conditionColumnAccess + "}";
            var conditionColumnLiteral = SymbolDisplay.FormatLiteral(conditionColumn, quote: true);

            for (var branchIndex = 0; branchIndex < switchBranches.Count; branchIndex++)
            {
                var branch = switchBranches[branchIndex];
                var conditionOperand = branch.ConditionParsesAtRuntime
                    ? SwitchConditionVariable(parameterIndex, branchIndex)
                    : branch.ConditionLiteral!;
                var conditionExpression = "__r." + conditionColumnAccess + " == " + conditionOperand;

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
                sb.Append(errorIndent).Append("    ").Append(recordDisplayExpr).Append(", ").Append(propertyNameLiteral).AppendLine(",");
                sb.Append(errorIndent).Append("    global::System.FormattableString.Invariant($\"{__v}\"), ").Append(branchTargetsLiteral).AppendLine(",");
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

        if (fkParameter.IsCollection)
        {
            sb.Append(indent).AppendLine("    }");
        }

        sb.Append(indent).AppendLine("}");
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

    private static string BuildKeyValueAccess(IParameterSymbol keyParameter)
    {
        var access = "__r." + EmitHelper.EscapeIdentifier(keyParameter.Name);
        if (keyParameter.Type is INamedTypeSymbol { IsValueType: true, IsRecord: true } keyType)
        {
            var primaryConstructor = SymbolResolver.FindPrimaryConstructor(keyType);
            if (primaryConstructor is { Parameters.Length: 1 })
            {
                access += "." + EmitHelper.EscapeIdentifier(primaryConstructor.Parameters[0].Name);
            }
        }

        return "{" + access + "}";
    }
}
