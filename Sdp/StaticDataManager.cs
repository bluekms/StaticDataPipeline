using System.Reflection;
using Sdp.Table;

namespace Sdp;

public abstract class StaticDataManager<TTableSet>
    where TTableSet : class
{
    private volatile TTableSet current = null!;

    protected TTableSet Current => current;

    public void Load(string csvDir, List<string>? disabledTables = null)
    {
        current = BuildTables(csvDir, disabledTables);
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
            throw new AggregateException("One or more tables failed to load.", errors);
        }

        return (TTableSet)ctor.Invoke(args);
    }

    private static object? CreateArg(ParameterInfo param, string csvDir, List<string>? disabledTables)
    {
        if (!IsStaticDataTable(param.ParameterType))
        {
            throw new InvalidOperationException(FormattableString.Invariant(
                $"Parameter '{param.Name}' of type '{param.ParameterType.Name}' is not a StaticDataTable<,> subtype."));
        }

        if (disabledTables?.Contains(param.Name!) == true)
        {
            return null;
        }

        return Activator.CreateInstance(param.ParameterType, csvDir);
    }

    private static bool IsStaticDataTable(Type type)
    {
        var t = type;
        while (t != null)
        {
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(StaticDataTable<,>))
            {
                return true;
            }

            t = t.BaseType;
        }

        return false;
    }
}
