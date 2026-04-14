using System.Globalization;
using System.Reflection;
using Sdp.Attributes;
using Sdp.Resources;
using Sdp.Table;

namespace Sdp.Manager;

internal static class SwitchForeignKeyValidator
{
    internal static void Validate(Dictionary<string, IStaticDataTable> tableMap)
    {
        var errors = new List<Exception>();
        var cache = new Dictionary<(string TableName, string ColumnName), HashSet<object?>>();

        foreach (var (_, table) in tableMap)
        {
            var recordType = table.RecordType;
            var checks = BuildSwitchFkChecks(recordType, tableMap, errors);

            if (checks.Count == 0)
            {
                continue;
            }

            foreach (var record in table.GetAllRecords())
            {
                foreach (var check in checks)
                {
                    ValidateSwitchFkRecord(record, check, recordType, errors, cache);
                }
            }
        }

        if (errors.Count > 0)
        {
            throw new AggregateException(Messages.FkValidationFailed, errors);
        }
    }

    private static List<SwitchFkCheck> BuildSwitchFkChecks(
        Type recordType,
        Dictionary<string, IStaticDataTable> tableMap,
        List<Exception> errors)
    {
        var checks = new List<SwitchFkCheck>();

        foreach (var (param, sFkAttrs) in ForeignKeyResolver.GetParamsWithAttribute<SwitchForeignKeyAttribute>(recordType))
        {
            foreach (var conditionGroup in sFkAttrs.GroupBy(sFkAttr => sFkAttr.ConditionColumnName))
            {
                var conditionProp = recordType.GetProperty(conditionGroup.Key);
                if (conditionProp is null)
                {
                    errors.Add(new InvalidOperationException(string.Format(
                        CultureInfo.CurrentCulture,
                        Messages.Composite.SwitchFkConditionColumnNotFound,
                        conditionGroup.Key,
                        recordType.Name)));

                    continue;
                }

                var branches = new List<SwitchFkBranch>();
                foreach (var sFkAttr in conditionGroup)
                {
                    if (ForeignKeyResolver.TryResolveTarget(
                            sFkAttr.TableSetName,
                            sFkAttr.RecordColumnName,
                            tableMap,
                            errors,
                            out var target))
                    {
                        branches.Add(new SwitchFkBranch(sFkAttr.ConditionValue, target!));
                    }
                }

                if (branches.Count > 0)
                {
                    checks.Add(new SwitchFkCheck(conditionProp, recordType.GetProperty(param.Name!)!, branches));
                }
            }
        }

        return checks;
    }

    private static void ValidateSwitchFkRecord(
        object record,
        SwitchFkCheck check,
        Type recordType,
        List<Exception> errors,
        Dictionary<(string TableName, string ColumnName), HashSet<object?>> cache)
    {
        var conditionValue = check.ConditionProperty.GetValue(record)?.ToString();
        var matchingBranch = check.Branches.Find(sFkBranch => sFkBranch.ConditionValue == conditionValue);

        if (matchingBranch is null)
        {
            return;
        }

        var fkValue = check.FkProperty.GetValue(record);
        if (ForeignKeyResolver.ContainsFkValue(matchingBranch.Target, fkValue, cache))
        {
            return;
        }

        errors.Add(new InvalidOperationException(string.Format(
            CultureInfo.CurrentCulture,
            Messages.Composite.FkValueNotFound,
            recordType.Name,
            check.FkProperty.Name,
            fkValue,
            FormattableString.Invariant(
                $"{matchingBranch.Target.TargetName}.{matchingBranch.Target.ColumnName} (when {check.ConditionProperty.Name}={conditionValue})"))));
    }

    private sealed record SwitchFkBranch(
        string ConditionValue,
        ForeignKeyResolver.FkTarget Target);

    private sealed record SwitchFkCheck(
        PropertyInfo ConditionProperty,
        PropertyInfo FkProperty,
        List<SwitchFkBranch> Branches);
}
