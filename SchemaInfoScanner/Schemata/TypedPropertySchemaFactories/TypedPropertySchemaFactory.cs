using System.Globalization;
using Microsoft.CodeAnalysis;
using SchemaInfoScanner.Extensions;
using SchemaInfoScanner.NameObjects;
using SchemaInfoScanner.Resources;
using SchemaInfoScanner.Schemata.TypedPropertySchemaFactories.CollectionTypes;
using SchemaInfoScanner.Schemata.TypedPropertySchemaFactories.PrimitiveTypes;
using SchemaInfoScanner.Schemata.TypedPropertySchemaFactories.RecordTypes;
using SchemaInfoScanner.TypeCheckers;
using Sdp.Attributes;

namespace SchemaInfoScanner.Schemata.TypedPropertySchemaFactories;

public static class TypedPropertySchemaFactory
{
    public static PropertySchemaBase Create(
        PropertyName propertyName,
        INamedTypeSymbol propertySymbol,
        IReadOnlyList<AttributeInfo> attributeList,
        INamedTypeSymbol parentRecordSymbol,
        SchemaFactoryContext context)
    {
        MapTypeChecker.CheckNonNullableKeyAndValue(propertyName, propertySymbol);

        if (PrimitiveTypeChecker.IsSupportedPrimitiveType(propertySymbol))
        {
            return PrimitivePropertySchemaFactory.Create(propertyName, propertySymbol, attributeList);
        }
        else if (ArrayTypeChecker.IsPrimitiveArrayType(propertySymbol))
        {
            var isSingleColumnCollection = AttributeAccessors.HasAttribute<SingleColumnCollectionAttribute>(attributeList);

            return isSingleColumnCollection
                ? PrimitiveArrayPropertySchemaFactory.CreateForSingleColumn(propertyName, propertySymbol, attributeList)
                : PrimitiveArrayPropertySchemaFactory.Create(propertyName, propertySymbol, attributeList);
        }
        else if (SetTypeChecker.IsPrimitiveSetType(propertySymbol))
        {
            var isSingleColumnCollection = AttributeAccessors.HasAttribute<SingleColumnCollectionAttribute>(attributeList);

            return isSingleColumnCollection
                ? PrimitiveSetPropertySchemaFactory.CreateForSingleColumn(propertyName, propertySymbol, attributeList)
                : PrimitiveSetPropertySchemaFactory.Create(propertyName, propertySymbol, attributeList);
        }
        else if (RecordTypeChecker.IsSupportedRecordType(propertySymbol))
        {
            return RecordPropertySchemaFactory.Create(
                propertyName,
                propertySymbol,
                attributeList,
                parentRecordSymbol,
                context);
        }
        else if (RecordTypeChecker.TryFindNestedRecordTypeSymbol(parentRecordSymbol, propertySymbol, out var nestedRecordSymbol))
        {
            return RecordPropertySchemaFactory.Create(
                propertyName,
                nestedRecordSymbol,
                attributeList,
                parentRecordSymbol,
                context);
        }
        else if (ArrayTypeChecker.IsSupportedArrayType(propertySymbol))
        {
            return RecordArrayPropertySchemaFactory.Create(
                propertyName,
                propertySymbol,
                attributeList,
                parentRecordSymbol,
                context);
        }
        else if (SetTypeChecker.IsSupportedSetType(propertySymbol))
        {
            return RecordSetPropertySchemaFactory.Create(
                propertyName,
                propertySymbol,
                attributeList,
                parentRecordSymbol,
                context);
        }
        else if (MapTypeChecker.IsPrimitiveKeyRecordValueMapType(propertySymbol))
        {
            return PrimitiveKeyRecordValueMapPropertySchemaFactory.Create(
                propertyName,
                propertySymbol,
                attributeList,
                parentRecordSymbol,
                context);
        }
        else if (MapTypeChecker.IsRecordKeyAndValueMapType(propertySymbol))
        {
            return RecordKeyAndValueMapPropertySchemaFactory.Create(
                propertyName,
                propertySymbol,
                attributeList,
                parentRecordSymbol,
                context);
        }

        throw new NotSupportedException(string.Format(
            CultureInfo.CurrentCulture,
            Messages.Composite.NotSupportedPropertyType,
            propertyName,
            propertySymbol.Name));
    }
}
