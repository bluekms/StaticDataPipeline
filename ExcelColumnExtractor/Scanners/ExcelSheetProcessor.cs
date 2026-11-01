using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ExcelColumnExtractor.NameObjects;
using ExcelColumnExtractor.Resources;
using ExcelDataReader;
using Microsoft.Extensions.Logging;
using SchemaInfoScanner.Schemata.TypedPropertySchemata;

namespace ExcelColumnExtractor.Scanners;

public sealed class ExcelSheetProcessor
{
    public delegate void ProcessHeader(SheetHeader header);
    public delegate void ProcessBodyRow(SheetBodyRow row);

    internal sealed record CellAddress(int Column, int Row);

    private static readonly IReadOnlySet<int> NoInspectedColumns = new HashSet<int>();

    private readonly ProcessHeader? processHeader;
    private readonly ProcessBodyRow? processBodyRow;
    private readonly IReadOnlySet<int> inspectedColumns = NoInspectedColumns;

    public ExcelSheetProcessor(
        ProcessHeader processHeader,
        ProcessBodyRow processBodyRow)
    {
        this.processHeader = processHeader;
        this.processBodyRow = processBodyRow;
    }

    public ExcelSheetProcessor(ProcessHeader processHeader)
    {
        this.processHeader = processHeader;
        this.processBodyRow = null;
    }

    public ExcelSheetProcessor(ProcessBodyRow processBodyRow)
    {
        this.processHeader = null;
        this.processBodyRow = processBodyRow;
    }

    public ExcelSheetProcessor(ProcessBodyRow processBodyRow, IReadOnlySet<int> inspectedColumns)
    {
        this.processHeader = null;
        this.processBodyRow = processBodyRow;
        this.inspectedColumns = inspectedColumns;
    }

    public void Process(ExcelSheetName excelSheetName, string startCell, ILogger logger)
    {
        var startAddress = ParseStartCell(startCell);

        using var opener = new LockedFileStreamOpener(excelSheetName.ExcelPath);
        if (opener.IsTemp)
        {
            var lastWriteTime = File.GetLastWriteTime(excelSheetName.ExcelPath);
            var msg = string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.FileAlreadyOpen,
                excelSheetName.FullName,
                lastWriteTime);
            LogInformation(logger, msg, null);
        }

