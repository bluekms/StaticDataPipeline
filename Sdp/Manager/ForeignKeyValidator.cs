using System.Globalization;
using System.Reflection;
using Sdp.Attributes;
using Sdp.Resources;
using Sdp.Table;

namespace Sdp.Manager;

internal static class ForeignKeyValidator
{
    internal static List<FkCheck> BuildChecks(
        Type recordType,
        Dictionary<string, IStaticDataTable> tableMap,
        List<Exception> errors)
    {
        var checks = new List<FkCheck>();

        foreach (var attributed in ForeignKeyResolver.GetAttributedParameters<ForeignKeyAttribute>(recordType))
        {
            var targets = new List<ForeignKeyResolver.FkTarget>();
            foreach (var fkAttr in attributed.Attrs)
            {
                var target = ForeignKeyResolver.ResolveTarget(
                    fkAttr.TableSetName,
                    fkAttr.RecordColumnName,
                    tableMap,
                    errors);

                if (target is not null)
                {
                    targets.Add(target);
                }
            }

            if (targets.Count > 0)
            {
                checks.Add(new FkCheck(recordType.GetProperty(attributed.Param.Name!)!, targets));
            }
        }

        return checks;
    }

    internal static void ValidateRecord(
        object record,
        FkCheck check,
        Type recordType,
        List<Exception> errors,
        Dictionary<ForeignKeyResolver.TargetColumn, HashSet<object?>> cache)
    {
        var fkValue = check.FkProperty.GetValue(record);
        if (check.Targets.Any(t => ForeignKeyResolver.ContainsFkValue(t, fkValue, cache)))
        {
            return;
        }

        var targetList = string.Join(", ", check.Targets.Select(t => t.QualifiedName));

        errors.Add(new InvalidOperationException(string.Format(
            CultureInfo.CurrentCulture,
            Messages.Composite.FkValueNotFound,
            recordType.Name,
            check.FkProperty.Name,
            fkValue,
            targetList)));
    }

    internal sealed record FkCheck(
        PropertyInfo FkProperty,
        List<ForeignKeyResolver.FkTarget> Targets);
}
