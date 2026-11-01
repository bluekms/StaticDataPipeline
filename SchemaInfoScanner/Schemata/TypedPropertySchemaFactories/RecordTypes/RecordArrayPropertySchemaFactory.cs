using System.Globalization;
using Microsoft.CodeAnalysis;
using SchemaInfoScanner.NameObjects;
using SchemaInfoScanner.Resources;
using SchemaInfoScanner.Schemata.TypedPropertySchemata.RecordTypes;
using SchemaInfoScanner.TypeCheckers;

namespace SchemaInfoScanner.Schemata.TypedPropertySchemaFactories.RecordTypes;

public static class RecordArrayPropertySchemaFactory
{
    public static PropertySchemaBase Create(
        PropertyName propertyName,
        INamedTypeSymbol propertySymbol,
        IReadOnlyList<AttributeInfo> attributeList,
        INamedTypeSymbol parentRecordSymbol,
        SchemaFactoryContext context)
    {
        if (!ArrayTypeChecker.IsSupportedArrayType(propertySymbol))
        {
            throw new InvalidOperationException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.FactoryNotSupportedArrayType,
                propertyName,
                propertySymbol.Name));
        }

        if (ArrayTypeChecker.IsPrimitiveArrayType(propertySymbol))
        {
            throw new InvalidOperationException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.NotRecordArrayType,
                propertyName,
                propertySymbol.Name));
        }

        var typeArgumentSymbol = (INamedTypeSymbol)propertySymbol.TypeArguments.Single();
        if (CollectionTypeChecker.IsSupportedCollectionType(typeArgumentSymbol))
        {
            throw new NotSupportedException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.NotSupportedNestedCollectionType,
                propertyName,
                typeArgumentSymbol.Name));
        }

        var nestedSchema = RecordPropertySchemaFactory.Create(
            propertyName,
            typeArgumentSymbol,
            attributeList,
            parentRecordSymbol,
            context);

        var genericArgumentSchema = new RecordTypeGenericArgumentSchema(
            RecordTypeGenericArgumentSchema.CollectionKind.Array,
            nestedSchema);

        return new RecordArrayPropertySchema(
            genericArgumentSchema,
            propertySymbol,
            attributeList);
    }
}
