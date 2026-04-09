using System.Globalization;
using SchemaInfoScanner.Extensions;
using SchemaInfoScanner.Resources;
using Sdp.Attributes;

namespace SchemaInfoScanner.Schemata.AttributeCheckers;

public static class NullStringAttributeChecker
{
    public sealed record Result(bool IsNull);

    public static Result Check(PropertySchemaBase propertySchema, string argument)
    {
        if (!propertySchema.TryGetAttributeValue<NullStringAttribute, string>(0, out var attributeValue))
        {
            throw new InvalidOperationException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.NullStringAttributeNotFound,
                propertySchema.PropertyName.FullName));
        }

        return new(argument == attributeValue);
    }
}
