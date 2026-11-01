using System.Globalization;
using ExcelColumnExtractor.Resources;
using Microsoft.Extensions.Logging;
using SchemaInfoScanner.Extensions;
using SchemaInfoScanner.Schemata;
using Sdp.Attributes;

namespace ExcelColumnExtractor.Extensions;

public static class StartCellResolver
{
    public const string DefaultStartCell = "A1";

    private const int StartCellAttributeParameterIndex = 2;

    public static string Resolve(RecordSchema recordSchema, string? cliStartCell, ILogger logger)
    {
        var attrStartCell = FindAttributeStartCell(recordSchema);
        if (attrStartCell is null)
        {
            return cliStartCell ?? DefaultStartCell;
        }

        if (cliStartCell is not null &&
            !string.Equals(attrStartCell.Trim(), cliStartCell.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            var msg = string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.StartCellOverriddenByAttribute,
                recordSchema.RecordName.FullName,
                attrStartCell,
                cliStartCell);
            LogWarning(logger, msg, null);
        }

        return attrStartCell;
    }

    private static string? FindAttributeStartCell(RecordSchema recordSchema)
    {
        if (!recordSchema.TryGetAttributeValue<StaticDataRecordAttribute, string>(
                StartCellAttributeParameterIndex,
                out var attrStartCell))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(attrStartCell))
        {
            throw new ArgumentException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.EmptyStartCellInAttribute,
                recordSchema.RecordName.FullName));
        }

        return attrStartCell;
    }

    private static readonly Action<ILogger, string, Exception?> LogWarning =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(0, nameof(StartCellResolver)), "{Message}");
}
