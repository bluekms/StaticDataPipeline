using System.Globalization;
using System.IO.Compression;
using System.Xml;
using System.Xml.Linq;
using ExcelColumnExtractor.Resources;

namespace ExcelColumnExtractor.Scanners;

// ExcelDataReader 는 수식 셀의 저장값만 주고 수식 자체는 알려 주지 않으므로 xlsx 의 OOXML 을 직접 읽는다.
// Transitional 과 Strict 는 네임스페이스만 다르므로 요소·속성은 LocalName 으로 찾는다.
public static class SheetFormulaScanner
{
    public sealed record FormulaCell(string Reference, int Row, int Column);

    public static IReadOnlyDictionary<string, IReadOnlyList<FormulaCell>> Scan(string excelPath)
    {
        var workbookName = Path.GetFileNameWithoutExtension(excelPath);

        using var opener = new LockedFileStreamOpener(excelPath);
        using var archive = new ZipArchive(opener.Stream, ZipArchiveMode.Read, leaveOpen: true);

        var workbookXml = LoadXml(archive, "xl/workbook.xml", workbookName);
        var relsXml = LoadXml(archive, "xl/_rels/workbook.xml.rels", workbookName);
        var targetByRelationshipId = ReadRelationshipTargets(relsXml);

        var result = new Dictionary<string, IReadOnlyList<FormulaCell>>(StringComparer.Ordinal);
        var sheetElements = workbookXml
            .Descendants()
            .Where(x => x.Name.LocalName == "sheet");
        foreach (var sheetElement in sheetElements)
        {
            var sheetName = (string?)sheetElement.Attribute("name");
            var relationshipId = FindRelationshipId(sheetElement);
            if (sheetName is null || relationshipId is null)
            {
                continue;
            }

            var target = targetByRelationshipId.GetValueOrDefault(relationshipId);
            if (target is null)
            {
                continue;
            }

            var sheetEntry = archive.GetEntry(ToPartPath(target));
            if (sheetEntry is null)
            {
                continue;
            }

            result[sheetName] = ScanSheet(sheetEntry);
        }

        return result;
    }

    private static List<FormulaCell> ScanSheet(ZipArchiveEntry sheetEntry)
    {
        using var sheetStream = sheetEntry.Open();
        using var reader = XmlReader.Create(sheetStream);

        var formulaCells = new List<FormulaCell>();
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
            formulaCells.Add(new(cellReference, address.Row, address.Column));

            cellReference = null;
        }

        return formulaCells;
    }

    private static Dictionary<string, string> ReadRelationshipTargets(XDocument relsXml)
    {
        var targetByRelationshipId = new Dictionary<string, string>(StringComparer.Ordinal);
        var relationships = relsXml
            .Descendants()
            .Where(x => x.Name.LocalName == "Relationship");
        foreach (var relationship in relationships)
        {
            var id = (string?)relationship.Attribute("Id");
            var target = (string?)relationship.Attribute("Target");
            if (id is null || target is null)
            {
                continue;
            }

            targetByRelationshipId[id] = target;
        }

        return targetByRelationshipId;
    }

    private static string? FindRelationshipId(XElement sheetElement)
    {
        return sheetElement
            .Attributes()
            .Where(x => x.Name.LocalName == "id")
            .Where(x => x.Name.Namespace != XNamespace.None)
            .Select(x => x.Value)
            .FirstOrDefault();
    }

    private static string ToPartPath(string target)
    {
        return target.StartsWith('/')
            ? target.TrimStart('/')
            : "xl/" + target;
    }

    private static XDocument LoadXml(ZipArchive archive, string entryName, string workbookName)
    {
        var entry = archive.GetEntry(entryName);
        if (entry is null)
        {
            throw new ArgumentException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.SheetNotFound,
                workbookName));
        }

        using var stream = entry.Open();

        return XDocument.Load(stream);
    }
}
