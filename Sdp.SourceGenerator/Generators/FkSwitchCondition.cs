using System.Globalization;
using System.Text;

namespace Sdp.SourceGenerator.Generators;

internal static class FkSwitchCondition
{
    public static List<FkBranch> SelectSwitchBranches(FkParameter fkParameter)
    {
        return fkParameter.Branches
            .Where(static branch => branch.IsSwitch)
            .ToList();
    }

    public static List<string> TargetMembers(FkParameter fkParameter)
    {
        return fkParameter.Branches
            .Where(static branch => branch.IsSwitch)
            .Select(static branch => branch.TableSetMember)
            .Distinct()
            .ToList();
    }

    public static string MissingReportedVariable(FkParameter fkParameter, string tableSetMember)
    {
        var targetIndex = TargetMembers(fkParameter).IndexOf(tableSetMember);
        return "__fkMissingReported" + fkParameter.Index.ToString(CultureInfo.InvariantCulture)
            + "_" + targetIndex.ToString(CultureInfo.InvariantCulture);
    }

    public static void EmitRuntimeParsedConditions(
        StringBuilder sb,
        FkParameter fkParameter,
        string indent)
    {
        var switchBranches = SelectSwitchBranches(fkParameter);
        for (var branchIndex = 0; branchIndex < switchBranches.Count; branchIndex++)
        {
            var branch = switchBranches[branchIndex];
            if (!branch.ConditionParsesAtRuntime)
            {
                continue;
            }

            sb.Append(indent).Append("var ").Append(Variable(fkParameter.Index, branchIndex))
                .Append(" = ").Append(branch.ConditionLiteral!).AppendLine(";");
        }
    }

    public static string BuildOperand(FkParameter fkParameter, FkBranch branch, int branchIndex)
    {
        return branch.ConditionParsesAtRuntime
            ? Variable(fkParameter.Index, branchIndex)
            : branch.ConditionLiteral!;
    }

    // bool 은 ToString() 이 "True" 라서 [SwitchForeignKey] 에 적는 표기(true, false)로 직접 찍는다.
    public static string BuildDisplay(string conditionAccess, FkBranch branch)
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

    private static string Variable(int parameterIndex, int branchIndex)
    {
        return "__switchCondition" + parameterIndex.ToString(CultureInfo.InvariantCulture)
            + "_" + branchIndex.ToString(CultureInfo.InvariantCulture);
    }

    private static string EscapeInterpolationFormat(string format)
    {
        return format
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("{", "{{")
            .Replace("}", "}}");
    }
}
