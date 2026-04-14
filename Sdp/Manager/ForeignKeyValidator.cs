using System.Globalization;
using System.Reflection;
using Sdp.Attributes;
using Sdp.Resources;
using Sdp.Table;

namespace Sdp.Manager;

internal static class ForeignKeyValidator
{
    internal static void Validate(Dictionary<string, IStaticDataTable> tableMap)
    {
        var errors = new List<Exception>();
        var cache = new Dictionary<(string TableName, string ColumnName), HashSet<object?>>();

        foreach (var (_, table) in tableMap)
        {
            var recordType = table.RecordType;
            var checks = BuildFkChecks(recordType, tableMap, errors);

            if (checks.Count == 0)
            {
                continue;
            }

            foreach (var record in table.GetAllRecords())
            {
                foreach (var check in checks)
                {
                    ValidateFkRecord(record, check, recordType, errors, cache);
                }
            }
        }

        if (errors.Count > 0)
        {
            throw new AggregateException(Messages.FkValidationFailed, errors);
        }
    }

    private static List<FkCheck> BuildFkChecks(
        Type recordType,
        Dictionary<string, IStaticDataTable> tableMap,
        List<Exception> errors)
    {
        var checks = new List<FkCheck>();

        foreach (var (param, fkAttrs) in ForeignKeyResolver.GetParamsWithAttribute<ForeignKeyAttribute>(recordType))
        {
            var targets = new List<ForeignKeyResolver.FkTarget>();
            foreach (var fkAttr in fkAttrs)
            {
                if (ForeignKeyResolver.TryResolveTarget(
                        fkAttr.TableSetName,
                        fkAttr.RecordColumnName,
                        tableMap,
                        errors,
                        out var target))
                {
                    targets.Add(target!);
                }
            }

            if (targets.Count > 0)
            {
                checks.Add(new FkCheck(recordType.GetProperty(param.Name!)!, targets));
            }
        }

        return checks;
    }

    private static void ValidateFkRecord(
        object record,
        FkCheck check,
        Type recordType,
        List<Exception> errors,
        Dictionary<(string TableName, string ColumnName), HashSet<object?>> cache)
    {
        var fkValue = check.FkProperty.GetValue(record);
        if (check.Targets.Any(t => ForeignKeyResolver.ContainsFkValue(t, fkValue, cache)))
        {
            return;
        }

        var targetList = string.Join(", ", check.Targets.Select(t =>
            FormattableString.Invariant($"{t.TargetName}.{t.ColumnName}")));

        errors.Add(new InvalidOperationException(string.Format(
            CultureInfo.CurrentCulture,
            Messages.Composite.FkValueNotFound,
            recordType.Name,
            check.FkProperty.Name,
            fkValue,
            targetList)));
    }

    private sealed record FkCheck(
        PropertyInfo FkProperty,
        List<ForeignKeyResolver.FkTarget> Targets);
}
