using System.Collections.Frozen;
using System.Globalization;

namespace Eds.Csv;

internal static class CsvRecordMapper
{
    public static TRecord MapToRecord<TRecord>(string[] headers, string[] values)
        where TRecord : class
    {
        var typeInfo = CsvTypeCache.GetTypeInfo(typeof(TRecord));
        var headerIndexMap = BuildHeaderIndexMap(headers);
        var args = new object?[typeInfo.Parameters.Length];

        for (var i = 0; i < typeInfo.Parameters.Length; i++)
        {
            var paramInfo = typeInfo.Parameters[i];
            args[i] = ConvertValue(paramInfo, paramInfo.ColumnName, headerIndexMap, values);
        }

        return (TRecord)typeInfo.Constructor.Invoke(args);
    }

    private static Dictionary<string, int> BuildHeaderIndexMap(string[] headers)
    {
        var map = new Dictionary<string, int>(headers.Length);
        for (var i = 0; i < headers.Length; i++)
        {
            map[headers[i]] = i;
        }

        return map;
    }

    private static object? ConvertValue(
        ParameterMappingInfo paramInfo,
        string baseName,
        Dictionary<string, int> headerIndexMap,
        string[] values)
    {
        if (paramInfo.CollectionKind != CollectionKind.None)
        {
            return paramInfo.CollectionKind switch
            {
                CollectionKind.ImmutableArray => ConvertToImmutableArray(
                    paramInfo.ElementType!,
                    baseName,
                    paramInfo.Length!.Value,
                    headerIndexMap,
                    values,
                    paramInfo.NullString),
                CollectionKind.FrozenSet => ConvertToFrozenSet(
                    paramInfo.ElementType!,
                    baseName,
                    paramInfo.Length!.Value,
                    headerIndexMap,
                    values,
                    paramInfo.NullString),
                CollectionKind.FrozenDictionary => ConvertToFrozenDictionary(
                    paramInfo.KeyType!,
                    paramInfo.ElementType!,
                    baseName,
                    paramInfo.Length!.Value,
                    headerIndexMap,
                    values,
                    paramInfo.NullString),
                CollectionKind.SingleColumnImmutableArray => ConvertToSingleColumnImmutableArray(
                    paramInfo.ElementType!,
                    baseName,
                    paramInfo.SingleColumnSeparator!,
                    headerIndexMap,
                    values),
                _ => throw new InvalidOperationException($"Unknown collection type: {paramInfo.CollectionKind}"),
            };
        }

        if (!IsPrimitiveOrSimpleType(paramInfo.ParameterType))
        {
            return CreateRecordInstance(
                paramInfo.ParameterType,
                baseName,
                headerIndexMap,
                values,
                paramInfo.NullString);
        }

        if (!headerIndexMap.TryGetValue(baseName, out var index))
        {
            throw new InvalidOperationException($"Header '{baseName}' not found in CSV");
        }

        return ConvertStringValue(paramInfo.ParameterType, values[index], paramInfo.NullString);
    }

    private static object ConvertStringValue(Type targetType, string value, string? nullString)
    {
        var underlyingType = Nullable.GetUnderlyingType(targetType);
        if (underlyingType is not null)
        {
            if (nullString is not null && value == nullString)
            {
                return null!;
            }

            targetType = underlyingType;
        }

        if (targetType == typeof(string))
        {
            if (nullString is not null && value == nullString)
            {
                return null!;
            }

            return value;
        }

        if (targetType.IsEnum)
        {
            var parsed = Enum.Parse(targetType, value);
            if (!Enum.IsDefined(targetType, parsed))
            {
                throw new ArgumentException($"'{value}' is not a defined value of enum '{targetType.Name}'");
            }

            return parsed;
        }

        return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
    }

