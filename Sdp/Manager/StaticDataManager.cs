using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Globalization;
using System.Reflection;
using Sdp.Attributes;
using Sdp.Csv;
using Sdp.Resources;
using Sdp.Table;

namespace Sdp.Manager;

public abstract class StaticDataManager<TTableSet>
    where TTableSet : class
{
    private volatile TTableSet current = null!;

    protected TTableSet Current => current;

    public async Task LoadAsync(string csvDir, List<string>? disabledTables = null)
    {
        var tableSet = await BuildTablesAsync(csvDir, disabledTables);
        FinalizeLoad(tableSet);
    }

    internal void Load(TTableSet tableSet)
    {
        FinalizeLoad(tableSet);
    }

    protected virtual void Validate(TTableSet tableSet)
    {
    }

    private void FinalizeLoad(TTableSet tableSet)
    {
        var tableMap = ForeignKeyResolver.BuildTableMap(tableSet);

        foreach (var table in tableMap.Values)
        {
            table.Validate();
        }

        ForeignKeyValidator.Validate(tableMap);
        SwitchForeignKeyValidator.Validate(tableMap);

        Validate(tableSet);
        current = tableSet;
    }

    private static async Task<TTableSet> BuildTablesAsync(string csvDir, List<string>? disabledTables)
    {
        var ctor = typeof(TTableSet).GetConstructors().Single();
        var parameters = ctor.GetParameters();
        var recordCache = new ConcurrentDictionary<RecordCacheKey, Lazy<Task<object>>>();

        var tasks = parameters
            .Select(p => CreateTableAsync(p, csvDir, disabledTables, recordCache))
            .ToArray();

        try
        {
            await Task.WhenAll(tasks);
        }
        catch
        {
            // 모든 Task가 완료될 때까지 대기. 예외는 아래에서 일괄 수집
        }

        var errors = tasks
            .Where(t => t.IsFaulted)
            .SelectMany(t => t.Exception!.InnerExceptions)
            .ToList();

        if (errors.Count > 0)
        {
            throw new AggregateException(Messages.TablesFailedToLoad, errors);
        }

        return (TTableSet)ctor.Invoke(tasks.Select(t => t.Result).ToArray());
    }

    private static async Task<object?> CreateTableAsync(
        ParameterInfo param,
        string csvDir,
        List<string>? disabledTables,
        ConcurrentDictionary<RecordCacheKey, Lazy<Task<object>>> recordCache)
    {
        var tableType = param.ParameterType;
        if (!IsStaticDataTable(tableType))
        {
            throw new InvalidOperationException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.InvalidTableParameter,
                param.Name!,
                tableType.Name));
        }

        if (disabledTables?.Contains(param.Name!) == true)
        {
            return null;
        }

        var recordType = ExtractRecordType(tableType);
        var csvPath = ResolveCsvPath(csvDir, recordType);

        var lazyTask = recordCache.GetOrAdd(
            new RecordCacheKey(csvPath, recordType),
            key => new Lazy<Task<object>>(() => CsvLoader.LoadAsync(key.CsvPath, key.RecordType)));

        var records = await lazyTask.Value;

        var ctor = FindTableConstructor(tableType, recordType);
        try
        {
            return ctor.Invoke([records]);
        }
        catch (TargetInvocationException tie)
        {
            throw tie.InnerException ?? tie;
        }
    }

    private static bool IsStaticDataTable(Type type)
        => typeof(IStaticDataTable).IsAssignableFrom(type);

    private static Type ExtractRecordType(Type tableType)
    {
        var current = tableType;
        while (current is not null && current != typeof(object))
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(StaticDataTable<,>))
            {
                return current.GetGenericArguments()[1];
            }

            current = current.BaseType;
        }

        throw new InvalidOperationException(FormattableString.Invariant(
            $"{tableType.Name} does not derive from StaticDataTable<,>."));
    }

    private static string ResolveCsvPath(string csvDir, Type recordType)
    {
        var attr = recordType.GetCustomAttribute<StaticDataRecordAttribute>();

        if (attr is null)
        {
            throw new InvalidOperationException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.StaticDataRecordAttributeRequired,
                recordType.Name));
        }

        var fileName = FormattableString.Invariant($"{attr.ExcelFileName}.{attr.SheetName}.csv");
        return Path.Combine(csvDir, fileName);
    }

    private sealed record RecordCacheKey(string CsvPath, Type RecordType);

    private static ConstructorInfo FindTableConstructor(Type tableType, Type recordType)
    {
        var paramType = typeof(ImmutableList<>).MakeGenericType(recordType);
        var ctor = tableType.GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            [paramType]);

        if (ctor is null)
        {
            throw new InvalidOperationException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.TableConstructorNotFound,
                tableType.Name,
                recordType.Name));
        }

        return ctor;
    }
}
