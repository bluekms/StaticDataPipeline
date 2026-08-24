using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Sdp.SourceGenerator.Generators;

internal static partial class TableSetEmitter
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

            sb.AppendLine();
            for (var parameterIndex = 0; parameterIndex < recordFkInfo.Parameters.Length; parameterIndex++)
            {
                EmitFkTargetsNotLoadedGuard(
                    sb, recordFkInfo.Parameters[parameterIndex], parameterIndex, recordFkInfo, fkSetVariables, indent + "        ");
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

    private sealed record FkTargetSetKey(string TableSetMember, string TargetColumn, string PropertyTypeName);

    private static Dictionary<FkTargetSetKey, string> EmitFkTargetSets(
        StringBuilder sb,
        RecordFkInfo recordFkInfo,
        string indent)
    {
        var setVariables = new Dictionary<FkTargetSetKey, string>();

        foreach (var fkParameter in recordFkInfo.Parameters)
        {
            var propertyTypeFullyQualifiedName = fkParameter.PropertyType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
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

    private static void EmitFkTargetsNotLoadedGuard(
        StringBuilder sb,
        FkParameter fkParameter,
        int parameterIndex,
        RecordFkInfo recordFkInfo,
        Dictionary<FkTargetSetKey, string> fkSetVariables,
        string indent)
    {
        var propertyTypeFullyQualifiedName = fkParameter.PropertyType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var setVariables = new List<string>();
        foreach (var branch in fkParameter.Branches)
        {
            var setKey = new FkTargetSetKey(branch.TableSetMember, branch.TargetColumn, propertyTypeFullyQualifiedName);
            var setVariable = fkSetVariables[setKey];
            if (!setVariables.Contains(setVariable))
            {
                setVariables.Add(setVariable);
            }
        }

        var condition = string.Join(" && ", setVariables.Select(setVariable => setVariable + " is null"));
        var missingReportedVariable = "__fkMissingReported" + parameterIndex.ToString(CultureInfo.InvariantCulture);
        var sourceLiteral = SymbolDisplay.FormatLiteral(
            recordFkInfo.RecordSymbol.Name + "." + fkParameter.PropertyName, quote: true);
        var targetMembers = string.Join(", ", fkParameter.Branches.Select(branch => branch.TableSetMember).Distinct());
        var targetLiteral = SymbolDisplay.FormatLiteral(targetMembers, quote: true);

        sb.Append(indent).Append("if (").Append(condition).AppendLine(")");
        sb.Append(indent).AppendLine("{");
        sb.Append(indent).Append("    ").Append(missingReportedVariable).AppendLine(" = true;");
        sb.Append(indent).Append("    __errors.Add(global::Sdp.Manager.TableSetLoaderHelper.FkTargetNotLoadedError(")
            .Append(sourceLiteral).Append(", ").Append(targetLiteral).AppendLine("));");
        sb.Append(indent).AppendLine("}");
    }

    private static void EmitFkParameterCheck(
        StringBuilder sb,
        FkParameter fkParameter,
        int parameterIndex,
        RecordFkInfo recordFkInfo,
        Dictionary<FkTargetSetKey, string> fkSetVariables,
        string indent)
    {
        var propertyTypeFullyQualifiedName = fkParameter.PropertyType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
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

        var isNullable = TypeClassifier.IsNullable(fkParameter.PropertyType);

        sb.Append(indent).AppendLine("{");
        sb.Append(indent).Append("    var __v = __r.")
            .Append(EmitHelper.EscapeIdentifier(fkParameter.PropertyName)).AppendLine(";");

        var body = indent;
        if (isNullable)
        {
            sb.Append(indent).AppendLine("    if (__v is not null)");
            sb.Append(indent).AppendLine("    {");
            body = indent + "    ";
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
            sb.Append(body).AppendLine("    var __targetLoaded = false;");
            foreach (var branch in fkBranches)
            {
                var setKey = new FkTargetSetKey(branch.TableSetMember, branch.TargetColumn, propertyTypeFullyQualifiedName);
                var setVariable = fkSetVariables[setKey];
                sb.Append(body).Append("    if (").Append(setVariable).AppendLine(" is not null)");
                sb.Append(body).AppendLine("    {");
                sb.Append(body).AppendLine("        __targetLoaded = true;");
                sb.Append(body).Append("        if (!__ok && ").Append(setVariable).AppendLine(".Contains(__v))");
                sb.Append(body).AppendLine("        {");
                sb.Append(body).AppendLine("            __ok = true;");
                sb.Append(body).AppendLine("        }");
                sb.Append(body).AppendLine("    }");
            }

            var fkTargetMembers = string.Join(", ", fkBranches.Select(branch => branch.TableSetMember).Distinct());
            var fkTargets = string.Join(", ", fkBranches.Select(branch => branch.TableSetMember + "." + branch.TargetColumn).Distinct());

            sb.Append(body).AppendLine("    if (!__ok)");
            sb.Append(body).AppendLine("    {");
            sb.Append(body).AppendLine("        if (!__targetLoaded)");
            sb.Append(body).AppendLine("        {");
            AppendTargetNotLoadedError(body + "            ", fkTargetMembers);
            sb.Append(body).AppendLine("        }");
            sb.Append(body).AppendLine("        else");
            sb.Append(body).AppendLine("        {");
            AppendValueNotFoundError(body + "            ", "{__v}", fkTargets);
            sb.Append(body).AppendLine("        }");
            sb.Append(body).AppendLine("    }");
        }

        if (switchBranches.Count > 0)
        {
            var conditionColumn = switchBranches[0].ConditionColumn!;
            var conditionColumnAccess = EmitHelper.EscapeIdentifier(conditionColumn);

            var conditionPropertyType = switchBranches[0].ConditionType!;
            var conditionType = TypeClassifier.UnwrapNullable(conditionPropertyType);
            var typedLiterals = TryBuildTypedConditionLiterals(conditionType, switchBranches);
            if (typedLiterals.Count == 0)
            {
                var useInvariant = IsFormattable(conditionPropertyType);
                var isNonNullableValue = conditionPropertyType.IsValueType
                    && conditionPropertyType.NullableAnnotation != NullableAnnotation.Annotated;
                var conditionalOperator = isNonNullableValue ? "." : "?.";

                string conditionInitializer;
                if (useInvariant)
                {
                    var castType = isNonNullableValue ? "global::System.IFormattable" : "global::System.IFormattable?";
                    conditionInitializer = "((" + castType + ")__r." + conditionColumnAccess + ")" + conditionalOperator
                        + "ToString(null, global::System.Globalization.CultureInfo.InvariantCulture)";
                }
                else
                {
                    conditionInitializer = "__r." + conditionColumnAccess + conditionalOperator + "ToString()";
                }

                sb.Append(body).Append("    var __cond = ").Append(conditionInitializer).AppendLine(";");
            }

            sb.Append(body).AppendLine("    var __matched = false;");
            sb.Append(body).AppendLine("    var __switchTargetLoaded = false;");

            var conditionDisplay = typedLiterals.Count > 0 ? "{__r." + conditionColumnAccess + "}" : "{__cond}";
            var conditionSuffix = " (when " + conditionColumn + "=" + conditionDisplay + ")";

            for (var branchIndex = 0; branchIndex < switchBranches.Count; branchIndex++)
            {
                var branch = switchBranches[branchIndex];
                var conditionExpression = typedLiterals.Count > 0
                    ? "__r." + conditionColumnAccess + " == " + typedLiterals[branchIndex]
                    : "__cond == " + SymbolDisplay.FormatLiteral(branch.ConditionValue!, quote: true);

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

                AppendValueNotFoundError(
                    body + "                ",
                    "{__v}" + conditionSuffix,
                    branch.TableSetMember + "." + branch.TargetColumn);
                sb.Append(body).AppendLine("            }");
                sb.Append(body).AppendLine("        }");
                sb.Append(body).AppendLine("    }");
            }

            var conditionColumnLiteral = SymbolDisplay.FormatLiteral(conditionColumn, quote: true);
            var switchTargetMembers = string.Join(", ", switchBranches.Select(branch => branch.TableSetMember).Distinct());

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
            sb.Append(indent).AppendLine("    }");
        }

        sb.Append(indent).AppendLine("}");
    }

    private static IParameterSymbol? FindKeyParameter(INamedTypeSymbol record)
    {
        var primaryConstructor = SinglePrimaryConstructorResolver.Resolve(record);
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
            var primaryConstructor = SinglePrimaryConstructorResolver.Resolve(keyType);
            if (primaryConstructor is { Parameters.Length: 1 })
            {
                access += "." + EmitHelper.EscapeIdentifier(primaryConstructor.Parameters[0].Name);
            }
        }

        return "{" + access + "}";
    }
}
