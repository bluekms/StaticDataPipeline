using System.Globalization;
using Microsoft.CodeAnalysis;
using SchemaInfoScanner.Catalogs;
using SchemaInfoScanner.Resources;
using SchemaInfoScanner.Schemata;
using SchemaInfoScanner.TypeCheckers;

namespace SchemaInfoScanner.Extensions;

public static class ParameterSchemaInnerSchemaFinder
{
    public static RecordSchema FindInnerRecordSchema(
        this PropertySchemaBase property,
        RecordSchemaCatalog recordSchemaCatalog)
    {
        var typeArgument = GetTypeArgument(property);
        var typeArgumentSchema = recordSchemaCatalog.TryFind(typeArgument);
        if (typeArgumentSchema is null)
        {
            var innerException = new KeyNotFoundException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.RecordSchemaTypeArgNotFound,
                typeArgument.Name));
            var msg = string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.NotSupportedType,
                property.PropertyName.FullName);
            throw new NotSupportedException(msg, innerException);
        }

        return typeArgumentSchema;
    }

    private static INamedTypeSymbol GetTypeArgument(PropertySchemaBase property)
    {
        if (ArrayTypeChecker.IsSupportedArrayType(property.NamedTypeSymbol) ||
            SetTypeChecker.IsSupportedSetType(property.NamedTypeSymbol))
        {
            return (INamedTypeSymbol)property.NamedTypeSymbol.TypeArguments.Single();
        }
        else if (MapTypeChecker.IsSupportedMapType(property.NamedTypeSymbol))
        {
            return (INamedTypeSymbol)property.NamedTypeSymbol.TypeArguments.Last();
        }

        throw new InvalidOperationException(string.Format(
            CultureInfo.CurrentCulture,
            Messages.Composite.ExpectedRecordCollectionType,
            property.PropertyName.FullName));
    }
}
