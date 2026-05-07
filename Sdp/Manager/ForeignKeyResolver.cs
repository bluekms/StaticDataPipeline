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

    internal static FkTarget? ResolveTarget(
        string tableSetName,
        string columnName,
        Dictionary<string, IStaticDataTable> tableMap,
        List<Exception> errors)
    {
        if (!tableMap.TryGetValue(tableSetName, out var targetTable))
        {
            errors.Add(new InvalidOperationException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.FkTargetNotFound,
                tableSetName)));

            return null;
        }

        if (targetTable.RecordType.GetProperty(columnName) is null)
        {
            errors.Add(new InvalidOperationException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.IndexNotRegistered,
                columnName,
                targetTable.RecordType.Name)));

            return null;
        }

        return new FkTarget(tableSetName, columnName, targetTable);
    }

    internal static bool ContainsFkValue(
        FkTarget target,
        object? fkValue,
        Dictionary<TargetColumn, HashSet<object?>> cache)
    {
        var cacheKey = new TargetColumn(target.TargetName, target.ColumnName);

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

    internal static List<AttributedParameter<TAttr>> GetAttributedParameters<TAttr>(Type recordType)
        where TAttr : Attribute
    {
        return recordType.GetConstructors().Single()
            .GetParameters()
            .Select(p => new AttributedParameter<TAttr>(p, p.GetCustomAttributes<TAttr>().ToList()))
            .Where(x => x.Attrs.Count > 0)
            .ToList();
    }

    internal sealed record FkTarget(
        string TargetName,
        string ColumnName,
        IStaticDataTable TargetTable)
    {
        public string QualifiedName
            => FormattableString.Invariant($"{TargetName}.{ColumnName}");
    }

    internal sealed record TargetColumn(
        string TableName,
        string ColumnName);

    internal sealed record AttributedParameter<TAttr>(
        ParameterInfo Param,
        List<TAttr> Attrs)
        where TAttr : Attribute;
}
