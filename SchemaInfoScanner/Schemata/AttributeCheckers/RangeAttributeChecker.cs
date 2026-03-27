using System.Globalization;
using SchemaInfoScanner.Extensions;
using Sdp.Attributes;

namespace SchemaInfoScanner.Schemata.AttributeCheckers;

public static class RangeAttributeChecker
{
    public static void Check<T>(PropertySchemaBase propertySchema, T value)
        where T : IComparable<T>
    {
        if (typeof(T).IsEnum)
        {
            throw new InvalidOperationException("RangeAttribute cannot be used in enum.");
        }

        var attributeValues = propertySchema.GetAttributeValueList<RangeAttribute>();
        if (!attributeValues.Any())
        {
            return;
        }

        if (attributeValues.Count != 2)
        {
            throw new InvalidOperationException("RangeAttribute must have two values.");
        }

        var min = (T)Convert.ChangeType(attributeValues[0], typeof(T), CultureInfo.InvariantCulture);
        var max = (T)Convert.ChangeType(attributeValues[1], typeof(T), CultureInfo.InvariantCulture);

        if (value.CompareTo(min) < 0 || value.CompareTo(max) > 0)
        {
            throw new ArgumentOutOfRangeException(propertySchema.PropertyName.FullName, value, $"Value({value}) must be between {min} and {max}.");
        }
    }
}
