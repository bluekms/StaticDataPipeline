using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using ExcelColumnExtractor.NameObjects;
using ExcelColumnExtractor.Resources;
using SchemaInfoScanner.Extensions;
using SchemaInfoScanner.Schemata;
using Sdp.Attributes;

namespace ExcelColumnExtractor.Mappings;

public sealed class ExcelSheetNameMap(IReadOnlyDictionary<string, ExcelSheetName> sheetNames)
{
    public int Count => sheetNames.Count;

    public ExcelSheetName Get(RecordSchema rawRecordSchema)
    {
        var values = rawRecordSchema.GetAttributeValueList<StaticDataRecordAttribute>();
        var workbookName = values[0];
        var sheetName = values[1];
        var excelSheetNameString = $"{workbookName}.{sheetName}";

        var found = sheetNames.TryGetValue(excelSheetNameString, out var excelSheetName);
        if (found)
        {
            return excelSheetName!;
        }

        var sheetsInWorkbook = sheetNames.Values
            .Where(x => x.WorkbookName == workbookName)
            .Select(x => x.SheetName)
            .ToList();
        if (sheetsInWorkbook.Count == 0)
        {
            throw new ArgumentException(DescribeMissingWorkbook(workbookName));
        }

        throw new ArgumentException(string.Format(
            CultureInfo.CurrentCulture,
            Messages.Composite.SheetNotFoundInWorkbook,
            sheetName,
            workbookName,
            string.Join(", ", sheetsInWorkbook)));
    }

    internal string? DescribeMissingWorkbook(string workbookName)
    {
        var hasWorkbook = sheetNames.Values.Any(x => x.WorkbookName == workbookName);
        if (hasWorkbook)
        {
            return null;
        }

        var sheetInWorkbookWithDifferentCase = sheetNames.Values
            .FirstOrDefault(x => string.Equals(x.WorkbookName, workbookName, StringComparison.OrdinalIgnoreCase));
        if (sheetInWorkbookWithDifferentCase is not null)
        {
            return string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.WorkbookNameCaseMismatch,
                workbookName,
                Path.GetFileName(sheetInWorkbookWithDifferentCase.ExcelPath));
        }

        return string.Format(CultureInfo.CurrentCulture, Messages.Composite.WorkbookNotFound, workbookName);
    }

    internal bool TryGet(string excelFileName, string sheetName, [MaybeNullWhen(false)] out ExcelSheetName excelSheetName)
    {
        var excelSheetNameString = $"{excelFileName}.{sheetName}";

        return sheetNames.TryGetValue(excelSheetNameString, out excelSheetName);
    }
}
