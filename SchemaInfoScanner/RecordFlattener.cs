using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using SchemaInfoScanner.Catalogs;
using SchemaInfoScanner.Extensions;
using SchemaInfoScanner.Resources;
using SchemaInfoScanner.Schemata;
using SchemaInfoScanner.TypeCheckers;
using Sdp.Attributes;

namespace SchemaInfoScanner;

public static class RecordFlattener
{
    public sealed record FlattenedHeader(string Name, string? DateTimeFormat, string? ExpandedName = null);

    private enum ElementHeaderShape
    {
        Scalar,
        SingleValueRecord,
        Record,
    }

    public static IReadOnlyList<string> Flatten(
        RecordSchema recordSchema,
        RecordSchemaCatalog recordSchemaCatalog,
        ILogger logger,
        IReadOnlyList<string>? sheetHeaders = null)
    {
        return FlattenHeaders(recordSchema, recordSchemaCatalog, logger, sheetHeaders)
            .Select(x => x.Name)
            .ToList();
    }

    public static IReadOnlyList<FlattenedHeader> FlattenHeaders(
        RecordSchema recordSchema,
        RecordSchemaCatalog recordSchemaCatalog,
        ILogger logger,
        IReadOnlyList<string>? sheetHeaders = null)
    {
        return OnFlatten(
            recordSchema,
            recordSchemaCatalog,
            string.Empty,
            logger,
            sheetHeaders);
    }

    private static List<FlattenedHeader> OnFlatten(
        RecordSchema recordSchema,
        RecordSchemaCatalog recordSchemaCatalog,
        string parentPrefix,
        ILogger logger,
        IReadOnlyList<string>? sheetHeaders)
    {
        var headers = new List<FlattenedHeader>();

        foreach (var parameter in recordSchema.PropertySchemata)
        {
            var name = HeaderNameOf(parameter);

            var headerName = string.IsNullOrEmpty(parentPrefix)
                ? name
                : $"{parentPrefix}.{name}";

            var dateTimeFormat = FindDateTimeFormat(parameter);
            if (PrimitiveTypeChecker.IsSupportedPrimitiveType(parameter.NamedTypeSymbol))
            {
                headers.Add(new(headerName, dateTimeFormat));
            }
            else if (CollectionTypeChecker.IsPrimitiveCollection(parameter.NamedTypeSymbol))
            {
                if (parameter.HasAttribute<SingleColumnCollectionAttribute>())
                {
                    headers.Add(new(headerName, dateTimeFormat));
                }
                else
                {
                    var length = ResolveLength(parameter, headerName, sheetHeaders, ElementHeaderShape.Scalar);

                    for (var i = 0; i < length; ++i)
                    {
                        headers.Add(new(FormattableString.Invariant($"{headerName}[{i}]"), dateTimeFormat));
                    }
                }
            }
            else if (MapTypeChecker.IsSupportedMapType(parameter.NamedTypeSymbol))
            {
                var typeArgument = (INamedTypeSymbol)parameter.NamedTypeSymbol.TypeArguments.Last();
                var innerRecordSchema = recordSchemaCatalog.Find(typeArgument);

                if (IsSingleValueRecord(innerRecordSchema))
                {
                    AddSingleValueElementHeaders(headers, parameter, headerName, innerRecordSchema, sheetHeaders);
                }
                else
                {
                    var length = ResolveLength(parameter, headerName, sheetHeaders, ElementHeaderShape.Record);

                    for (var i = 0; i < length; ++i)
                    {
                        var innerFlattenResult = OnFlatten(
                            innerRecordSchema,
                            recordSchemaCatalog,
                            $"{headerName}[{i}]",
                            logger,
                            sheetHeaders);

                        headers.AddRange(innerFlattenResult);
                    }
                }
            }
            else if (CollectionTypeChecker.IsSupportedCollectionType(parameter.NamedTypeSymbol))
            {
                var typeArgument = (INamedTypeSymbol)parameter.NamedTypeSymbol.TypeArguments.Single();
                var innerRecordSchema = recordSchemaCatalog.Find(typeArgument);

                if (IsSingleValueRecord(innerRecordSchema))
                {
                    AddSingleValueElementHeaders(headers, parameter, headerName, innerRecordSchema, sheetHeaders);
                }
                else
                {
                    var length = ResolveLength(parameter, headerName, sheetHeaders, ElementHeaderShape.Record);

                    for (var i = 0; i < length; ++i)
                    {
                        var innerFlattenResult = OnFlatten(
                            innerRecordSchema,
                            recordSchemaCatalog,
                            $"{headerName}[{i}]",
                            logger,
                            sheetHeaders);

                        headers.AddRange(innerFlattenResult);
                    }
                }
            }
            else
            {
                var innerRecordSchema = recordSchemaCatalog.Find(parameter.NamedTypeSymbol);

                if (IsSingleValueRecord(innerRecordSchema))
                {
                    var innerParameter = innerRecordSchema.PropertySchemata[0];
                    headers.Add(new(headerName, FindDateTimeFormat(innerParameter), $"{headerName}.{HeaderNameOf(innerParameter)}"));
                }
                else
                {
                    var innerFlatten = OnFlatten(
                        innerRecordSchema,
                        recordSchemaCatalog,
                        headerName,
                        logger,
                        sheetHeaders);

                    headers.AddRange(innerFlatten);
                }
            }
        }

        return headers;
    }

