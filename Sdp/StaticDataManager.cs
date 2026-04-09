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
        var ctor = typeof(TTableSet).GetConstructors().Single();
        var tableMap = ctor.GetParameters()
            .ToDictionary(
                p => p.Name!,
                p => typeof(TTableSet).GetProperty(p.Name!)?.GetValue(tableSet) as IStaticDataTable);

        var errors = new List<Exception>();
        var valueSetCache = new Dictionary<(string TableName, string ColumnName), HashSet<object?>>();

        foreach (var (_, tableObj) in tableMap)
        {
            if (tableObj is null)
            {
                continue;
            }

            var recordType = tableObj.RecordType;
            var fkParams = recordType.GetConstructors().Single()
                .GetParameters()
                .Select(p => (Param: p, Attrs: p.GetCustomAttributes<ForeignKeyAttribute>().ToList()))
                .Where(x => x.Attrs.Count > 0)
                .ToList();

            if (fkParams.Count == 0)
            {
                continue;
            }

            var validChecks = new List<FkCheck>();

            foreach (var (param, attrs) in fkParams)
            {
                var targets = new List<FkTarget>();

                foreach (var attr in attrs)
                {
                    if (!tableMap.TryGetValue(attr.TableSetName, out var targetTable) || targetTable is null)
                    {
                        errors.Add(new InvalidOperationException(string.Format(
                            CultureInfo.CurrentCulture,
                            Messages.Composite.FkTargetNotFound,
                            attr.TableSetName)));

                        continue;
                    }

                    var isPk = attr.RecordColumnName == targetTable.PrimaryKeyPropertyName;
                    if (!isPk)
                    {
                        var prop = targetTable.RecordType.GetProperty(attr.RecordColumnName);
                        if (prop is null)
                        {
                            errors.Add(new InvalidOperationException(string.Format(
                                CultureInfo.CurrentCulture,
                                Messages.Composite.IndexNotRegistered,
                                attr.RecordColumnName,
                                targetTable.RecordType.Name)));

                            continue;
                        }
                    }

                    targets.Add(new FkTarget(attr.TableSetName, attr.RecordColumnName, targetTable, isPk));
                }

                if (targets.Count > 0)
                {
                    validChecks.Add(new FkCheck(recordType.GetProperty(param.Name!)!, targets));
                }
            }

            if (validChecks.Count == 0)
            {
                continue;
            }

            foreach (var record in tableObj.GetAllRecords())
            {
                foreach (var check in validChecks)
                {
                    var fkValue = check.FkProperty.GetValue(record);
                    var existsInAny = check.Targets.Any(t =>
                        ContainsFkValue(t, fkValue, valueSetCache));

                    if (!existsInAny)
                    {
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
                }
            }
        }

        if (errors.Count > 0)
        {
            throw new AggregateException(Messages.FkValidationFailed, errors);
        }
    }

    private static bool ContainsFkValue(
        FkTarget target,
        object? fkValue,
        Dictionary<(string TargetName, string ColumnName), HashSet<object?>> valueSetCache)
    {
        if (target.IsPrimaryKey)
        {
            return target.TargetTable.ContainsPrimaryKey(fkValue);
        }

        var cacheKey = (target.TargetName, target.ColumnName);
        if (!valueSetCache.TryGetValue(cacheKey, out var valueSet))
        {
            var prop = target.TargetTable.RecordType.GetProperty(target.ColumnName)!;
            valueSet = target.TargetTable.GetAllRecords()
                .Cast<object>()
                .Select(r => prop.GetValue(r))
                .ToHashSet();
            valueSetCache[cacheKey] = valueSet;
        }

        return valueSet.Contains(fkValue);
    }

    private sealed record FkTarget(
        string TargetName,
        string ColumnName,
        IStaticDataTable TargetTable,
        bool IsPrimaryKey);

    private sealed record FkCheck(PropertyInfo FkProperty, List<FkTarget> Targets);
}
