using System.Text;
using CLICommonLibrary;
using Microsoft.Extensions.Logging;
using SchemaInfoScanner;
using SchemaInfoScanner.Extensions;
using Sdp.Attributes;
using StaticDataHeaderGenerator.ProgramOptions;
using StaticDataHeaderGenerator.Resources;

namespace StaticDataHeaderGenerator.OptionHandlers;

public class GenerateAllHeaderHandler
{
    public static int Generate(GenerateAllHeaderOptions options)
    {
        var logger = string.IsNullOrEmpty(options.LogPath)
            ? Logger.CreateLoggerWithoutFile<Program>(options.MinLogLevel)
            : Logger.CreateLogger<Program>(options.MinLogLevel, options.LogPath);

        LogInformation(logger, Messages.GeneratingHeaderFile(), null);

        var catalogs = RecordScanner.Scan(options.RecordCsPath, logger);
        if (catalogs.RecordSchemaCatalog.StaticDataRecordSchemata.Count == 0)
        {
            var exception = new ArgumentException("No records found.");
            LogError(logger, exception.Message, exception);
            throw exception;
        }

        var sb = new StringBuilder();
        sb.AppendLine("# StaticDataHeaderGenerator Results");
        sb.AppendLine();

        foreach (var targetRecordSchema in catalogs.RecordSchemaCatalog.StaticDataRecordSchemata)
        {
            var headers = RecordFlattener.Flatten(
                targetRecordSchema,
                catalogs.RecordSchemaCatalog,
                logger);

            var excelFileName = targetRecordSchema.GetAttributeValue<StaticDataRecordAttribute, string>(0);
            var sheetName = targetRecordSchema.GetAttributeValue<StaticDataRecordAttribute, string>(1);

            sb.AppendLine(FormattableString.Invariant($"## {targetRecordSchema.RecordName.FullName}"));
            sb.AppendLine(FormattableString.Invariant($"- Excel File: `Docs/SampleExcel/{excelFileName}.xlsx`"));
            sb.AppendLine(FormattableString.Invariant($"- Sheet Name: `{sheetName}`"));
            sb.AppendLine();

            sb.AppendLine("### Headers (List)");
            foreach (var header in headers)
            {
                sb.AppendLine(FormattableString.Invariant($"- {header}"));
            }

            sb.AppendLine();

            sb.AppendLine("### Headers (TSV)");
            sb.AppendLine("```");
            sb.AppendLine(string.Join("\t", headers));
            sb.AppendLine("```");
            sb.AppendLine();

            LogInformation(logger, Messages.HeadersGenerated(targetRecordSchema.RecordName.FullName), null);
        }

        if (!string.IsNullOrEmpty(options.OutputFileName))
        {
            var outputFileName = string.IsNullOrEmpty(Path.GetExtension(options.OutputFileName))
                ? FormattableString.Invariant($"{options.OutputFileName}.md")
                : options.OutputFileName;

            var directoryName = Path.GetDirectoryName(outputFileName);
            if (!string.IsNullOrEmpty(directoryName) && !Directory.Exists(directoryName))
            {
                Directory.CreateDirectory(directoryName);
            }

            File.WriteAllText(outputFileName, sb.ToString());

            LogInformation(logger, Messages.HeaderFileSaved(outputFileName), null);
        }

        return 0;
    }

    public static async Task<int> GenerateAsync(GenerateAllHeaderOptions options, CancellationToken cancellationToken = default)
    {
        var logger = string.IsNullOrEmpty(options.LogPath)
            ? Logger.CreateLoggerWithoutFile<Program>(options.MinLogLevel)
            : Logger.CreateLogger<Program>(options.MinLogLevel, options.LogPath);

        LogInformation(logger, Messages.GeneratingHeaderFile(), null);

        var catalogs = await RecordScanner.ScanAsync(options.RecordCsPath, logger, cancellationToken);
        if (catalogs.RecordSchemaCatalog.StaticDataRecordSchemata.Count == 0)
        {
            var exception = new ArgumentException("No records found.");
            LogError(logger, exception.Message, exception);
            throw exception;
        }

        var sb = new StringBuilder();
        sb.AppendLine("# StaticDataHeaderGenerator Results");
        sb.AppendLine();

        foreach (var targetRecordSchema in catalogs.RecordSchemaCatalog.StaticDataRecordSchemata)
        {
            var headers = RecordFlattener.Flatten(
                targetRecordSchema,
                catalogs.RecordSchemaCatalog,
                logger);

            var excelFileName = targetRecordSchema.GetAttributeValue<StaticDataRecordAttribute, string>(0);
            var sheetName = targetRecordSchema.GetAttributeValue<StaticDataRecordAttribute, string>(1);

            sb.AppendLine(FormattableString.Invariant($"## {targetRecordSchema.RecordName.FullName}"));
            sb.AppendLine(FormattableString.Invariant($"- Excel File: `Docs/SampleExcel/{excelFileName}.xlsx`"));
            sb.AppendLine(FormattableString.Invariant($"- Sheet Name: `{sheetName}`"));
            sb.AppendLine();

            sb.AppendLine("### Headers (List)");
            foreach (var header in headers)
            {
                sb.AppendLine(FormattableString.Invariant($"- {header}"));
            }

            sb.AppendLine();

            sb.AppendLine("### Headers (TSV)");
            sb.AppendLine("```");
            sb.AppendLine(string.Join("\t", headers));
            sb.AppendLine("```");
            sb.AppendLine();

            LogInformation(logger, Messages.HeadersGenerated(targetRecordSchema.RecordName.FullName), null);
        }

        if (!string.IsNullOrEmpty(options.OutputFileName))
        {
            var outputFileName = string.IsNullOrEmpty(Path.GetExtension(options.OutputFileName))
                ? FormattableString.Invariant($"{options.OutputFileName}.md")
                : options.OutputFileName;

            var directoryName = Path.GetDirectoryName(outputFileName);
            if (!string.IsNullOrEmpty(directoryName) && !Directory.Exists(directoryName))
            {
                Directory.CreateDirectory(directoryName);
            }

            await File.WriteAllTextAsync(outputFileName, sb.ToString(), cancellationToken);

            LogInformation(logger, Messages.HeaderFileSaved(outputFileName), null);
        }

        return 0;
    }

    private static readonly Action<ILogger, string, Exception?> LogTrace =
        LoggerMessage.Define<string>(LogLevel.Trace, new EventId(0, nameof(LogTrace)), "{Message}");

    private static readonly Action<ILogger, string, Exception?> LogInformation =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(0, nameof(LogInformation)), "{Message}");

    private static readonly Action<ILogger, string, Exception?> LogError =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(0, nameof(LogError)), "{Message}");
}
