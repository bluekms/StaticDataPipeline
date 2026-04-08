using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SchemaInfoScanner.Resources;

namespace SchemaInfoScanner.Extensions;

public static class AttributeAccessors
{
    public static bool HasAttribute<T>(
        IReadOnlyList<AttributeSyntax> attributeSyntaxList)
        where T : Attribute
    {
        var attributeName = typeof(T).Name.Replace("Attribute", string.Empty);
        return attributeSyntaxList.Any(x => x.Name.ToString() == attributeName);
    }

    public static TValue GetAttributeValue<TAttribute, TValue>(
        IReadOnlyList<AttributeSyntax> attributeSyntaxList,
        int attributeParameterIndex = 0)
        where TAttribute : Attribute
    {
        var attributeName = typeof(TAttribute).Name.Replace("Attribute", string.Empty);
        var attribute = attributeSyntaxList.Single(x => x.Name.ToString() == attributeName);

        if (attribute.ArgumentList is null)
        {
            throw new ArgumentNullException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.AttributeHasNoProperty,
                typeof(TAttribute).Name));
        }

        var valueString = attribute.ArgumentList.Arguments[attributeParameterIndex].ToString().Trim('"');
        return typeof(TValue).IsEnum
            ? (TValue)Enum.Parse(typeof(TValue), valueString.Split('.')[^1])
            : (TValue)Convert.ChangeType(valueString, typeof(TValue), CultureInfo.InvariantCulture);
    }

    public static bool TryGetAttributeValue<TAttribute, TValue>(
        IReadOnlyList<AttributeSyntax> attributeSyntaxList,
        [NotNullWhen(true)] out TValue value,
        int attributeParameterIndex = 0)
        where TAttribute : Attribute
    {
        if (!HasAttribute<TAttribute>(attributeSyntaxList))
        {
            value = default!;
            return false;
        }

        var result = GetAttributeValue<TAttribute, TValue>(attributeSyntaxList, attributeParameterIndex);
        if (result is null)
        {
            value = default!;
            return false;
        }

        value = result;
        return true;
    }
}
