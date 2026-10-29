using System.Globalization;
using Microsoft.CodeAnalysis;
using SchemaInfoScanner.NameObjects;
using SchemaInfoScanner.Resources;
using SchemaInfoScanner.Schemata.TypedPropertySchemata.RecordTypes;
using SchemaInfoScanner.TypeCheckers;

namespace SchemaInfoScanner.Schemata.TypedPropertySchemaFactories.RecordTypes;

public static class RecordKeyAndValueMapPropertySchemaFactory
{
    public static PropertySchemaBase Create(
        PropertyName propertyName,
        INamedTypeSymbol propertySymbol,
        IReadOnlyList<AttributeInfo> attributeList,
        INamedTypeSymbol parentRecordSymbol,
        SchemaFactoryContext context)
    {
        var keySymbol = (INamedTypeSymbol)propertySymbol.TypeArguments[0];
        if (!RecordTypeChecker.IsSupportedRecordType(keySymbol))
        {
            throw new InvalidOperationException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.FactoryDictionaryKeyMustBeRecordType,
                propertyName,
                propertySymbol.Name));
        }

        var valueSymbol = (INamedTypeSymbol)propertySymbol.TypeArguments[1];
        if (!RecordTypeChecker.IsSupportedRecordType(valueSymbol))
        {
            throw new NotSupportedException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.FactoryDictionaryValueMustBeRecordType,
                propertyName,
                propertySymbol.Name));
        }

        var keySchema = new RecordTypeGenericArgumentSchema(
            RecordTypeGenericArgumentSchema.CollectionKind.DictionaryKey,
            RecordPropertySchemaFactory.Create(propertyName, keySymbol, attributeList, parentRecordSymbol, context));

        var valueSchema = new RecordTypeGenericArgumentSchema(
            RecordTypeGenericArgumentSchema.CollectionKind.DictionaryValue,
            RecordPropertySchemaFactory.Create(propertyName, valueSymbol, attributeList, parentRecordSymbol, context));

        return new RecordKeyRecordValueMapSchema(keySchema, valueSchema, propertySymbol, attributeList);
    }
}
