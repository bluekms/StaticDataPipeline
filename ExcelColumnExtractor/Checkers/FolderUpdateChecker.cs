using System.Globalization;
using ExcelColumnExtractor.Resources;
using ExcelColumnExtractor.Scanners;
using Microsoft.Extensions.Logging;

namespace ExcelColumnExtractor.Checkers;

public static class FolderUpdateChecker
{
    private static string DateTimeFormat => "yyyy-MM-dd HH:mm:ss";

    public static void Check(FolderState before, FolderState after, ILogger logger)
    {
        if (before.FolderPath != after.FolderPath)
        {
            throw new ArgumentException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.FolderPathsDifferent,
                before.FolderPath,
                after.FolderPath));
        }

        var added = after.FileStates.Keys.Except(before.FileStates.Keys).ToList();
        foreach (var path in added)
        {
            var msg = string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.FileAdded,
                path);
            LogWarning(logger, msg, null);
        }

        var removed = before.FileStates.Keys.Except(after.FileStates.Keys).ToList();
        foreach (var path in removed)
        {
            var msg = string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.FileRemoved,
                path);
            LogWarning(logger, msg, null);
        }

        foreach (var file in before.FileStates.Keys.Intersect(after.FileStates.Keys))
        {
            var beforeTime = before.FileStates[file];
            var afterTime = after.FileStates[file];

            if (beforeTime != afterTime)
            {
                if (afterTime < beforeTime)
                {
                    throw new InvalidOperationException(string.Format(
                        CultureInfo.CurrentCulture,
                        Messages.Composite.FileUpdatedBeforeCapture,
                        file,
                        beforeTime.ToString(DateTimeFormat, CultureInfo.InvariantCulture),
                        afterTime.ToString(DateTimeFormat, CultureInfo.InvariantCulture)));
                }

                var msg = string.Format(
                    CultureInfo.CurrentCulture,
                    Messages.Composite.FileUpdated,
                    file);
                LogWarning(logger, msg, null);
            }
        }
    }

    private static readonly Action<ILogger, string, Exception?> LogWarning =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(0, nameof(LogWarning)), "{Message}");
}
