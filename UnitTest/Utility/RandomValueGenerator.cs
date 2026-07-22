namespace UnitTest.Utility;

public static class RandomValueGenerator
{
    public static T? Generate<T>()
    {
        var type = typeof(T);
        var isNullable = Nullable.GetUnderlyingType(type) != null;
        if (isNullable)
        {
            if (Random.Shared.NextDouble() < 0.25)
            {
                return default;
            }
        }

        if (type.IsEnum)
        {
            var values = Enum.GetValues(type);

            return (T)values.GetValue(Random.Shared.Next(values.Length))!;
        }

        if (type == typeof(string))
        {
            return (T)(object)GenerateString(1, 255);
        }

        if (type.IsPrimitive ||
            type == typeof(decimal) ||
            type == typeof(decimal?) ||
            Nullable.GetUnderlyingType(type) != null)
        {
            return (T)GeneratePrimitive(type);
        }

        throw new NotSupportedException($"Type {type} is not supported");
    }

    public static object? Generate(Type type)
    {
        var isNullable = Nullable.GetUnderlyingType(type) != null;
        if (isNullable)
        {
            if (Random.Shared.NextDouble() < 0.25)
            {
                return null;
            }
        }

        if (type.IsEnum)
        {
            var values = Enum.GetValues(type);

            return values.GetValue(Random.Shared.Next(values.Length))!;
        }

        if (type == typeof(string))
        {
            return GenerateString(1, 255);
        }

        if (type.IsPrimitive ||
            type == typeof(decimal) ||
            type == typeof(decimal?) ||
            Nullable.GetUnderlyingType(type) != null)
        {
            return GeneratePrimitive(type);
        }

        throw new NotSupportedException($"Type {type} is not supported");
    }

    public static string GenerateString(int minLength, int maxLength)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()_+-=[]{}|;:',.<>?";

        var length = Random.Shared.Next(minLength, maxLength + 1);

        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[Random.Shared.Next(s.Length)])
            .ToArray());
    }

    private static object GeneratePrimitive(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
        if (underlyingType == typeof(int))
        {
            return Random.Shared.Next();
        }

        if (underlyingType == typeof(double))
        {
            return Random.Shared.NextDouble();
        }

        if (underlyingType == typeof(float))
        {
            return (float)Random.Shared.NextDouble();
        }

        if (underlyingType == typeof(bool))
        {
            return Random.Shared.Next(2) is 1;
        }

        if (underlyingType == typeof(byte))
        {
            var buffer = new byte[1];
            Random.Shared.NextBytes(buffer);

            return buffer[0];
        }

        if (underlyingType == typeof(char))
        {
            return (char)Random.Shared.Next(33, 127); // 모든 printable ASCII 문자
        }

        if (underlyingType == typeof(sbyte))
        {
            return (sbyte)Random.Shared.Next(sbyte.MinValue, sbyte.MaxValue + 1);
        }

        if (underlyingType == typeof(short))
        {
            return (short)Random.Shared.Next(short.MinValue, short.MaxValue + 1);
        }

        if (underlyingType == typeof(ushort))
        {
            return (ushort)Random.Shared.Next(ushort.MinValue, ushort.MaxValue + 1);
        }

        if (underlyingType == typeof(uint))
        {
            return (uint)Random.Shared.Next();
        }

        if (underlyingType == typeof(long))
        {
            return Random.Shared.NextInt64();
        }

        if (underlyingType == typeof(ulong))
        {
            return (ulong)Random.Shared.NextInt64();
        }

        if (underlyingType == typeof(decimal))
        {
            var scale = (byte)Random.Shared.Next(29);
            var sign = Random.Shared.Next(2) == 1;

            return new decimal(Random.Shared.Next(), Random.Shared.Next(), Random.Shared.Next(), sign, scale);
        }

        throw new NotSupportedException($"Type {type} is not supported");
    }
}
