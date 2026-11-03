using System.Globalization;
using Microsoft.CodeAnalysis;
using SchemaInfoScanner.Collectors;
using SchemaInfoScanner.Extensions;
using SchemaInfoScanner.Resources;
using Sdp.Attributes;

namespace SchemaInfoScanner.Schemata.AttributeCheckers;

public static class RangeAttributeChecker
{
    public static void Check<T>(PropertySchemaBase propertySchema, T value)
        where T : IComparable<T>
    {
        var attributeValues = propertySchema.GetAttributeValueList<RangeAttribute>();
        if (attributeValues.Count is 0)
        {
            return;
        }

        var (minRaw, maxRaw) = ExtractMinMax(attributeValues);

        var min = ParseRangeBound<T>(propertySchema, minRaw);
        var max = ParseRangeBound<T>(propertySchema, maxRaw);

        if (CompareValues(value, min) < 0 || CompareValues(value, max) > 0)
        {
            throw new ArgumentOutOfRangeException(propertySchema.PropertyName.FullName, value, string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.ValueOutOfRange,
                value,
                min,
                max));
        }
    }

    public static void CheckEnum(
        PropertySchemaBase propertySchema,
        string memberName,
        INamedTypeSymbol enumType)
    {
        var attributeValues = propertySchema.GetAttributeValueList<RangeAttribute>();
        if (attributeValues.Count is 0)
        {
            return;
        }

        var (minName, maxName) = ExtractMinMax(attributeValues);

        var valueUnderlying = ParseEnumValueOrName(enumType, memberName);
        var minUnderlying = ParseEnumValueOrName(enumType, minName);
        var maxUnderlying = ParseEnumValueOrName(enumType, maxName);

        if (valueUnderlying < minUnderlying || valueUnderlying > maxUnderlying)
        {
            throw new ArgumentOutOfRangeException(propertySchema.PropertyName.FullName, memberName, string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.ValueOutOfRange,
                memberName,
                minName,
                maxName));
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

    private static int CompareValues<T>(T value, T bound)
        where T : IComparable<T>
    {
        if (typeof(T) == typeof(string))
        {
            return string.CompareOrdinal((string)(object)value!, (string)(object)bound!);
        }

        return value.CompareTo(bound);
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

        if (typeof(T) == typeof(DateTimeOffset))
        {
            var format = propertySchema.GetAttributeValue<DateTimeFormatAttribute, string>();

            return (T)(object)DateTimeOffset.ParseExact(raw, format, CultureInfo.InvariantCulture, DateTimeStyles.None);
        }

        if (typeof(T) == typeof(DateOnly))
        {
            var format = propertySchema.GetAttributeValue<DateTimeFormatAttribute, string>();

            return (T)(object)DateOnly.ParseExact(raw, format, CultureInfo.InvariantCulture, DateTimeStyles.None);
        }

        if (typeof(T) == typeof(TimeOnly))
        {
            var format = propertySchema.GetAttributeValue<DateTimeFormatAttribute, string>();

            return (T)(object)TimeOnly.ParseExact(raw, format, CultureInfo.InvariantCulture, DateTimeStyles.None);
        }

        return (T)Convert.ChangeType(raw, typeof(T), CultureInfo.InvariantCulture);
    }

    private static long ParseEnumValueOrName(INamedTypeSymbol enumType, string raw)
    {
        if (long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var asLong))
        {
            return asLong;
        }

        return EnumMemberValueResolver.Resolve(enumType, raw);
    }
}
