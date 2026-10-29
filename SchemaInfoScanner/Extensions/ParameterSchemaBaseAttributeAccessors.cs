using System.Diagnostics.CodeAnalysis;
using SchemaInfoScanner.Schemata;

namespace SchemaInfoScanner.Extensions;

public static class ParameterSchemaBaseAttributeAccessors
{
    public static bool HasAttribute<T>(this PropertySchemaBase propertySchema)
        where T : Attribute
    {
        return AttributeAccessors.HasAttribute<T>(propertySchema.AttributeList);
    }

    public static TValue GetAttributeValue<TAttribute, TValue>(
        this PropertySchemaBase propertySchema,
        int attributeParameterIndex = 0)
        where TAttribute : Attribute
    {
        return AttributeAccessors.GetAttributeValue<TAttribute, TValue>(propertySchema.AttributeList, attributeParameterIndex);
    }

    public static bool TryGetAttributeValue<TAttribute, TValue>(
        this PropertySchemaBase propertySchema,
        int attributeParameterIndex,
        [NotNullWhen(true)] out TValue? value)
        where TAttribute : Attribute
    {
        var found = AttributeAccessors.TryGetAttributeValue<TAttribute, TValue>(
            propertySchema.AttributeList,
            out var result,
            attributeParameterIndex);
        value = found ? result : default;

        return found;
    }

    public static IReadOnlyList<string> GetAttributeValueList<TAttribute>(this PropertySchemaBase propertySchema)
        where TAttribute : Attribute
    {
        return AttributeAccessors.GetAttributeValueList<TAttribute>(propertySchema.AttributeList);
    }
}
