using System.Globalization;
using System.Reflection;
using Sdp.Resources;
using Sdp.Table;

namespace Sdp.Manager;

internal static class ForeignKeyResolver
{
    internal static Dictionary<string, IStaticDataTable> BuildTableMap<TTableSet>(TTableSet tableSet)
    {
        var type = typeof(TTableSet);
        var ctor = type.GetConstructors().Single();
        var map = new Dictionary<string, IStaticDataTable>();

        foreach (var param in ctor.GetParameters())
        {
            var name = param.Name!;
            var property = type.GetProperty(name);

            if (property?.GetValue(tableSet) is not IStaticDataTable table)
            {
                continue;
            }

            map[name] = table;
        }

        return map;
    }

    internal static bool TryResolveTarget(
        string tableSetName,
        string columnName,
        Dictionary<string, IStaticDataTable> tableMap,
        List<Exception> errors,
        out FkTarget? target)
    {
        target = null;

        if (!tableMap.TryGetValue(tableSetName, out var targetTable))
        {
            errors.Add(new InvalidOperationException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.FkTargetNotFound,
                tableSetName)));

            return false;
        }

        if (targetTable.RecordType.GetProperty(columnName) is null)
        {
            errors.Add(new InvalidOperationException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.IndexNotRegistered,
                columnName,
                targetTable.RecordType.Name)));

            return false;
        }

        target = new FkTarget(tableSetName, columnName, targetTable);
        return true;
    }

    internal static bool ContainsFkValue(
        FkTarget target,
        object? fkValue,
        Dictionary<(string TargetName, string ColumnName), HashSet<object?>> cache)
    {
        var cacheKey = (target.TargetName, target.ColumnName);

        if (!cache.TryGetValue(cacheKey, out var valueSet))
        {
            var prop = target.TargetTable.RecordType.GetProperty(target.ColumnName)!;

            valueSet = target.TargetTable.GetAllRecords()
                .Cast<object>()
                .Select(r => prop.GetValue(r))
                .ToHashSet();

            cache[cacheKey] = valueSet;
        }

        return valueSet.Contains(fkValue);
    }

    internal static IEnumerable<(ParameterInfo Param, List<TAttr> Attrs)> GetParamsWithAttribute<TAttr>(Type recordType)
        where TAttr : Attribute
    {
        return recordType.GetConstructors().Single()
            .GetParameters()
            .Select(p => (Param: p, Attrs: p.GetCustomAttributes<TAttr>().ToList()))
            .Where(x => x.Attrs.Count > 0);
    }

    internal sealed record FkTarget(
        string TargetName,
        string ColumnName,
        IStaticDataTable TargetTable);
}