        ProcessCore(opener.Stream, excelSheetName, startCell, startAddress, logger);
    }

    public async Task ProcessAsync(ExcelSheetName excelSheetName, string startCell, ILogger logger, CancellationToken cancellationToken = default)
    {
        var startAddress = ParseStartCell(startCell);

        using var opener = await LockedFileStreamOpener.CreateAsync(excelSheetName.ExcelPath, cancellationToken);
        if (opener.IsTemp)
        {
            var lastWriteTime = File.GetLastWriteTime(excelSheetName.ExcelPath);
            var msg = string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.FileAlreadyOpen,
                excelSheetName.FullName,
                lastWriteTime);
            LogInformation(logger, msg, null);
        }

        ProcessCore(opener.Stream, excelSheetName, startCell, startAddress, logger);
    }

    private void ProcessCore(
        Stream stream,
        ExcelSheetName excelSheetName,
        string startCell,
        CellAddress startAddress,
        ILogger logger)
    {
        using var reader = ExcelReaderFactory.CreateReader(stream);

        var sheetFound = false;
        do
        {
            if (reader.Name == excelSheetName.SheetName)
            {
                sheetFound = true;
                break;
            }
        }
        while (reader.NextResult());

        if (!sheetFound)
        {
            throw new ArgumentException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.SheetNotFound,
                excelSheetName.FullName));
        }

        var excelPhysicalRow = 0;
        while (excelPhysicalRow < startAddress.Row)
        {
            if (!reader.Read())
            {
                throw new EndOfStreamException(string.Format(
                    CultureInfo.CurrentCulture,
                    Messages.Composite.UnexpectedEndOfSheetWithStartCell,
                    excelSheetName.FullName,
                    startCell));
            }

            excelPhysicalRow++;
        }

        if (processHeader is not null)
        {
            var cells = new object[reader.FieldCount];
            reader.GetValues(cells);

            var headerCells = cells
                .Skip(startAddress.Column - 1)
                .Select(FormatCellValue)
                .ToList();

            processHeader(new(headerCells));
        }

        if (processBodyRow is null)
        {
            return;
        }

        var formatWarnedColumns = new HashSet<int>();
        while (reader.Read())
        {
            excelPhysicalRow++;

            var cells = new object[reader.FieldCount];
            reader.GetValues(cells);

            var rowCells = cells
                .Skip(startAddress.Column - 1)
                .Select((x, offset) =>
                {
                    var column = startAddress.Column + offset;
                    var address = ToAddress(column, excelPhysicalRow);
                    var value = FormatCellValue(x);
                    if (!inspectedColumns.Contains(column))
                    {
                        return new CellData(address, value);
                    }

                    WarnIfSurroundedByWhitespace(excelSheetName, address, value, logger);
                    WarnIfNumberFormatted(
                        reader,
                        column - 1,
                        x,
                        excelSheetName,
                        address,
                        value,
                        formatWarnedColumns,
                        logger);

                    return new CellData(address, value);
                })
                .ToList();

            processBodyRow(new(rowCells));
        }
    }

    private static void WarnIfSurroundedByWhitespace(
        ExcelSheetName excelSheetName,
        string address,
        string value,
        ILogger logger)
    {
        if (value.Length == 0 || (!char.IsWhiteSpace(value[0]) && !char.IsWhiteSpace(value[^1])))
        {
            return;
        }

        var msg = string.Format(
            CultureInfo.CurrentCulture,
            Messages.Composite.CellHasSurroundingWhitespace,
            excelSheetName.FullName,
            address,
            value);
        LogWarning(logger, msg, null);
    }

    // 날짜 셀은 서식으로만 날짜임을 알 수 있어서(ExcelDataReader 가 DateTime 으로 돌려줌) 경고 대상에서 뺀다.
    private static void WarnIfNumberFormatted(
        IExcelDataReader reader,
        int fieldIndex,
        object? rawValue,
        ExcelSheetName excelSheetName,
        string address,
        string value,
        HashSet<int> warnedColumns,
        ILogger logger)
    {
        if (rawValue is null or string or DateTime || warnedColumns.Contains(fieldIndex))
        {
            return;
        }

        var numberFormat = reader.GetNumberFormatString(fieldIndex);
        if (string.IsNullOrEmpty(numberFormat) || IsPlainNumberFormat(numberFormat))
        {
            return;
        }

        warnedColumns.Add(fieldIndex);

        var msg = string.Format(
            CultureInfo.CurrentCulture,
            Messages.Composite.ColumnHasNumberFormat,
            excelSheetName.FullName,
            address,
            numberFormat,
            value);
        LogWarning(logger, msg, null);
    }

    private static bool IsPlainNumberFormat(string numberFormat)
    {
        return string.Equals(numberFormat, "General", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(numberFormat, "@", StringComparison.Ordinal);
    }

    private static string ToAddress(int column, int row)
    {
        return ToExcelColumnName(column) + row.ToString(CultureInfo.InvariantCulture);
    }

    private static string ToExcelColumnName(int columnNumber)
    {
        var sb = new StringBuilder();

        while (columnNumber > 0)
        {
            columnNumber--;
            sb.Insert(0, (char)('A' + (columnNumber % 26)));
            columnNumber /= 26;
        }

        return sb.ToString();
    }

    private static string FormatCellValue(object? value)
    {
        return value switch
        {
            null => string.Empty,
            DateTime dt when dt.TimeOfDay == TimeSpan.Zero => dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty,
        };
    }

    internal static CellAddress ParseStartCell(string startCell)
    {
        var normalized = startCell.Trim().ToUpperInvariant();
        if (!IsValidCellAddress(normalized))
        {
            throw new ArgumentException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.InvalidStartCellAddress,
                startCell));
        }

        return ParseCellAddress(normalized);
    }

    private static bool IsValidCellAddress(string cellAddress)
    {
        return CellAddressPattern.IsMatch(cellAddress);
    }

    private static readonly Regex CellAddressPattern = new(@"^[A-Z]+[1-9][0-9]*$", RegexOptions.CultureInvariant);

    internal static CellAddress ParseCellAddress(string cellAddress)
    {
        var row = 0;
        var column = 0;

        foreach (var c in cellAddress)
        {
            if (char.IsDigit(c))
            {
                row = (row * 10) + (c - '0');
            }
            else if (char.IsLetter(c))
            {
                column = (column * 26) + (c - 'A' + 1);
            }
        }

        return new(column, row);
    }

    private static readonly Action<ILogger, string, Exception?> LogInformation =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(0, nameof(LogInformation)), "{Message}");

    private static readonly Action<ILogger, string, Exception?> LogWarning =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(1, nameof(LogWarning)), "{Message}");
}
