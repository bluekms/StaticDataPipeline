using System.Globalization;
using System.IO.Compression;
using System.Xml;
using System.Xml.Linq;
using ExcelColumnExtractor.NameObjects;
using ExcelColumnExtractor.Resources;

namespace ExcelColumnExtractor.Scanners;

// ExcelDataReader 는 수식 셀의 저장값만 주고 수식 자체는 알려 주지 않으므로 xlsx 의 OOXML 을 직접 읽는다.
// Transitional 과 Strict 는 네임스페이스만 다르므로 요소·속성은 LocalName 으로 찾는다.
public static class SheetFormulaScanner
{
    public static IReadOnlyList<string> Scan(
        ExcelSheetName excelSheetName,
        string startCell,
        IReadOnlySet<int> targetColumns)
    {
        var startAddress = ExcelSheetProcessor.ParseStartCell(startCell);

        using var opener = new LockedFileStreamOpener(excelSheetName.ExcelPath);
        using var archive = new ZipArchive(opener.Stream, ZipArchiveMode.Read, leaveOpen: true);

        var sheetPartPath = FindSheetPartPath(archive, excelSheetName);
        var sheetEntry = archive.GetEntry(sheetPartPath);
        if (sheetEntry is null)
        {
            throw SheetNotFound(excelSheetName);
        }

        using var sheetStream = sheetEntry.Open();
        using var reader = XmlReader.Create(sheetStream);

        var formulaCells = new List<string>();
        string? cellReference = null;
        var cellDepth = -1;
        while (reader.Read())
        {
            if (reader.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            if (reader.LocalName == "c")
            {
                cellReference = reader.GetAttribute("r");
                cellDepth = reader.Depth;
                continue;
            }

            if (reader.LocalName != "f" || reader.Depth != cellDepth + 1 || string.IsNullOrEmpty(cellReference))
            {
                continue;
            }

            var address = ExcelSheetProcessor.ParseCellAddress(cellReference.ToUpperInvariant());
            if (address.Row >= startAddress.Row && targetColumns.Contains(address.Column))
            {
                formulaCells.Add(cellReference);
            }

            cellReference = null;
        }

        return formulaCells;
    }

    private static string FindSheetPartPath(ZipArchive archive, ExcelSheetName excelSheetName)
    {
        var workbookXml = LoadXml(archive, "xl/workbook.xml", excelSheetName);
        var sheetElement = workbookXml
            .Descendants()
            .Where(x => x.Name.LocalName == "sheet")
            .FirstOrDefault(x => IsSheetNamed(x, excelSheetName.SheetName));
        if (sheetElement is null)
        {
            throw SheetNotFound(excelSheetName);
        }

        var relationshipId = sheetElement
            .Attributes()
            .Where(x => x.Name.LocalName == "id")
            .Where(x => x.Name.Namespace != XNamespace.None)
            .Select(x => x.Value)
            .FirstOrDefault();
        var relsXml = LoadXml(archive, "xl/_rels/workbook.xml.rels", excelSheetName);
        var target = relsXml
            .Descendants()
            .Where(x => x.Name.LocalName == "Relationship")
            .Where(x => string.Equals((string?)x.Attribute("Id"), relationshipId, StringComparison.Ordinal))
            .Select(x => (string?)x.Attribute("Target"))
            .FirstOrDefault();
        if (target is null)
        {
            throw SheetNotFound(excelSheetName);
        }

        return target.StartsWith('/')
            ? target.TrimStart('/')
            : "xl/" + target;
    }

    private static XDocument LoadXml(ZipArchive archive, string entryName, ExcelSheetName excelSheetName)
    {
        var entry = archive.GetEntry(entryName);
        if (entry is null)
        {
            throw SheetNotFound(excelSheetName);
        }

        using var stream = entry.Open();

        return XDocument.Load(stream);
    }

    private static bool IsSheetNamed(XElement sheetElement, string sheetName)
        => string.Equals((string?)sheetElement.Attribute("name"), sheetName, StringComparison.Ordinal);

    private static ArgumentException SheetNotFound(ExcelSheetName excelSheetName)
        => new(string.Format(
            CultureInfo.CurrentCulture,
            Messages.Composite.SheetNotFound,
            excelSheetName.FullName));
}
