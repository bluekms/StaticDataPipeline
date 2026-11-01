using System.Globalization;
using Microsoft.CodeAnalysis;
using SchemaInfoScanner.Extensions;
using SchemaInfoScanner.NameObjects;
using SchemaInfoScanner.Resources;
using SchemaInfoScanner.Schemata.TypedPropertySchemaFactories.PrimitiveTypes;
using SchemaInfoScanner.Schemata.TypedPropertySchemata.CollectionTypes;
using SchemaInfoScanner.Schemata.TypedPropertySchemata.CollectionTypes.NullableTypes;
using SchemaInfoScanner.TypeCheckers;
using Sdp.Attributes;

namespace SchemaInfoScanner.Schemata.TypedPropertySchemaFactories.CollectionTypes;

public static class PrimitiveSetPropertySchemaFactory
{
    public static PropertySchemaBase Create(
        PropertyName propertyName,
        INamedTypeSymbol propertySymbol,
        IReadOnlyList<AttributeInfo> attributeList)
    {
        if (!SetTypeChecker.IsPrimitiveSetType(propertySymbol))
        {
            throw new NotSupportedException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.NotSupportedHashSetType,
                propertyName,
                propertySymbol.Name));
        }

        var typeArgumentSymbol = (INamedTypeSymbol)propertySymbol.TypeArguments.Single();
        var isNullable = PrimitiveTypeChecker.IsNullablePrimitiveType(typeArgumentSymbol);

        var nestedSchema = PrimitivePropertySchemaFactory.Create(
            propertyName,
            typeArgumentSymbol,
            attributeList);

        PrimitiveTypeChecker.Check(nestedSchema);

        var genericArgumentSchema = new PrimitiveTypeGenericArgumentSchema(
            PrimitiveTypeGenericArgumentSchema.CollectionKind.HashSet,
            nestedSchema);

        return isNullable
            ? new NullablePrimitiveSetPropertySchema(
                genericArgumentSchema,
                propertySymbol,
                attributeList)
            : new PrimitiveSetPropertySchema(
                genericArgumentSchema,
                propertySymbol,
                attributeList);
    }

    public static PropertySchemaBase CreateForSingleColumn(
        PropertyName propertyName,
        INamedTypeSymbol propertySymbol,
        IReadOnlyList<AttributeInfo> attributeList)
    {
        if (!SetTypeChecker.IsPrimitiveSetType(propertySymbol))
        {
            throw new NotSupportedException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.NotSupportedHashSetType,
                propertyName,
                propertySymbol.Name));
        }

        var hasSeparator = AttributeAccessors.TryGetAttributeValue<SingleColumnCollectionAttribute, string>(attributeList, out var separator);
        if (!hasSeparator)
        {
            throw new InvalidOperationException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.AttributeArgumentMustNotBeNull,
                "SingleColumnCollection",
                propertyName));
        }

        var typeArgumentSymbol = (INamedTypeSymbol)propertySymbol.TypeArguments.Single();
        var isNullable = PrimitiveTypeChecker.IsNullablePrimitiveType(typeArgumentSymbol);

        var nestedSchema = PrimitivePropertySchemaFactory.Create(
            propertyName,
            typeArgumentSymbol,
            attributeList);

        PrimitiveTypeChecker.Check(nestedSchema);

        var genericArgumentSchema = new PrimitiveTypeGenericArgumentSchema(
            PrimitiveTypeGenericArgumentSchema.CollectionKind.HashSet,
            nestedSchema);

        return isNullable
            ? new SingleColumnNullablePrimitiveSetPropertySchema(
                genericArgumentSchema,
                propertySymbol,
                attributeList,
                separator)
            : new SingleColumnPrimitiveSetPropertySchema(
                genericArgumentSchema,
                propertySymbol,
                attributeList,
                separator);
    }
}
