using System.Globalization;
using Microsoft.CodeAnalysis;
using SchemaInfoScanner.NameObjects;
using SchemaInfoScanner.Resources;

namespace SchemaInfoScanner.Schemata.TypedPropertySchemata.PrimitiveTypes;

public sealed record GuidPropertySchema(
    PropertyName PropertyName,
    INamedTypeSymbol NamedTypeSymbol,
    IReadOnlyList<AttributeInfo> AttributeList)
    : PropertySchemaBase(PropertyName, NamedTypeSymbol, AttributeList)
{
    protected override void OnCheckCompatibility(CompatibilityContext context)
    {
        var cell = context.Consume();

        if (!Guid.TryParse(cell.Value, out var value))
        {
            throw new InvalidOperationException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    Messages.Composite.InvalidCellValue,
                    cell.Value,
                    cell.Address));
        }

        context.Collect(value);
    }
}
