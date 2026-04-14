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
        return ctor.GetParameters()
            .Select(p => (Name: p.Name!, Table: type.GetProperty(p.Name!)?.GetValue(tableSet) as IStaticDataTable))
            .Where(x => x.Table is not null)
            .ToDictionary(x => x.Name, x => x.Table!);
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

        var isPk = columnName == targetTable.PrimaryKeyPropertyName;
        if (!isPk && targetTable.RecordType.GetProperty(columnName) is null)
        {
            errors.Add(new InvalidOperationException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.IndexNotRegistered,
                columnName,
                targetTable.RecordType.Name)));

            return false;
        }

        target = new FkTarget(tableSetName, columnName, targetTable, isPk);
        return true;
    }

    internal static bool ContainsFkValue(
        FkTarget target,
        object? fkValue,
        Dictionary<(string TargetName, string ColumnName), HashSet<object?>> cache)
    {
        if (target.IsPrimaryKey)
        {
            return target.TargetTable.ContainsPrimaryKey(fkValue);
        }

        var cacheKey = (target.TargetName, target.ColumnName);

        // 첫 조회 시(cache에 값이 없음) target 테이블의 해당 컬럼 전체 값을 조회해서 HashSet cache에 저장
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
        IStaticDataTable TargetTable,
        bool IsPrimaryKey);
}