    private static object? ConvertToImmutableArray(
        Type elementType,
        string baseName,
        int length,
        Dictionary<string, int> headerIndexMap,
        string[] values,
        string? nullString)
    {
        var array = Array.CreateInstance(elementType, length);
        var isPrimitive = IsPrimitiveOrSimpleType(elementType);

        for (var i = 0; i < length; i++)
        {
            var headerName = $"{baseName}[{i}]";
            object? convertedValue;

            if (isPrimitive)
            {
                if (!headerIndexMap.TryGetValue(headerName, out var index))
                {
                    throw new InvalidOperationException($"Header '{headerName}' not found in CSV");
                }

                convertedValue = ConvertStringValue(elementType, values[index], nullString);
            }
            else
            {
                convertedValue = CreateRecordInstance(elementType, headerName, headerIndexMap, values, nullString);
            }

            array.SetValue(convertedValue, i);
        }

        var createMethod = CsvTypeCache.GetImmutableArrayCreateMethod(elementType);
        return createMethod.Invoke(null, [array]);
    }

    private static object? ConvertToSingleColumnImmutableArray(
        Type elementType,
        string baseName,
        string separator,
        Dictionary<string, int> headerIndexMap,
        string[] values)
    {
        if (!headerIndexMap.TryGetValue(baseName, out var index))
        {
            throw new InvalidOperationException($"Header '{baseName}' not found in CSV");
        }

        var cellValue = values[index];
        var parts = cellValue.Split(separator);
        var array = Array.CreateInstance(elementType, parts.Length);

        for (var i = 0; i < parts.Length; i++)
        {
            var trimmedValue = parts[i].Trim();
            var convertedValue = ConvertStringValue(elementType, trimmedValue, null);
            array.SetValue(convertedValue, i);
        }

        var createMethod = CsvTypeCache.GetImmutableArrayCreateMethod(elementType);
        return createMethod.Invoke(null, [array]);
    }

    private static object? ConvertToFrozenSet(
        Type elementType,
        string baseName,
        int length,
        Dictionary<string, int> headerIndexMap,
        string[] values,
        string? nullString)
    {
        var helperMethod = CsvTypeCache.GetFrozenSetHelperMethod(elementType, nameof(ConvertToFrozenSetHelper));
        return helperMethod.Invoke(null, [baseName, length, headerIndexMap, values, nullString]);
    }

    internal static FrozenSet<T> ConvertToFrozenSetHelper<T>(
        string baseName,
        int length,
        Dictionary<string, int> headerIndexMap,
        string[] values,
        string? nullString)
    {
        var list = new List<T>(length);
        var elementType = typeof(T);
        var isPrimitive = IsPrimitiveOrSimpleType(elementType);

        for (var i = 0; i < length; i++)
        {
            var headerName = $"{baseName}[{i}]";
            object? convertedValue;

            if (isPrimitive)
            {
                if (!headerIndexMap.TryGetValue(headerName, out var index))
                {
                    throw new InvalidOperationException($"Header '{headerName}' not found in CSV");
                }

                convertedValue = ConvertStringValue(elementType, values[index], nullString);
            }
            else
            {
                convertedValue = CreateRecordInstance(elementType, headerName, headerIndexMap, values, nullString);
            }

            list.Add((T)convertedValue!);
        }

        return list.ToFrozenSet();
    }

    private static object? ConvertToFrozenDictionary(
        Type keyType,
        Type valueType,
        string baseName,
        int length,
        Dictionary<string, int> headerIndexMap,
        string[] values,
        string? nullString)
    {
        var helperMethod = CsvTypeCache.GetFrozenDictionaryHelperMethod(
            keyType,
            valueType,
            nameof(ConvertToFrozenDictionaryHelper));
        return helperMethod.Invoke(null, [baseName, length, headerIndexMap, values, nullString]);
    }

    internal static FrozenDictionary<TKey, TValue> ConvertToFrozenDictionaryHelper<TKey, TValue>(
        string baseName,
        int length,
        Dictionary<string, int> headerIndexMap,
        string[] values,
        string? nullString)
        where TKey : notnull
    {
        var dictionary = new Dictionary<TKey, TValue>(length);
        var valueType = typeof(TValue);

        for (var i = 0; i < length; i++)
        {
            var elementBaseName = $"{baseName}[{i}]";
            var valueInstance = CreateRecordInstance(valueType, elementBaseName, headerIndexMap, values, nullString);

            var keyProperty = CsvTypeCache.GetKeyProperty(valueType);
            var keyInstance = keyProperty.GetValue(valueInstance);
            dictionary.Add((TKey)keyInstance!, (TValue)valueInstance!);
        }

        return dictionary.ToFrozenDictionary();
    }

