using System.Globalization;
using SchemaInfoScanner.Extensions;
using SchemaInfoScanner.Resources;
using Sdp.Attributes;

namespace SchemaInfoScanner.Schemata.AttributeCheckers;

public static class RangeAttributeChecker
{
    public static void Check<T>(PropertySchemaBase propertySchema, T value)
        where T : IComparable<T>
    {
        if (typeof(T).IsEnum)
        {
            throw new InvalidOperationException(Messages.RangeAttributeCannotBeUsedInEnum);
        }

        var attributeValues = propertySchema.GetAttributeValueList<RangeAttribute>();
        if (attributeValues.Count is 0)
        {
            return;
        }

        var (minRaw, maxRaw) = ExtractMinMax(attributeValues);

        var min = ParseRangeBound<T>(propertySchema, minRaw);
        var max = ParseRangeBound<T>(propertySchema, maxRaw);

        if (value.CompareTo(min) < 0 || value.CompareTo(max) > 0)
        {
            throw new ArgumentOutOfRangeException(propertySchema.PropertyName.FullName, value, string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.ValueOutOfRange,
                value,
                min,
                max));
        }
    }

    private static (string Min, string Max) ExtractMinMax(IReadOnlyList<string> attributeValues)
    {
        if (attributeValues.Count is 2)
        {
            return (attributeValues[0], attributeValues[1]);
        }

        if (attributeValues.Count is 3)
        {
            return (attributeValues[1], attributeValues[2]);
        }

        throw new InvalidOperationException(Messages.RangeAttributeMustHaveTwoValues);
    }

    private static T ParseRangeBound<T>(PropertySchemaBase propertySchema, string raw)
    {
        if (typeof(T) == typeof(TimeSpan))
        {
            var format = propertySchema.GetAttributeValue<TimeSpanFormatAttribute, string>();
            return (T)(object)TimeSpan.ParseExact(raw, format, CultureInfo.InvariantCulture);
        }

        if (typeof(T) == typeof(DateTime))
        {
            var format = propertySchema.GetAttributeValue<DateTimeFormatAttribute, string>();
            return (T)(object)DateTime.ParseExact(raw, format, CultureInfo.InvariantCulture, DateTimeStyles.None);
        }

        return (T)Convert.ChangeType(raw, typeof(T), CultureInfo.InvariantCulture);
    }
}
