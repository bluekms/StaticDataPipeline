using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using SchemaInfoScanner.Resources;
using SchemaInfoScanner.Schemata;

namespace SchemaInfoScanner.Extensions;

public static class AttributeAccessors
{
    public static bool HasAttribute<T>(IReadOnlyList<AttributeInfo> attributes)
        where T : Attribute
    {
        var attributeName = AttributeName<T>();

        return attributes.Any(x => x.Name == attributeName);
    }

    public static TValue GetAttributeValue<TAttribute, TValue>(
        IReadOnlyList<AttributeInfo> attributes,
        int attributeParameterIndex = 0)
        where TAttribute : Attribute
    {
        var attributeName = AttributeName<TAttribute>();
        var attribute = attributes.Single(x => x.Name == attributeName);
        var value = ResolveArgumentValue<TAttribute>(attribute, attributeParameterIndex);

        return ConvertValue<TValue>(value);
    }

    public static bool TryGetAttributeValue<TAttribute, TValue>(
        IReadOnlyList<AttributeInfo> attributes,
        [NotNullWhen(true)] out TValue value,
        int attributeParameterIndex = 0)
        where TAttribute : Attribute
    {
        if (!HasAttribute<TAttribute>(attributes))
        {
            value = default!;

            return false;
        }

        var result = GetAttributeValue<TAttribute, TValue>(attributes, attributeParameterIndex);
        if (result is null)
        {
            value = default!;

            return false;
        }

        value = result;

        return true;
    }

    public static IReadOnlyList<string> GetAttributeValueList<TAttribute>(IReadOnlyList<AttributeInfo> attributes)
        where TAttribute : Attribute
    {
        var attributeName = AttributeName<TAttribute>();
        var attribute = attributes.Single(x => x.Name == attributeName);

        return attribute.Arguments.Select(x => x.Text).ToList();
    }

    public static string AttributeName<T>()
        where T : Attribute
    {
        return typeof(T).Name.Replace("Attribute", string.Empty);
    }

    private static object? ResolveArgumentValue<TAttribute>(AttributeInfo attribute, int index)
        where TAttribute : Attribute
    {
        if (index < attribute.Arguments.Count)
        {
            var argument = attribute.Arguments[index];
            if (!argument.IsConstant)
            {
                throw new InvalidOperationException(Messages.UnsupportedExpressionType);
            }

            return argument.Value;
        }

        var hasDefault = TryGetConstructorDefault(typeof(TAttribute), index, out var defaultValue);
        if (hasDefault)
        {
            return defaultValue;
        }

        if (attribute.Arguments.Count == 0)
        {
            throw new ArgumentNullException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.AttributeHasNoProperty,
                typeof(TAttribute).Name));
        }

        throw new ArgumentOutOfRangeException(nameof(index), index, typeof(TAttribute).Name);
    }

    private static bool TryGetConstructorDefault(Type attributeType, int index, out object? defaultValue)
    {
        var constructors = attributeType
            .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .OrderByDescending(x => x.GetParameters().Length);

        foreach (var constructor in constructors)
        {
            var parameters = constructor.GetParameters();
            if (index < parameters.Length && parameters[index].HasDefaultValue)
            {
                defaultValue = parameters[index].DefaultValue;

                return true;
            }
        }

        defaultValue = null;

        return false;
    }

    private static TValue ConvertValue<TValue>(object? value)
    {
        if (value is null)
        {
            return default!;
        }

        if (typeof(TValue).IsEnum)
        {
            return value is string name
                ? (TValue)Enum.Parse(typeof(TValue), name.Split('.')[^1])
                : (TValue)Enum.ToObject(typeof(TValue), value);
        }

        return (TValue)Convert.ChangeType(value, typeof(TValue), CultureInfo.InvariantCulture);
    }
}
