using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using SchemaInfoScanner.Extensions;
using SchemaInfoScanner.NameObjects;
using SchemaInfoScanner.Resources;
using SchemaInfoScanner.Schemata.AttributeCheckers;
using Sdp.Attributes;

namespace SchemaInfoScanner.Schemata.TypedPropertySchemata.PrimitiveTypes;

public sealed record StringPropertySchema(
    PropertyName PropertyName,
    INamedTypeSymbol NamedTypeSymbol,
    IReadOnlyList<AttributeInfo> AttributeList)
    : PropertySchemaBase(PropertyName, NamedTypeSymbol, AttributeList)
{
    protected override void OnCheckCompatibility(CompatibilityContext context)
    {
        var cell = context.Consume();

        if (this.TryGetAttributeValue<RegularExpressionAttribute, string>(0, out var pattern))
        {
            if (!Regex.IsMatch(cell.Value, pattern))
            {
                throw new InvalidOperationException(string.Format(
                    CultureInfo.CurrentCulture,
                    Messages.Composite.InvalidCellValueWithPattern,
                    cell.Value,
                    cell.Address,
                    pattern));
            }
        }

        if (this.HasAttribute<RangeAttribute>())
        {
            RangeAttributeChecker.Check(this, cell.Value);
        }

        context.Collect(cell.Value);
    }
}
