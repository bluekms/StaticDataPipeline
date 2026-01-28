using System.Text.RegularExpressions;
using Eds.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using SchemaInfoScanner.Catalogs;
using SchemaInfoScanner.Extensions;
using SchemaInfoScanner.Schemata;
using SchemaInfoScanner.TypeCheckers;

namespace SchemaInfoScanner;

public static class RecordFlattener
{
    public static IReadOnlyList<string> Flatten(
        RecordSchema recordSchema,
        RecordSchemaCatalog recordSchemaCatalog,
        ILogger logger)
    {
        return OnFlatten(
            recordSchema,
            recordSchemaCatalog,
            string.Empty,
            logger);
    }

    private static List<string> OnFlatten(
        RecordSchema recordSchema,
        RecordSchemaCatalog recordSchemaCatalog,
        string parentPrefix,
        ILogger logger)
    {
        var headers = new List<string>();

        foreach (var parameter in recordSchema.PropertySchemata)
        {
            var name = parameter.TryGetAttributeValue<ColumnNameAttribute, string>(0, out var columnName)
                ? columnName
                : parameter.PropertyName.Name;

            var headerName = string.IsNullOrEmpty(parentPrefix)
                ? name
                : $"{parentPrefix}.{name}";

            if (PrimitiveTypeChecker.IsSupportedPrimitiveType(parameter.NamedTypeSymbol))
            {
                headers.Add(headerName);
            }
            else if (CollectionTypeChecker.IsPrimitiveCollection(parameter.NamedTypeSymbol))
            {
                if (parameter.HasAttribute<SingleColumnCollectionAttribute>())
                {
                    headers.Add(headerName);
                }
                else
                {
                    if (!parameter.TryGetAttributeValue<LengthAttribute, int>(out var length))
                    {
                        throw new InvalidOperationException($"Parameter {parameter.PropertyName} cannot have LengthAttribute");
                    }

                    for (var i = 0; i < length; ++i)
                    {
                        headers.Add(FormattableString.Invariant($"{headerName}[{i}]"));
                    }
                }
            }
            else if (MapTypeChecker.IsSupportedMapType(parameter.NamedTypeSymbol))
            {
                var typeArgument = (INamedTypeSymbol)parameter.NamedTypeSymbol.TypeArguments.Last();
                var innerRecordSchema = recordSchemaCatalog.Find(typeArgument);

                if (!parameter.TryGetAttributeValue<LengthAttribute, int>(out var length))
                {
                    throw new InvalidOperationException($"Parameter {parameter.PropertyName} cannot have LengthAttribute");
                }

                for (var i = 0; i < length; ++i)
                {
                    var innerFlattenResult = OnFlatten(
                        innerRecordSchema,
                        recordSchemaCatalog,
                        $"{headerName}[{i}]",
                        logger);

                    headers.AddRange(innerFlattenResult);
                }
            }
            else if (CollectionTypeChecker.IsSupportedCollectionType(parameter.NamedTypeSymbol))
            {
                var typeArgument = (INamedTypeSymbol)parameter.NamedTypeSymbol.TypeArguments.Single();
                var innerRecordSchema = recordSchemaCatalog.Find(typeArgument);

                if (!parameter.TryGetAttributeValue<LengthAttribute, int>(out var length))
                {
                    throw new InvalidOperationException($"Parameter {parameter.PropertyName} cannot have LengthAttribute");
                }

                for (var i = 0; i < length; ++i)
                {
                    var innerFlattenResult = OnFlatten(
                        innerRecordSchema,
                        recordSchemaCatalog,
                        $"{headerName}[{i}]",
                        logger);

                    headers.AddRange(innerFlattenResult);
                }
            }
            else
            {
                var innerRecordSchema = recordSchemaCatalog.Find(parameter.NamedTypeSymbol);

                // single parameter record
                if (innerRecordSchema.PropertySchemata.Count == 1 &&
                    PrimitiveTypeChecker.IsSupportedPrimitiveType(innerRecordSchema.PropertySchemata[0].NamedTypeSymbol))
                {
                    headers.Add(headerName);
                }
                else
                {
                    var innerFlatten = OnFlatten(
                        innerRecordSchema,
                        recordSchemaCatalog,
                        headerName,
                        logger);

                    headers.AddRange(innerFlatten);
                }
            }
        }

        return headers;
    }

    private static readonly Regex IndexRegex = new(@"\[.*?\]");

    private static readonly Action<ILogger, string, string, Exception?> LogInformation =
        LoggerMessage.Define<string, string>(
            LogLevel.Information, new EventId(0, nameof(RecordFlattener)), "{Message} {Argument}");
}
