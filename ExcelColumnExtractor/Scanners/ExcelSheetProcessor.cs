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

    private sealed record CellAddress(int Column, int Row);

    private readonly ProcessHeader? processHeader;
    private readonly ProcessBodyRow? processBodyRow;

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

        ProcessCore(opener.Stream, excelSheetName, startCell, startAddress);
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

        ProcessCore(opener.Stream, excelSheetName, startCell, startAddress);
    }

    private void ProcessCore(
        Stream stream,
        ExcelSheetName excelSheetName,
        string startCell,
        CellAddress startAddress)
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
                .Select(x => x?.ToString() ?? string.Empty)
                .ToList();

            processHeader(new(headerCells));
        }

        if (processBodyRow is null)
        {
            return;
        }

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
                    var address = ToExcelColumnName(column) + excelPhysicalRow;
                    var value = FormatCellValue(x);

                    return new CellData(address, value);
                })
                .ToList();

            processBodyRow(new(rowCells));
        }
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
            _ => value.ToString() ?? string.Empty,
        };
    }

    private static CellAddress ParseStartCell(string startCell)
    {
        if (string.IsNullOrEmpty(startCell) || !IsValidCellAddress(startCell))
        {
            throw new ArgumentException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.InvalidStartCellAddress,
                startCell));
        }

        return ParseCellAddress(startCell);
    }

    private static bool IsValidCellAddress(string cellAddress)
    {
        var regex = new Regex(@"^[A-Z]+[0-9]+$", RegexOptions.IgnoreCase);
        return regex.IsMatch(cellAddress);
    }

    private static CellAddress ParseCellAddress(string cellAddress)
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
}
