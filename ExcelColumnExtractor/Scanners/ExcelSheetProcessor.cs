using System.Text;
using System.Text.RegularExpressions;
using ExcelColumnExtractor.NameObjects;
using ExcelDataReader;
using Microsoft.Extensions.Logging;
using SchemaInfoScanner.Exceptions;
using SchemaInfoScanner.Schemata.TypedPropertySchemata;

namespace ExcelColumnExtractor.Scanners;

public sealed class ExcelSheetProcessor
{
    public delegate void ProcessHeader(SheetHeader header);
    public delegate void ProcessBodyRow(SheetBodyRow row);

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

    public void Process(ExcelSheetName excelSheetName, ILogger logger)
    {
        using var loader = new LockedFileStreamOpener(excelSheetName.ExcelPath);
        if (loader.IsTemp)
        {
            var lastWriteTime = File.GetLastWriteTime(excelSheetName.ExcelPath);
            LogInformation(logger, $"{nameof(SheetHeaderScanner)}: {Path.GetFileName(excelSheetName.ExcelPath)} 이미 열려있어 사본을 읽습니다. 마지막으로 저장된 시간: {lastWriteTime}", null);
        }

        ProcessCore(loader.Stream, excelSheetName, logger);
    }

    public async Task ProcessAsync(ExcelSheetName excelSheetName, ILogger logger, CancellationToken cancellationToken = default)
    {
        using var loader = await LockedFileStreamOpener.CreateAsync(excelSheetName.ExcelPath, cancellationToken);
        if (loader.IsTemp)
        {
            var lastWriteTime = File.GetLastWriteTime(excelSheetName.ExcelPath);
            LogInformation(logger, $"{nameof(SheetHeaderScanner)}: {Path.GetFileName(excelSheetName.ExcelPath)} 이미 열려있어 사본을 읽습니다. 마지막으로 저장된 시간: {lastWriteTime}", null);
        }

        ProcessCore(loader.Stream, excelSheetName, logger);
    }

    private void ProcessCore(Stream stream, ExcelSheetName excelSheetName, ILogger logger)
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
            throw new ArgumentException($"{excelSheetName.FullName} 을 찾을 수 없습니다.");
        }

        if (!reader.Read())
        {
            throw new EndOfStreamException($"{excelSheetName.FullName} 예상치 못한 Sheet의 끝입니다.");
        }

        var excelPhysicalRow = 1;

        var startCell = reader.GetValue(0)?.ToString();
        if (startCell is null || !IsValidCellAddress(startCell))
        {
            throw new InvalidUsageException($"A1 셀에는 반드시 첫 번째 헤더의 Cell Address가 있어야 합니다. ex) B10");
        }

        var (startColumn, startRow) = ParseCellAddress(startCell);
        while (excelPhysicalRow < startRow)
        {
            if (!reader.Read())
            {
                throw new EndOfStreamException($"{excelSheetName.FullName} 예상치 못한 Sheet의 끝입니다. A1: {startCell}");
            }

            excelPhysicalRow++;
        }

        if (processHeader is not null)
        {
            var cells = new object[reader.FieldCount];
            reader.GetValues(cells);

            var headerCells = cells
                .Skip(startColumn - 1)
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
                .Skip(startColumn - 1)
                .Select((x, offset) =>
                {
                    var column = startColumn + offset;
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
            DateTime dt when dt.TimeOfDay == TimeSpan.Zero => dt.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty,
        };
    }

    private static bool IsValidCellAddress(string cellAddress)
    {
        var regex = new Regex(@"^[A-Z]+[0-9]+$", RegexOptions.IgnoreCase);
        return regex.IsMatch(cellAddress);
    }

    private static (int Column, int Row) ParseCellAddress(string cellAddress)
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

        return (column, row);
    }

    private static readonly Action<ILogger, string, Exception?> LogInformation =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(0, nameof(LogInformation)), "{Message}");
}
