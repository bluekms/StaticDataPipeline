using System.Globalization;
using Microsoft.Extensions.Logging;
using SchemaInfoScanner.Catalogs;
using SchemaInfoScanner.Extensions;
using SchemaInfoScanner.NameObjects;
using SchemaInfoScanner.Resources;
using SchemaInfoScanner.TypeCheckers;
using Sdp.Attributes;

namespace SchemaInfoScanner;

public static class RecordComplianceChecker
{
    public static void Check(RecordSchemaCatalog recordSchemaCatalog, ILogger logger)
    {
        var visited = new HashSet<RecordName>();

        if (recordSchemaCatalog.StaticDataRecordSchemata.Count is 0)
        {
            throw new InvalidOperationException(Messages.NoStaticDataRecordFound);
        }

        foreach (var recordSchema in recordSchemaCatalog.StaticDataRecordSchemata)
        {
            if (!visited.Add(recordSchema.RecordName))
            {
                var msg = string.Format(
                    CultureInfo.CurrentCulture,
                    Messages.Composite.AlreadyVisited,
                    recordSchema.RecordName.FullName);
                LogTrace(logger, msg, null);
                continue;
            }

            foreach (var propertySchema in recordSchema.PropertySchemata)
            {
                try
                {
                    SupportedTypeChecker.Check(propertySchema, recordSchemaCatalog, visited, logger);
                }
                catch (Exception e)
                {
                    LogException(logger, $"{propertySchema.PropertyName.FullName}: {e.Message}", e);
                    throw;
                }
            }
        }
    }

    public static int TryCheck(RecordSchemaCatalog recordSchemaCatalog, ILogger logger)
    {
        var exceptionCount = 0;
        var visited = new HashSet<RecordName>();
        foreach (var recordSchema in recordSchemaCatalog.StaticDataRecordSchemata)
        {
            if (!recordSchema.HasAttribute<StaticDataRecordAttribute>())
            {
                continue;
            }

            if (!visited.Add(recordSchema.RecordName))
            {
                var msg = string.Format(
                    CultureInfo.CurrentCulture,
                    Messages.Composite.AlreadyVisited,
                    recordSchema.RecordName.FullName);
                LogTrace(logger, msg, null);
                continue;
            }

            foreach (var recordParameter in recordSchema.PropertySchemata)
            {
                try
                {
                    SupportedTypeChecker.Check(recordParameter, recordSchemaCatalog, visited, logger);
                }
                catch (Exception e)
                {
                    exceptionCount += 1;
                    LogException(logger, $"{recordParameter.PropertyName.FullName}: {e.Message}", e);
                }
            }
        }

        return exceptionCount;
    }

    private static readonly Action<ILogger, string, Exception?> LogTrace =
        LoggerMessage.Define<string>(LogLevel.Trace, new EventId(0, nameof(LogTrace)), "{Message}");

    private static readonly Action<ILogger, string, Exception?> LogException =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(1, nameof(LogException)), "{Message}");
}
