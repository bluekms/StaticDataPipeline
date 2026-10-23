using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Sdp.Resources;

namespace Sdp.Manager;

public static class TableSetLoaderHelper
{
    public static async Task<T?> LoadTableOrSkipAsync<T>(
        string csvDir,
        string tableName,
        List<string>? disabledTables,
        Func<string, ILogger, Task<T>> loadAsync,
        ILogger logger)
        where T : class
    {
        if (disabledTables is not null && disabledTables.Contains(tableName))
        {
            return null;
        }

        var stopwatch = Stopwatch.StartNew();

        var table = await loadAsync(csvDir, logger);

        stopwatch.Stop();
        logger.LogTrace(Messages.LoadedTable, tableName, stopwatch.ElapsedMilliseconds);

        return table;
    }

    public static string TablesFailedToLoadMessage
        => Messages.TablesFailedToLoad;

    public static string ForeignKeyValidationFailedMessage
        => Messages.FkValidationFailed;

    public static Exception NonNullableTableDisabledError(string tableName)
    {
        return new InvalidOperationException(string.Format(
            CultureInfo.CurrentCulture,
            Messages.Composite.NonNullableTableDisabled,
            tableName));
    }

    public static Exception FkValueNotFoundError(string source, string propertyName, string value, string targets)
    {
        return new InvalidOperationException(string.Format(
            CultureInfo.CurrentCulture,
            Messages.Composite.FkValueNotFound,
            source,
            propertyName,
            value,
            targets));
    }

    public static Exception FkTargetNotLoadedError(string source, string targets)
    {
        return new InvalidOperationException(string.Format(
            CultureInfo.CurrentCulture,
            Messages.Composite.FkTargetNotFound,
            source,
            targets));
    }

    public static Exception SwitchFkConditionNotMatchedError(string source, string propertyName, string conditionColumn, string conditionValue)
    {
        return new InvalidOperationException(string.Format(
            CultureInfo.CurrentCulture,
            Messages.Composite.SwitchFkConditionValueNotMatched,
            source,
            propertyName,
            conditionColumn,
            conditionValue));
    }
}
