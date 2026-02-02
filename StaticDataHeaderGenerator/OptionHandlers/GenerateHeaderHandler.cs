using System.Text;
using CLICommonLibrary;
using Eds.Attributes;
using Microsoft.Extensions.Logging;
using SchemaInfoScanner;
using SchemaInfoScanner.Extensions;
using StaticDataHeaderGenerator.ProgramOptions;

namespace StaticDataHeaderGenerator.OptionHandlers;

public static class GenerateHeaderHandler
{
    public static int Generate(GenerateHeaderOptions options)
    {
        var logger = string.IsNullOrEmpty(options.LogPath)
            ? Logger.CreateLoggerWithoutFile<Program>(options.MinLogLevel)
            : Logger.CreateLogger<Program>(options.MinLogLevel, options.LogPath);

        LogInformation(logger, "Generate Header File", null);

        var catalogs = RecordScanner.Scan(options.RecordCsPath, logger);
        if (catalogs.RecordSchemaCatalog.StaticDataRecordSchemata.Count == 0)
        {
            var exception = new ArgumentException($"RecordName {options.RecordName} is not found.");
            LogError(logger, exception.Message, exception);
            throw exception;
        }

        var targetRecordSchema = catalogs.RecordSchemaCatalog.StaticDataRecordSchemata
            .Single(x => x.RecordName.Name == options.RecordName);

        var headers = RecordFlattener.Flatten(
            targetRecordSchema,
            catalogs.RecordSchemaCatalog,
            logger);

        var excelFileName = targetRecordSchema.GetAttributeValue<StaticDataRecordAttribute, string>(0);
        var sheetName = targetRecordSchema.GetAttributeValue<StaticDataRecordAttribute, string>(1);

        var output = BuildMarkdownOutput(targetRecordSchema.RecordName.FullName, headers, excelFileName, sheetName);
        LogInformation(logger, FormattableString.Invariant($"\n{output}\n"), null);

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

            File.WriteAllText(outputFileName, output);

            LogInformation(logger, $"Header file saved to {outputFileName}", null);
        }

        return 0;
    }

    public static async Task<int> GenerateAsync(GenerateHeaderOptions options, CancellationToken cancellationToken = default)
    {
        var logger = string.IsNullOrEmpty(options.LogPath)
            ? Logger.CreateLoggerWithoutFile<Program>(options.MinLogLevel)
            : Logger.CreateLogger<Program>(options.MinLogLevel, options.LogPath);

        LogInformation(logger, "Generate Header File", null);

        var catalogs = await RecordScanner.ScanAsync(options.RecordCsPath, logger, cancellationToken);
        if (catalogs.RecordSchemaCatalog.StaticDataRecordSchemata.Count == 0)
        {
            var exception = new ArgumentException($"RecordName {options.RecordName} is not found.");
            LogError(logger, exception.Message, exception);
            throw exception;
        }

        var targetRecordSchema = catalogs.RecordSchemaCatalog.StaticDataRecordSchemata
            .Single(x => x.RecordName.Name == options.RecordName);

        var headers = RecordFlattener.Flatten(
            targetRecordSchema,
            catalogs.RecordSchemaCatalog,
            logger);

        var excelFileName = targetRecordSchema.GetAttributeValue<StaticDataRecordAttribute, string>(0);
        var sheetName = targetRecordSchema.GetAttributeValue<StaticDataRecordAttribute, string>(1);

        var output = BuildMarkdownOutput(targetRecordSchema.RecordName.FullName, headers, excelFileName, sheetName);
        LogInformation(logger, FormattableString.Invariant($"\n{output}\n"), null);

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

            await File.WriteAllTextAsync(outputFileName, output, cancellationToken);

            LogInformation(logger, $"Header file saved to {outputFileName}", null);
        }

        return 0;
    }

    private static readonly Action<ILogger, string, Exception?> LogInformation =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(0, nameof(LogInformation)), "{Message}");

    private static readonly Action<ILogger, string, Exception?> LogWarning =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(0, nameof(LogWarning)), "{Message}");

    private static readonly Action<ILogger, string, Exception?> LogError =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(0, nameof(LogError)), "{Message}");

    private static string BuildMarkdownOutput(string recordFullName, IReadOnlyList<string> headers, string excelFileName, string sheetName)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# StaticDataHeaderGenerator Results");
        sb.AppendLine();
        sb.AppendLine(FormattableString.Invariant($"## {recordFullName}"));
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

        return sb.ToString();
    }
}
