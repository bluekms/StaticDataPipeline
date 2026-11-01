using System.Globalization;
using Sdp.Resources;

namespace Sdp.Csv;

public static class CsvMapperHelper
{
    public static bool ParseBool(string value, string column)
    {
        var parsed = bool.TryParse(value, out var result);
        if (!parsed)
        {
            throw InvalidCell(value, "bool", column);
        }

        return result;
    }

    public static byte ParseByte(string value, string column)
    {
        var parsed = byte.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result);
        if (!parsed)
        {
            throw InvalidCell(value, "byte", column);
        }

        return result;
    }

    public static sbyte ParseSByte(string value, string column)
    {
        var parsed = sbyte.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result);
        if (!parsed)
        {
            throw InvalidCell(value, "sbyte", column);
        }

        return result;
    }

    public static short ParseInt16(string value, string column)
    {
        var parsed = short.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result);
        if (!parsed)
        {
            throw InvalidCell(value, "short", column);
        }

        return result;
    }

    public static ushort ParseUInt16(string value, string column)
    {
        var parsed = ushort.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result);
        if (!parsed)
        {
            throw InvalidCell(value, "ushort", column);
        }

        return result;
    }

    public static int ParseInt32(string value, string column)
    {
        var parsed = int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result);
        if (!parsed)
        {
            throw InvalidCell(value, "int", column);
        }

        return result;
    }

    public static uint ParseUInt32(string value, string column)
    {
        var parsed = uint.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result);
        if (!parsed)
        {
            throw InvalidCell(value, "uint", column);
        }

        return result;
    }

    public static long ParseInt64(string value, string column)
    {
        var parsed = long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result);
        if (!parsed)
        {
            throw InvalidCell(value, "long", column);
        }

        return result;
    }

    public static ulong ParseUInt64(string value, string column)
    {
        var parsed = ulong.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result);
        if (!parsed)
        {
            throw InvalidCell(value, "ulong", column);
        }

        return result;
    }

    public static float ParseSingle(string value, string column)
    {
        var parsed = float.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var result);
        if (!parsed)
        {
            throw InvalidCell(value, "float", column);
        }

        return result;
    }

    public static double ParseDouble(string value, string column)
    {
        var parsed = double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var result);
        if (!parsed)
        {
            throw InvalidCell(value, "double", column);
        }

        return result;
    }

    public static decimal ParseDecimal(string value, string column)
    {
        var parsed = decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result);
        if (!parsed)
        {
            throw InvalidCell(value, "decimal", column);
        }

        return result;
    }

    public static char ParseChar(string value, string column)
    {
        var parsed = char.TryParse(value, out var result);
        if (!parsed)
        {
            throw InvalidCell(value, "char", column);
        }

        return result;
    }

    public static Guid ParseGuid(string value, string column)
    {
        var parsed = Guid.TryParse(value, out var result);
        if (!parsed)
        {
            throw InvalidCell(value, "Guid", column);
        }

        return result;
    }

    public static DateTime ParseDateTime(string value, string format, string column)
    {
        var parsed = DateTime.TryParseExact(value, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result);
        if (!parsed)
        {
            throw InvalidFormattedCell(value, "DateTime", format, column);
        }

        return result;
    }

    public static DateTimeOffset ParseDateTimeOffset(string value, string format, string column)
    {
        var parsed = DateTimeOffset.TryParseExact(value, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result);
        if (!parsed)
        {
            throw InvalidFormattedCell(value, "DateTimeOffset", format, column);
        }

        return result;
    }

    public static TimeSpan ParseTimeSpan(string value, string format, string column)
    {
        var parsed = TimeSpan.TryParseExact(value, format, CultureInfo.InvariantCulture, out var result);
        if (!parsed)
        {
            throw InvalidFormattedCell(value, "TimeSpan", format, column);
        }

        return result;
    }

#if NET6_0_OR_GREATER
    public static DateOnly ParseDateOnly(string value, string format, string column)
    {
        var parsed = DateOnly.TryParseExact(value, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result);
        if (!parsed)
        {
            throw InvalidFormattedCell(value, "DateOnly", format, column);
        }

        return result;
    }

    public static TimeOnly ParseTimeOnly(string value, string format, string column)
    {
        var parsed = TimeOnly.TryParseExact(value, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result);
        if (!parsed)
        {
            throw InvalidFormattedCell(value, "TimeOnly", format, column);
        }

        return result;
    }
#endif

    public static Exception UndefinedEnumMember(string value, string enumName, string column)
    {
        var message = string.Format(CultureInfo.CurrentCulture, Messages.Composite.CellEnumMemberUndefined, value, enumName, column);

        return new ArgumentException(message, nameof(value));
    }

    public static Exception ValueOutOfRange(string name, string min, string max, object value)
    {
        var message = string.Format(CultureInfo.CurrentCulture, Messages.Composite.MappedValueOutOfRange, name, min, max);

        return new ArgumentOutOfRangeException(nameof(value), value, message);
    }

    public static Exception PatternMismatch(string value, string name)
    {
        var message = string.Format(CultureInfo.CurrentCulture, Messages.Composite.MappedValuePatternMismatch, value, name);

        return new ArgumentException(message, nameof(value));
    }

    public static Exception CountOutOfRange(string name, int min, int max, int actualCount)
    {
        var message = string.Format(CultureInfo.CurrentCulture, Messages.Composite.MappedCountOutOfRange, name, min, max, actualCount);

        return new ArgumentException(message);
    }

    public static Exception DuplicateSetValue(object? value, string name)
    {
        var message = string.Format(CultureInfo.CurrentCulture, Messages.Composite.MappedDuplicateSetValue, Display(value), name);

        return new ArgumentException(message);
    }

    public static Exception DuplicateDictionaryKey(object? key, string name)
    {
        var message = string.Format(CultureInfo.CurrentCulture, Messages.Composite.MappedDuplicateDictionaryKey, Display(key), name);

        return new ArgumentException(message);
    }

    public static Exception NonContiguousIndex(int index, int maxIndex, string name)
    {
        var message = string.Format(CultureInfo.CurrentCulture, Messages.Composite.MappedNonContiguousIndex, index, maxIndex, name);

        return new ArgumentException(message);
    }

    public static Exception ColumnExceedsLength(string column, int length, string name)
    {
        var message = string.Format(CultureInfo.CurrentCulture, Messages.Composite.MappedColumnExceedsLength, column, length, name);

        return new ArgumentException(message);
    }

    public static Exception MapperNotSupported(string typeName)
    {
        var message = string.Format(CultureInfo.CurrentCulture, Messages.Composite.MapperNotSupported, typeName);

        return new NotSupportedException(message);
    }

    private static FormatException InvalidCell(string value, string typeName, string column)
    {
        var message = string.Format(CultureInfo.CurrentCulture, Messages.Composite.CellValueInvalid, value, typeName, column);

        return new FormatException(message);
    }

    private static FormatException InvalidFormattedCell(string value, string typeName, string format, string column)
    {
        var message = string.Format(CultureInfo.CurrentCulture, Messages.Composite.CellValueInvalidFormat, value, typeName, format, column);

        return new FormatException(message);
    }

    private static string Display(object? value)
    {
        return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
    }
}
