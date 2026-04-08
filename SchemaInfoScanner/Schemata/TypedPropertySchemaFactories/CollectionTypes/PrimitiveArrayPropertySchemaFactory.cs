using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SchemaInfoScanner.Extensions;
using SchemaInfoScanner.NameObjects;
using SchemaInfoScanner.Resources;
using SchemaInfoScanner.Schemata.TypedPropertySchemaFactories.PrimitiveTypes;
using SchemaInfoScanner.Schemata.TypedPropertySchemata.CollectionTypes;
using SchemaInfoScanner.Schemata.TypedPropertySchemata.CollectionTypes.NullableTypes;
using SchemaInfoScanner.TypeCheckers;
using Sdp.Attributes;

namespace SchemaInfoScanner.Schemata.TypedPropertySchemaFactories.CollectionTypes;

public static class PrimitiveArrayPropertySchemaFactory
{
    public static PropertySchemaBase Create(
        PropertyName propertyName,
        INamedTypeSymbol propertySymbol,
        IReadOnlyList<AttributeSyntax> attributeList)
    {
        if (!ArrayTypeChecker.IsPrimitiveArrayType(propertySymbol))
        {
            throw new NotSupportedException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.NotSupportedListType,
                propertyName,
                propertySymbol.Name));
        }

        var typeArgumentSymbol = (INamedTypeSymbol)propertySymbol.TypeArguments.Single();
        var isNullable = typeArgumentSymbol.OriginalDefinition.SpecialType is SpecialType.System_Nullable_T;

        var nestedSchema = PrimitivePropertySchemaFactory.Create(
            propertyName,
            typeArgumentSymbol,
            attributeList);

        PrimitiveTypeChecker.Check(nestedSchema);

        var genericArgumentSchema = new PrimitiveTypeGenericArgumentSchema(
            PrimitiveTypeGenericArgumentSchema.CollectionKind.Array,
            nestedSchema);

        return isNullable
            ? new NullablePrimitiveArrayPropertySchema(
                genericArgumentSchema,
                propertySymbol,
                attributeList)
            : new PrimitiveArrayPropertySchema(
                genericArgumentSchema,
                propertySymbol,
                attributeList);
    }

    public static PropertySchemaBase CreateForSingleColumn(
        PropertyName propertyName,
        INamedTypeSymbol propertySymbol,
        IReadOnlyList<AttributeSyntax> attributeList)
    {
        if (!ArrayTypeChecker.IsPrimitiveArrayType(propertySymbol))
        {
            throw new NotSupportedException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.NotSupportedListType,
                propertyName,
                propertySymbol.Name));
        }

        if (!AttributeAccessors.TryGetAttributeValue<SingleColumnCollectionAttribute, string>(
            attributeList,
            out var separator))
        {
            throw new InvalidOperationException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.NotSingleColumnList,
                propertyName,
                propertySymbol.Name));
        }

        var typeArgumentSymbol = (INamedTypeSymbol)propertySymbol.TypeArguments.Single();
        var isNullable = typeArgumentSymbol.OriginalDefinition.SpecialType is SpecialType.System_Nullable_T;

        var nestedSchema = PrimitivePropertySchemaFactory.Create(
            propertyName,
            typeArgumentSymbol,
            attributeList);

        PrimitiveTypeChecker.Check(nestedSchema);

        var genericArgumentSchema = new PrimitiveTypeGenericArgumentSchema(
            PrimitiveTypeGenericArgumentSchema.CollectionKind.Array,
            nestedSchema);

        return isNullable
            ? new SingleColumnNullablePrimitiveArrayPropertySchema(
                genericArgumentSchema,
                propertySymbol,
                attributeList,
                separator)
            : new SingleColumnPrimitiveArrayPropertySchema(
                genericArgumentSchema,
                propertySymbol,
                attributeList,
                separator);
    }
}
