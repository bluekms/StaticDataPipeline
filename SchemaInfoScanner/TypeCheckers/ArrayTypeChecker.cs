using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using SchemaInfoScanner.Catalogs;
using SchemaInfoScanner.Extensions;
using SchemaInfoScanner.NameObjects;
using SchemaInfoScanner.Resources;
using SchemaInfoScanner.Schemata;
using Sdp.Attributes;

namespace SchemaInfoScanner.TypeCheckers;

internal static class ArrayTypeChecker
{
    private static readonly HashSet<string> SupportedTypeNames =
    [
        "ImmutableArray<>",
    ];

    public static void Check(
        PropertySchemaBase property,
        RecordSchemaCatalog recordSchemaCatalog,
        HashSet<RecordName> visited,
        ILogger logger)
    {
        if (!IsSupportedArrayType(property.NamedTypeSymbol))
        {
            throw new InvalidOperationException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.ExpectedArrayType,
                property.PropertyName.FullName));
        }

        var typeArgument = (INamedTypeSymbol)property.NamedTypeSymbol.TypeArguments.Single();

        if (PrimitiveTypeChecker.IsSupportedPrimitiveType(typeArgument))
        {
            return;
        }

        if (typeArgument.NullableAnnotation is NullableAnnotation.Annotated)
        {
            throw new NotSupportedException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.NullableRecordArrayNotSupported,
                property.PropertyName.FullName));
        }

        if (property.HasAttribute<SingleColumnCollectionAttribute>())
        {
            throw new NotSupportedException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.SingleColumnArrayOnlyPrimitive,
                property.PropertyName.FullName));
        }

        var innerRecordSchema = property.FindInnerRecordSchema(recordSchemaCatalog);
        RecordTypeChecker.Check(innerRecordSchema, recordSchemaCatalog, visited, logger);
    }

    public static bool IsSupportedArrayType(INamedTypeSymbol symbol)
    {
        if (symbol.OriginalDefinition.SpecialType is SpecialType.System_Nullable_T ||
            symbol.TypeArguments is not [INamedTypeSymbol])
        {
            return false;
        }

        var genericTypeDefinitionName = symbol
            .ConstructUnboundGenericType()
            .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        return SupportedTypeNames.Contains(genericTypeDefinitionName);
    }

    public static bool IsPrimitiveArrayType(INamedTypeSymbol symbol)
    {
        if (!IsSupportedArrayType(symbol))
        {
            return false;
        }

        var typeArgument = (INamedTypeSymbol)symbol.TypeArguments.Single();
        return PrimitiveTypeChecker.IsSupportedPrimitiveType(typeArgument);
    }
}
