using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.CodeAnalysis;
using SchemaInfoScanner.Extensions;
using SchemaInfoScanner.NameObjects;
using SchemaInfoScanner.Schemata.TypedPropertySchemata;
using SchemaInfoScanner.TypeCheckers;
using Sdp.Attributes;

namespace SchemaInfoScanner.Schemata;

public abstract record PropertySchemaBase(
    PropertyName PropertyName,
    INamedTypeSymbol NamedTypeSymbol,
    IReadOnlyList<AttributeInfo> AttributeList)
{
    protected abstract void OnCheckCompatibility(CompatibilityContext context);

    public void CheckCompatibility(CompatibilityContext context)
    {
        OnCheckCompatibility(context);
    }

    public override string ToString()
    {
        return PropertyName.FullName;
    }

    public bool IsNullable()
    {
        return PrimitiveTypeChecker.IsNullablePrimitiveType(NamedTypeSymbol);
    }

    protected int ResolveLength(CompatibilityContext context)
    {
        var hasLength = TryGetAttributeValue<LengthAttribute, int>(out var length);
        if (hasLength)
        {
            return length;
        }

        var indexedPrefix = context.PathPrefix + ColumnName + "[";
        var indices = new HashSet<int>();
        for (var offset = 0; ; offset++)
        {
            var header = context.PeekHeader(offset);
            if (header is null || !header.StartsWith(indexedPrefix, StringComparison.Ordinal))
            {
                break;
            }

            var close = header.IndexOf(']', indexedPrefix.Length);
            if (close < 0)
            {
                break;
            }

            var indexText = header.Substring(indexedPrefix.Length, close - indexedPrefix.Length);
            var isIndex = int.TryParse(indexText, NumberStyles.None, CultureInfo.InvariantCulture, out var index);
            if (!isIndex)
            {
                break;
            }

            indices.Add(index);
        }

        return indices.Count;
    }

    protected string ColumnName
    {
        get
        {
            TryGetAttributeValue<ColumnNameAttribute, string>(out var declaredColumnName);

            return declaredColumnName ?? PropertyName.Name;
        }
    }

    protected bool EmptyCellMeansNullElement()
    {
        var hasNullString = TryGetAttributeValue<NullStringAttribute, string>(out var nullString);

        return hasNullString && nullString is { Length: 0 };
    }

    public bool TryGetAttributeValue<TAttribute, TValue>(
        [NotNullWhen(true)] out TValue? value,
        int attributeParameterIndex = 0)
        where TAttribute : Attribute
    {
        return AttributeAccessors.TryGetAttributeValue<TAttribute, TValue>(
            AttributeList,
            out value,
            attributeParameterIndex);
    }
}
