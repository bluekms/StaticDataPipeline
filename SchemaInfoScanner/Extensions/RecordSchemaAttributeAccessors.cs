using System.Diagnostics.CodeAnalysis;
using SchemaInfoScanner.Schemata;

namespace SchemaInfoScanner.Extensions;

public static class RecordSchemaAttributeAccessors
{
    public static bool HasAttribute<T>(this RecordSchema recordSchema)
        where T : Attribute
    {
        return AttributeAccessors.HasAttribute<T>(recordSchema.RecordAttributeList);
    }

    public static TValue GetAttributeValue<TAttribute, TValue>(
        this RecordSchema recordSchema,
        int attributeParameterIndex)
        where TAttribute : Attribute
    {
        return AttributeAccessors.GetAttributeValue<TAttribute, TValue>(recordSchema.RecordAttributeList, attributeParameterIndex);
    }

    public static bool TryGetAttributeValue<TAttribute, TValue>(
        this RecordSchema recordSchema,
        int attributeParameterIndex,
        [NotNullWhen(true)] out TValue? value)
        where TAttribute : Attribute
    {
        var found = AttributeAccessors.TryGetAttributeValue<TAttribute, TValue>(
            recordSchema.RecordAttributeList,
            out var result,
            attributeParameterIndex);
        value = found ? result : default;

        return found;
    }

    public static IReadOnlyList<string> GetAttributeValueList<TAttribute>(this RecordSchema recordSchema)
        where TAttribute : Attribute
    {
        return AttributeAccessors.GetAttributeValueList<TAttribute>(recordSchema.RecordAttributeList);
    }
}