    private static object CreateRecordInstance(
        Type recordType,
        string baseName,
        Dictionary<string, int> headerIndexMap,
        string[] values,
        string? nullString)
    {
        if (IsPrimitiveOrSimpleType(recordType))
        {
            if (!headerIndexMap.TryGetValue(baseName, out var index))
            {
                throw new InvalidOperationException($"Header '{baseName}' not found in CSV");
            }

            return ConvertStringValue(recordType, values[index], nullString);
        }

        var typeInfo = CsvTypeCache.GetTypeInfo(recordType);

        // for single parameter record types, map directly
        if (typeInfo.Parameters.Length == 1 &&
            IsPrimitiveOrSimpleType(typeInfo.Parameters[0].ParameterType))
        {
            if (headerIndexMap.TryGetValue(baseName, out var index))
            {
                var paramInfo = typeInfo.Parameters[0];
                var value = ConvertStringValue(
                    paramInfo.ParameterType,
                    values[index],
                    paramInfo.NullString ?? nullString);
                return typeInfo.Constructor.Invoke([value]);
            }
        }

        var args = new object?[typeInfo.Parameters.Length];

        for (var i = 0; i < typeInfo.Parameters.Length; i++)
        {
            var paramInfo = typeInfo.Parameters[i];
            var fullName = $"{baseName}.{paramInfo.ColumnName}";
            args[i] = ConvertParameterValue(paramInfo, fullName, headerIndexMap, values, nullString);
        }

        return typeInfo.Constructor.Invoke(args);
    }

    private static object? ConvertParameterValue(
        ParameterMappingInfo paramInfo,
        string baseName,
        Dictionary<string, int> headerIndexMap,
        string[] values,
        string? nullString)
    {
        var effectiveNullString = paramInfo.NullString ?? nullString;

        if (paramInfo.CollectionKind != CollectionKind.None)
        {
            return paramInfo.CollectionKind switch
            {
                CollectionKind.ImmutableArray => ConvertToImmutableArray(
                    paramInfo.ElementType!,
                    baseName,
                    paramInfo.Length!.Value,
                    headerIndexMap,
                    values,
                    effectiveNullString),
                CollectionKind.FrozenSet => ConvertToFrozenSet(
                    paramInfo.ElementType!,
                    baseName,
                    paramInfo.Length!.Value,
                    headerIndexMap,
                    values,
                    effectiveNullString),
                CollectionKind.FrozenDictionary => ConvertToFrozenDictionary(
                    paramInfo.KeyType!,
                    paramInfo.ElementType!,
                    baseName,
                    paramInfo.Length!.Value,
                    headerIndexMap,
                    values,
                    effectiveNullString),
                _ => throw new InvalidOperationException($"Unknown collection type: {paramInfo.CollectionKind}"),
            };
        }

        if (IsPrimitiveOrSimpleType(paramInfo.ParameterType))
        {
            if (!headerIndexMap.TryGetValue(baseName, out var index))
            {
                throw new InvalidOperationException($"Header '{baseName}' not found in CSV");
            }

            return ConvertStringValue(paramInfo.ParameterType, values[index], effectiveNullString);
        }

        return CreateRecordInstance(paramInfo.ParameterType, baseName, headerIndexMap, values, effectiveNullString);
    }

    private static bool IsPrimitiveOrSimpleType(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        return underlyingType.IsPrimitive ||
               underlyingType.IsEnum ||
               underlyingType == typeof(string) ||
               underlyingType == typeof(decimal) ||
               underlyingType == typeof(DateTime) ||
               underlyingType == typeof(DateTimeOffset) ||
               underlyingType == typeof(TimeSpan) ||
               underlyingType == typeof(Guid);
    }
}
