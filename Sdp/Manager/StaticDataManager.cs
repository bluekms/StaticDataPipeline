using System.Globalization;
using System.Reflection;
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

        var tableMap = ForeignKeyResolver.BuildTableMap(tableSet);
        ForeignKeyValidator.Validate(tableMap);
        SwitchForeignKeyValidator.Validate(tableMap);

        Validate(tableSet);
        current = tableSet;
    }

    internal void Load(TTableSet tableSet)
    {
        var tableMap = ForeignKeyResolver.BuildTableMap(tableSet);
        ForeignKeyValidator.Validate(tableMap);
        SwitchForeignKeyValidator.Validate(tableMap);

        Validate(tableSet);
        current = tableSet;
    }

    protected virtual void Validate(TTableSet tableSet)
    {
    }

    private static async Task<TTableSet> BuildTablesAsync(string csvDir, List<string>? disabledTables)
    {
        var ctor = typeof(TTableSet).GetConstructors().Single();
        var parameters = ctor.GetParameters();
        var tasks = parameters
            .Select(p => CreateArgAsync(p, csvDir, disabledTables))
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

    private static async Task<object?> CreateArgAsync(ParameterInfo param, string csvDir, List<string>? disabledTables)
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

        var method = param.ParameterType.GetMethod(
            "CreateAsync",
            BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

        if (method is null)
        {
            throw new InvalidOperationException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.CreateAsyncNotFound,
                param.ParameterType.FullName ?? param.ParameterType.Name));
        }

        Task task;
        try
        {
            task = (Task)method.Invoke(null, [csvDir])!;
        }
        catch (TargetInvocationException tie)
        {
            throw tie.InnerException ?? tie;
        }

        await task;
        return task.GetType().GetProperty("Result")!.GetValue(task);
    }

    private static bool IsStaticDataTable(Type type)
        => typeof(IStaticDataTable).IsAssignableFrom(type);
}
