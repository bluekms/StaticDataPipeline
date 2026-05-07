using System.Globalization;
using System.Reflection;
using Sdp.Attributes;
using Sdp.Resources;
using Sdp.Table;

namespace Sdp.Manager;

internal static class ForeignKeyTargetValidator
{
    internal static AggregateException? Validate<TTableSet>()
        where TTableSet : class
    {
        var errors = new List<Exception>();
        var recordTypeMap = BuildRecordTypeMap<TTableSet>();
        var visited = new HashSet<Type>();

        foreach (var recordType in recordTypeMap.Values)
        {
            if (!visited.Add(recordType))
            {
                continue;
            }

            ValidateFkAndSfkTargets(recordType, recordTypeMap, errors);
        }

        return errors.Count > 0
            ? new AggregateException(Messages.FkValidationFailed, errors)
            : null;
    }

    private static Dictionary<string, Type> BuildRecordTypeMap<TTableSet>()
    {
        var ctor = typeof(TTableSet).GetConstructors().Single();
        var map = new Dictionary<string, Type>();

        foreach (var param in ctor.GetParameters())
        {
            var recordType = ExtractRecordType(param.ParameterType);
            if (recordType is not null)
            {
                map[param.Name!] = recordType;
            }
        }

        return map;
    }

    private static Type? ExtractRecordType(Type tableType)
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

        return null;
    }

    private static void ValidateFkAndSfkTargets(
        Type recordType,
        Dictionary<string, Type> recordTypeMap,
        List<Exception> errors)
    {
        foreach (var param in recordType.GetConstructors().Single().GetParameters())
        {
            foreach (var attr in param.GetCustomAttributes<ForeignKeyAttribute>())
            {
                ValidateTarget(attr.TableSetName, attr.RecordColumnName, recordTypeMap, errors);
            }

            foreach (var attr in param.GetCustomAttributes<SwitchForeignKeyAttribute>())
            {
                ValidateTarget(attr.TableSetName, attr.RecordColumnName, recordTypeMap, errors);
            }
        }
    }

    private static void ValidateTarget(
        string tableSetName,
        string columnName,
        Dictionary<string, Type> recordTypeMap,
        List<Exception> errors)
    {
        if (!recordTypeMap.TryGetValue(tableSetName, out var targetRecordType))
        {
            errors.Add(new InvalidOperationException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.FkTargetNotFound,
                tableSetName)));

            return;
        }

        var targetParam = targetRecordType.GetConstructors().Single()
            .GetParameters()
            .FirstOrDefault(p => p.Name == columnName);

        if (targetParam?.GetCustomAttribute<SingleColumnCollectionAttribute>() is not null)
        {
            errors.Add(new InvalidOperationException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.FkTargetIsSingleColumnCollection,
                columnName,
                targetRecordType.Name)));
        }
    }
}
