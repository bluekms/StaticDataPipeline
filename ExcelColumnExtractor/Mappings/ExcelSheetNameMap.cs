using System.Diagnostics.CodeAnalysis;
using ExcelColumnExtractor.NameObjects;
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
        var excelSheetNameString = $"{values[0]}.{values[1]}";

        return sheetNames[excelSheetNameString];
    }

    internal bool TryGet(string excelFileName, string sheetName, [MaybeNullWhen(false)] out ExcelSheetName excelSheetName)
    {
        var excelSheetNameString = $"{excelFileName}.{sheetName}";

        return sheetNames.TryGetValue(excelSheetNameString, out excelSheetName);
    }
}