    private static void AddSingleValueElementHeaders(
        List<FlattenedHeader> headers,
        PropertySchemaBase parameter,
        string headerName,
        RecordSchema innerRecordSchema,
        IReadOnlyList<string>? sheetHeaders)
    {
        var innerParameter = innerRecordSchema.PropertySchemata[0];
        var innerName = HeaderNameOf(innerParameter);
        var length = ResolveLength(parameter, headerName, sheetHeaders, ElementHeaderShape.SingleValueRecord, innerName);
        var elementDateTimeFormat = FindDateTimeFormat(innerParameter);

        for (var i = 0; i < length; ++i)
        {
            var elementHeader = FormattableString.Invariant($"{headerName}[{i}]");
            headers.Add(new(elementHeader, elementDateTimeFormat, $"{elementHeader}.{innerName}"));
        }
    }

    private static string HeaderNameOf(PropertySchemaBase parameter)
    {
        return parameter.TryGetAttributeValue<ColumnNameAttribute, string>(0, out var columnName)
            ? columnName
            : parameter.PropertyName.Name;
    }

    private static bool IsSingleValueRecord(RecordSchema recordSchema)
    {
        return recordSchema.PropertySchemata.Count == 1 &&
               PrimitiveTypeChecker.IsSupportedPrimitiveType(recordSchema.PropertySchemata[0].NamedTypeSymbol);
    }

    private static string? FindDateTimeFormat(PropertySchemaBase parameter)
    {
        return parameter.TryGetAttributeValue<DateTimeFormatAttribute, string>(out var format)
            ? format
            : null;
    }

    private static int ResolveLength(
        PropertySchemaBase parameter,
        string headerName,
        IReadOnlyList<string>? sheetHeaders,
        ElementHeaderShape elementHeaderShape,
        string? singleValueInnerName = null)
    {
        var hasLength = parameter.TryGetAttributeValue<LengthAttribute, int>(out var length);
        if (hasLength)
        {
            return length;
        }

        if (sheetHeaders is null)
        {
            return 1;
        }

        var count = 0;
        while (true)
        {
            var indexedHeader = FormattableString.Invariant($"{headerName}[{count}]");
            var present = IsIndexedElementPresent(sheetHeaders, indexedHeader, elementHeaderShape, singleValueInnerName);
            if (!present)
            {
                break;
            }

            count++;
        }

        var indexedPrefix = headerName + "[";
        foreach (var header in sheetHeaders)
        {
            if (!header.StartsWith(indexedPrefix, StringComparison.Ordinal))
            {
                continue;
            }

            var close = header.IndexOf(']', indexedPrefix.Length);
            if (close < 0)
            {
                continue;
            }

            var indexText = header.Substring(indexedPrefix.Length, close - indexedPrefix.Length);
            var isIndex = int.TryParse(indexText, NumberStyles.None, CultureInfo.InvariantCulture, out var index);
            if (isIndex && index >= count)
            {
                throw new InvalidOperationException(string.Format(
                    CultureInfo.CurrentCulture,
                    Messages.Composite.IndexedHeadersNotContiguous,
                    headerName,
                    count,
                    header));
            }
        }

        return count;
    }

    private static bool IsIndexedElementPresent(
        IReadOnlyList<string> sheetHeaders,
        string indexedHeader,
        ElementHeaderShape elementHeaderShape,
        string? singleValueInnerName)
    {
        return elementHeaderShape switch
        {
            ElementHeaderShape.Scalar => sheetHeaders.Contains(indexedHeader, StringComparer.Ordinal),
            ElementHeaderShape.SingleValueRecord => sheetHeaders.Contains(indexedHeader, StringComparer.Ordinal) ||
                                                    sheetHeaders.Contains(indexedHeader + "." + singleValueInnerName, StringComparer.Ordinal),
            _ => sheetHeaders.Any(header => header.StartsWith(indexedHeader + ".", StringComparison.Ordinal)),
        };
    }
}
