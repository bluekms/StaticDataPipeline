using SchemaInfoScanner.Extensions;
using Sdp.Attributes;

namespace SchemaInfoScanner.Schemata.AttributeCheckers;

public static class NullStringAttributeChecker
{
    public sealed record Result(bool IsNull);

    public static Result Check(PropertySchemaBase propertySchema, string argument)
    {
        if (!propertySchema.TryGetAttributeValue<NullStringAttribute, string>(0, out var attributeValue))
        {
            throw new InvalidOperationException($"Property {propertySchema.PropertyName.FullName} does not have a NullStringAttribute.");
        }

        return new(argument == attributeValue);
    }
}
