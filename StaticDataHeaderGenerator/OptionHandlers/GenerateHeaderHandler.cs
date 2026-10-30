using System.Globalization;
using System.Text;
using CLICommonLibrary;
using Microsoft.Extensions.Logging;
using SchemaInfoScanner;
using SchemaInfoScanner.Catalogs;
using SchemaInfoScanner.Extensions;
using SchemaInfoScanner.Schemata;
using Sdp.Attributes;
using StaticDataHeaderGenerator.ProgramOptions;
using StaticDataHeaderGenerator.Resources;

namespace StaticDataHeaderGenerator.OptionHandlers;

public static class GenerateHeaderHandler
{
    public static int Generate(GenerateHeaderOptions options)
    {
        var logger = string.IsNullOrEmpty(options.LogPath)
            ? Logger.CreateLoggerWithoutFile<Program>(options.MinLogLevel)
            : Logger.CreateLogger<Program>(options.MinLogLevel, options.LogPath);

        LogInformation(logger, Messages.GeneratingHeaderFile, null);

        var catalogs = RecordScanner.Scan(options.RecordCsPath, logger);

        var targetRecordSchema = FindRecord(catalogs.RecordSchemaCatalog, options.RecordName, logger);

        var headers = RecordFlattener.Flatten(
            targetRecordSchema,
            catalogs.RecordSchemaCatalog,
            logger);

        var separator = ResolveSeparator(options.Separator);
        HeaderSeparatorValidator.Validate(targetRecordSchema.RecordName.FullName, headers, separator);

        var excelFileName = targetRecordSchema.GetAttributeValue<StaticDataRecordAttribute, string>(0);
        var sheetName = targetRecordSchema.GetAttributeValue<StaticDataRecordAttribute, string>(1);

        var output = BuildMarkdownOutput(targetRecordSchema.RecordName.FullName, headers, excelFileName, sheetName, separator);
        LogInformation(logger, FormattableString.Invariant($"\n{output}\n"), null);

        if (!string.IsNullOrEmpty(options.OutputFileName))
        {
            var outputFileName = ResolveOutputFileName(options.OutputFileName);

            var directoryName = Path.GetDirectoryName(outputFileName);
            if (!string.IsNullOrEmpty(directoryName) && !Directory.Exists(directoryName))
            {
                Directory.CreateDirectory(directoryName);
            }

            File.WriteAllText(outputFileName, output);

            var msg = string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.HeaderFileSaved,
                outputFileName);
            LogInformation(logger, msg, null);
        }

        return 0;
    }

    public static async Task<int> GenerateAsync(GenerateHeaderOptions options, CancellationToken cancellationToken = default)
    {
        var logger = string.IsNullOrEmpty(options.LogPath)
            ? Logger.CreateLoggerWithoutFile<Program>(options.MinLogLevel)
            : Logger.CreateLogger<Program>(options.MinLogLevel, options.LogPath);

        LogInformation(logger, Messages.GeneratingHeaderFile, null);

        var catalogs = await RecordScanner.ScanAsync(options.RecordCsPath, logger, cancellationToken);

        var targetRecordSchema = FindRecord(catalogs.RecordSchemaCatalog, options.RecordName, logger);

        var headers = RecordFlattener.Flatten(
            targetRecordSchema,
            catalogs.RecordSchemaCatalog,
            logger);

        var separator = ResolveSeparator(options.Separator);
        HeaderSeparatorValidator.Validate(targetRecordSchema.RecordName.FullName, headers, separator);

        var excelFileName = targetRecordSchema.GetAttributeValue<StaticDataRecordAttribute, string>(0);
        var sheetName = targetRecordSchema.GetAttributeValue<StaticDataRecordAttribute, string>(1);

        var output = BuildMarkdownOutput(targetRecordSchema.RecordName.FullName, headers, excelFileName, sheetName, separator);
        LogInformation(logger, FormattableString.Invariant($"\n{output}\n"), null);

        if (!string.IsNullOrEmpty(options.OutputFileName))
        {
            var outputFileName = ResolveOutputFileName(options.OutputFileName);

            var directoryName = Path.GetDirectoryName(outputFileName);
            if (!string.IsNullOrEmpty(directoryName) && !Directory.Exists(directoryName))
            {
                Directory.CreateDirectory(directoryName);
            }

            await File.WriteAllTextAsync(outputFileName, output, cancellationToken);

            var msg = string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.HeaderFileSaved,
                outputFileName);
            LogInformation(logger, msg, null);
        }

        return 0;
    }

    private static readonly Action<ILogger, string, Exception?> LogInformation =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(0, nameof(LogInformation)), "{Message}");

    private static readonly Action<ILogger, string, Exception?> LogError =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(0, nameof(LogError)), "{Message}");

    private static RecordSchema FindRecord(RecordSchemaCatalog catalog, string recordName, ILogger logger)
    {
        var candidates = catalog.StaticDataRecordSchemata
            .Where(x => x.RecordName.Name == recordName || x.RecordName.FullName == recordName)
            .ToList();

        if (candidates.Count == 1)
        {
            return candidates[0];
        }

        var message = candidates.Count == 0
            ? string.Format(CultureInfo.CurrentCulture, Messages.Composite.RecordNameNotFound, recordName)
            : string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.RecordNameAmbiguous,
                recordName,
                string.Join(", ", candidates.Select(x => x.RecordName.FullName)));
        var exception = new ArgumentException(message);
        LogError(logger, exception.Message, exception);
        throw exception;
    }

    internal static string ResolveSeparator(string separator)
    {
        return separator
            .Replace(@"\t", "\t", StringComparison.Ordinal)
            .Replace(@"\n", "\n", StringComparison.Ordinal)
            .Replace(@"\r", "\r", StringComparison.Ordinal);
    }

    internal static string ResolveOutputFileName(string outputFileName)
    {
        return string.IsNullOrEmpty(Path.GetExtension(outputFileName))
            ? Path.ChangeExtension(outputFileName, ".md")
            : outputFileName;
    }

    private static string BuildMarkdownOutput(string recordFullName, IReadOnlyList<string> headers, string excelFileName, string sheetName, string separator)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# StaticDataHeaderGenerator Results");
        sb.AppendLine();
        sb.AppendLine(FormattableString.Invariant($"## {recordFullName}"));
        sb.AppendLine(FormattableString.Invariant($"- Excel File: `{excelFileName}.xlsx`"));
        sb.AppendLine(FormattableString.Invariant($"- Sheet Name: `{sheetName}`"));
        sb.AppendLine();

        sb.AppendLine("### Headers (List)");
        foreach (var header in headers)
        {
            sb.AppendLine(FormattableString.Invariant($"- {header}"));
        }

        sb.AppendLine();

        sb.AppendLine(HeaderSectionTitle.Resolve(separator));
        sb.AppendLine("```");
        sb.AppendLine(string.Join(separator, headers));
        sb.AppendLine("```");

        return sb.ToString();
    }
}
