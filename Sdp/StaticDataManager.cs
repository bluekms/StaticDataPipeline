using System.Globalization;
using System.Reflection;
using Sdp.Attributes;
using Sdp.Resources;
using Sdp.Table;

namespace Sdp;

public abstract class StaticDataManager<TTableSet>
    where TTableSet : class
{
    private volatile TTableSet current = null!;

    protected TTableSet Current => current;

    public void Load(string csvDir, List<string>? disabledTables = null)
    {
        var tableSet = BuildTables(csvDir, disabledTables);
        ValidateForeignKeys(tableSet);
        Validate(tableSet);
        current = tableSet;
    }

    internal void Load(TTableSet tableSet)
    {
        ValidateForeignKeys(tableSet);
        Validate(tableSet);
        current = tableSet;
    }

    protected virtual void Validate(TTableSet tableSet)
    {
    }

    private static TTableSet BuildTables(string csvDir, List<string>? disabledTables = null)
    {
        var ctor = typeof(TTableSet).GetConstructors().Single();
        var parameters = ctor.GetParameters();
        var args = new object?[parameters.Length];
        var exceptions = new Exception?[parameters.Length];

        Parallel.For(0, parameters.Length, i =>
        {
            try
            {
                args[i] = CreateArg(parameters[i], csvDir, disabledTables);
            }
            catch (TargetInvocationException tie)
            {
                exceptions[i] = tie.InnerException ?? tie;
            }
            catch (Exception ex)
            {
                exceptions[i] = ex;
            }
        });

        var errors = exceptions
            .Where(e => e is not null)
            .Select(e => e!)
            .ToList();

        if (errors.Count > 0)
        {
            throw new AggregateException(Messages.TablesFailedToLoad, errors);
        }

        return (TTableSet)ctor.Invoke(args);
    }

    private static object? CreateArg(ParameterInfo param, string csvDir, List<string>? disabledTables)
    {
        if (!IsStaticDataTable(param.ParameterType))
        {
            throw new InvalidOperationException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.InvalidTableParameter,
                param.Name!,
                param.ParameterType.Name));
        }

        if (disabledTables?.Contains(param.Name!) == true)
        {
            return null;
        }

        return Activator.CreateInstance(param.ParameterType, csvDir);
    }

    private static bool IsStaticDataTable(Type type)
        => typeof(IStaticDataTable).IsAssignableFrom(type);

    private static void ValidateForeignKeys(TTableSet tableSet)
    {
        var tableMap = BuildTableMap(tableSet);
        var errors = new List<Exception>();
        var cache = new Dictionary<(string TableName, string ColumnName), HashSet<object?>>();

        foreach (var (_, table) in tableMap)
        {
            if (table is null)
            {
                continue;
            }

            var recordType = table.RecordType;
            var fkChecks = BuildFkChecks(recordType, tableMap, errors);
            var switchFkChecks = BuildSwitchFkChecks(recordType, tableMap, errors);

            if (fkChecks.Count == 0 && switchFkChecks.Count == 0)
            {
                continue;
            }

            foreach (var record in table.GetAllRecords())
            {
                foreach (var check in fkChecks)
                {
                    ValidateFkRecord(record, check, recordType, errors, cache);
                }

                foreach (var check in switchFkChecks)
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

    private static Dictionary<string, IStaticDataTable?> BuildTableMap(TTableSet tableSet)
    {
        var ctor = typeof(TTableSet).GetConstructors().Single();
        return ctor.GetParameters()
            .ToDictionary(
                p => p.Name!,
                p => typeof(TTableSet).GetProperty(p.Name!)?.GetValue(tableSet) as IStaticDataTable);
    }

    private static FkTarget? ResolveTarget(
        string tableSetName,
        string columnName,
        Dictionary<string, IStaticDataTable?> tableMap,
        List<Exception> errors)
    {
        if (!tableMap.TryGetValue(tableSetName, out var targetTable) || targetTable is null)
        {
            errors.Add(new InvalidOperationException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.FkTargetNotFound,
                tableSetName)));

            return null;
        }

        var isPk = columnName == targetTable.PrimaryKeyPropertyName;
        if (!isPk && targetTable.RecordType.GetProperty(columnName) is null)
        {
            errors.Add(new InvalidOperationException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.IndexNotRegistered,
                columnName,
                targetTable.RecordType.Name)));

            return null;
        }

        return new FkTarget(tableSetName, columnName, targetTable, isPk);
    }

    private static List<FkCheck> BuildFkChecks(
        Type recordType,
        Dictionary<string, IStaticDataTable?> tableMap,
        List<Exception> errors)
    {
        var checks = new List<FkCheck>();

        foreach (var (param, attrs) in GetParamsWithAttribute<ForeignKeyAttribute>(recordType))
        {
            var targets = attrs
                .Select(a => ResolveTarget(a.TableSetName, a.RecordColumnName, tableMap, errors))
                .OfType<FkTarget>()
                .ToList();

            if (targets.Count > 0)
            {
                checks.Add(new FkCheck(recordType.GetProperty(param.Name!)!, targets));
            }
        }

        return checks;
    }

    private static List<SwitchFkCheck> BuildSwitchFkChecks(
        Type recordType,
        Dictionary<string, IStaticDataTable?> tableMap,
        List<Exception> errors)
    {
        var checks = new List<SwitchFkCheck>();

        foreach (var (param, attrs) in GetParamsWithAttribute<SwitchForeignKeyAttribute>(recordType))
        {
            foreach (var conditionGroup in attrs.GroupBy(a => a.ConditionColumnName))
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

                var branches = conditionGroup
                    .Select(a => ResolveTarget(a.TableSetName, a.RecordColumnName, tableMap, errors) is { } target
                        ? new SwitchFkBranch(a.ConditionValue, target)
                        : null)
                    .OfType<SwitchFkBranch>()
                    .ToList();

                if (branches.Count > 0)
                {
                    checks.Add(new SwitchFkCheck(conditionProp, recordType.GetProperty(param.Name!)!, branches));
                }
            }
        }

        return checks;
    }

    private static IEnumerable<(ParameterInfo Param, List<TAttr> Attrs)> GetParamsWithAttribute<TAttr>(Type recordType)
        where TAttr : Attribute
        => recordType.GetConstructors().Single()
            .GetParameters()
            .Select(p => (Param: p, Attrs: p.GetCustomAttributes<TAttr>().ToList()))
            .Where(x => x.Attrs.Count > 0);

    private static void ValidateFkRecord(
        object record,
        FkCheck check,
        Type recordType,
        List<Exception> errors,
        Dictionary<(string TableName, string ColumnName), HashSet<object?>> cache)
    {
        var fkValue = check.FkProperty.GetValue(record);
        if (check.Targets.Any(t => ContainsFkValue(t, fkValue, cache)))
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

    private static void ValidateSwitchFkRecord(
        object record,
        SwitchFkCheck check,
        Type recordType,
        List<Exception> errors,
        Dictionary<(string TableName, string ColumnName), HashSet<object?>> cache)
    {
        var conditionValue = check.ConditionProperty.GetValue(record)?.ToString();
        var matchingBranch = check.Branches.Find(b => b.ConditionValue == conditionValue);

        if (matchingBranch is null)
        {
            return;
        }

        var fkValue = check.FkProperty.GetValue(record);
        if (ContainsFkValue(matchingBranch.Target, fkValue, cache))
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

    private static bool ContainsFkValue(
        FkTarget target,
        object? fkValue,
        Dictionary<(string TargetName, string ColumnName), HashSet<object?>> cache)
    {
        if (target.IsPrimaryKey)
        {
            return target.TargetTable.ContainsPrimaryKey(fkValue);
        }

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

    private sealed record FkTarget(
        string TargetName,
        string ColumnName,
        IStaticDataTable TargetTable,
        bool IsPrimaryKey);

    private sealed record FkCheck(
        PropertyInfo FkProperty,
        List<FkTarget> Targets);

    private sealed record SwitchFkBranch(
        string ConditionValue,
        FkTarget Target);

    private sealed record SwitchFkCheck(
        PropertyInfo ConditionProperty,
        PropertyInfo FkProperty,
        List<SwitchFkBranch> Branches);
}
