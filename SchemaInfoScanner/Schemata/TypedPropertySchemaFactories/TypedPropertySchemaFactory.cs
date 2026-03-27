using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SchemaInfoScanner.Extensions;
using SchemaInfoScanner.NameObjects;
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
        IReadOnlyList<AttributeSyntax> attributeList,
        INamedTypeSymbol parentRecordSymbol)
    {
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
                parentRecordSymbol);
        }
        else if (RecordTypeChecker.TryFindNestedRecordTypeSymbol(parentRecordSymbol, propertySymbol, out var nestedRecordSymbol))
        {
            return RecordPropertySchemaFactory.Create(
                propertyName,
                nestedRecordSymbol,
                attributeList,
                parentRecordSymbol);
        }
        else if (ArrayTypeChecker.IsSupportedArrayType(propertySymbol))
        {
            return RecordArrayPropertySchemaFactory.Create(
                propertyName,
                propertySymbol,
                attributeList,
                parentRecordSymbol);
        }
        else if (SetTypeChecker.IsSupportedSetType(propertySymbol))
        {
            return RecordSetPropertySchemaFactory.Create(
                propertyName,
                propertySymbol,
                attributeList,
                parentRecordSymbol);
        }
        else if (MapTypeChecker.IsPrimitiveKeyRecordValueMapType(propertySymbol))
        {
            // 여기서 propertyName으로 찾아온 attributeList를 교체해 줘야 한다
            return PrimitiveKeyRecordValueMapPropertySchemaFactory.Create(
                propertyName,
                propertySymbol,
                attributeList,
                parentRecordSymbol);
        }
        else if (MapTypeChecker.IsRecordKeyAndValueMapType(propertySymbol))
        {
            return RecordKeyAndValueMapPropertySchemaFactory.Create(
                propertyName,
                propertySymbol,
                attributeList,
                parentRecordSymbol);
        }

        throw new NotSupportedException($"{propertyName}({propertySymbol.Name}) is not a supported property type.");
    }
}
