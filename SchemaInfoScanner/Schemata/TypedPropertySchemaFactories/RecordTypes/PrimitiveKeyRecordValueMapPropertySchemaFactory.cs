using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SchemaInfoScanner.NameObjects;
using SchemaInfoScanner.Resources;
using SchemaInfoScanner.Schemata.TypedPropertySchemaFactories.PrimitiveTypes;
using SchemaInfoScanner.Schemata.TypedPropertySchemata.CollectionTypes;
using SchemaInfoScanner.Schemata.TypedPropertySchemata.RecordTypes;
using SchemaInfoScanner.TypeCheckers;

namespace SchemaInfoScanner.Schemata.TypedPropertySchemaFactories.RecordTypes;

public static class PrimitiveKeyRecordValueMapPropertySchemaFactory
{
    public static PropertySchemaBase Create(
        PropertyName propertyName,
        INamedTypeSymbol propertySymbol,
        IReadOnlyList<AttributeSyntax> attributeList,
        INamedTypeSymbol parentRecordSymbol)
    {
        var keySymbol = (INamedTypeSymbol)propertySymbol.TypeArguments[0];
        if (!PrimitiveTypeChecker.IsSupportedPrimitiveType(keySymbol))
        {
            throw new NotSupportedException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.FactoryDictionaryKeyMustBePrimitiveType,
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

        var keySchema = new PrimitiveTypeGenericArgumentSchema(
            PrimitiveTypeGenericArgumentSchema.CollectionKind.DictionaryKey,
            PrimitivePropertySchemaFactory.Create(propertyName, keySymbol, attributeList));

        var valueSchema = new RecordTypeGenericArgumentSchema(
            RecordTypeGenericArgumentSchema.CollectionKind.DictionaryValue,
            RecordPropertySchemaFactory.Create(propertyName, valueSymbol, attributeList, parentRecordSymbol));

        return new PrimitiveKeyRecordValueMapSchema(keySchema, valueSchema, propertySymbol, attributeList);
    }
}
