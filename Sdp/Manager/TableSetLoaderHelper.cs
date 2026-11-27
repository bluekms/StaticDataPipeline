using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Sdp.Resources;

namespace Sdp.Manager;

public static class TableSetLoaderHelper
{
    private const string CsvErrorMark = "Sdp.CsvError";

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

        var table = await loadAsync(csvDir, logger).ConfigureAwait(false);

        stopwatch.Stop();
        logger.LogTrace(Messages.LoadedTable, tableName, stopwatch.ElapsedMilliseconds);

        return table;
    }

    public static void ThrowIfUnknownDisabledTables(List<string>? disabledTables, string[] tableNames)
    {
        if (disabledTables is null)
        {
            return;
        }

        var unknownNames = new List<string>();
        foreach (var disabledTable in disabledTables.Distinct())
        {
            if (Array.IndexOf(tableNames, disabledTable) >= 0)
            {
                continue;
            }

            var similar = Array.Find(
                tableNames,
                x => string.Equals(x, disabledTable, StringComparison.OrdinalIgnoreCase));
            if (similar is null)
            {
                unknownNames.Add($"'{disabledTable}'");

                continue;
            }

            unknownNames.Add(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.UnknownDisabledTableSimilarHint,
                disabledTable,
                similar));
        }

        if (unknownNames.Count == 0)
        {
            return;
        }

        var message = string.Format(
            CultureInfo.CurrentCulture,
            Messages.Composite.UnknownDisabledTables,
            string.Join(", ", unknownNames));

        throw new ArgumentException(message, nameof(disabledTables));
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

    public static Exception FkValueNotFoundError(string source, string propertyName, string value, string target)
    {
        return new InvalidOperationException(string.Format(
            CultureInfo.CurrentCulture,
            Messages.Composite.FkValueNotFound,
            source,
            propertyName,
            value,
            target));
    }

    public static Exception SwitchFkValueNotFoundError(
        string source,
        string propertyName,
        string value,
        string target,
        string conditionColumn,
        string conditionValue)
    {
        return new InvalidOperationException(string.Format(
            CultureInfo.CurrentCulture,
            Messages.Composite.SwitchFkValueNotFound,
            source,
            propertyName,
            value,
            target,
            conditionColumn,
            conditionValue));
    }

    public static Exception DuplicateKeyError(string fileName, string recordName, string key)
    {
        return new InvalidOperationException(string.Format(
            CultureInfo.CurrentCulture,
            Messages.Composite.CsvDuplicateKey,
            fileName,
            key,
            recordName));
    }

    public static Exception FkTargetNotLoadedError(string source, string target)
    {
        return new InvalidOperationException(string.Format(
            CultureInfo.CurrentCulture,
            Messages.Composite.FkTargetNotFound,
            source,
            target));
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

    public static void AddErrorsNotReportedBy(
        List<Exception> errors,
        IReadOnlyCollection<Exception> candidates,
        params Task[] sameCsvReaders)
    {
        var reported = new HashSet<string>(StringComparer.Ordinal);
        foreach (var sameCsvReader in sameCsvReaders)
        {
            if (sameCsvReader.Exception is null)
            {
                continue;
            }

            foreach (var error in sameCsvReader.Exception.InnerExceptions.Where(IsCsvError))
            {
                reported.Add(ErrorIdentity(error));
            }
        }

        foreach (var candidate in candidates)
        {
            if (IsCsvError(candidate) && reported.Contains(ErrorIdentity(candidate)))
            {
                continue;
            }

            errors.Add(candidate);
        }
    }

    public static void MarkAsCsvError(Exception error)
    {
        error.Data[CsvErrorMark] = true;
    }

    private static bool IsCsvError(Exception error)
    {
        return error.Data.Contains(CsvErrorMark);
    }

    private static string ErrorIdentity(Exception error)
    {
        return error.GetType().FullName + ": " + error.Message;
    }
}
