using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SchemaInfoScanner.Resources;
using Sdp.Attributes;

namespace SchemaInfoScanner.Schemata.TypedPropertySchemata.CollectionTypes;

public sealed record SingleColumnPrimitiveArrayPropertySchema(
    PrimitiveTypeGenericArgumentSchema GenericArgumentSchema,
    INamedTypeSymbol NamedTypeSymbol,
    IReadOnlyList<AttributeSyntax> AttributeList,
    string Separator)
    : PropertySchemaBase(GenericArgumentSchema.PropertyName, NamedTypeSymbol, AttributeList)
{
    protected override void OnCheckCompatibility(CompatibilityContext context)
    {
        var cell = context.Consume();
        var parts = cell.Value.Split(Separator);

        if (TryGetAttributeValue<LengthAttribute, int>(out var length))
        {
            if (parts.Length != length)
            {
                throw new InvalidOperationException(string.Format(
                    CultureInfo.CurrentCulture,
                    Messages.Composite.SingleColumnCellCountMismatch,
                    cell.Address,
                    parts.Length,
                    PropertyName,
                    length));
            }
        }

        foreach (var part in parts)
        {
            if (string.IsNullOrWhiteSpace(part))
            {
                throw new InvalidOperationException(string.Format(
                    CultureInfo.CurrentCulture,
                    Messages.Composite.SingleColumnCellEmptyValue,
                    cell.Address,
                    PropertyName));
            }

            var nestedCells = new[] { new CellData(cell.Address, part) };
            var nestedContext = CompatibilityContext.CreateNoCollect(context.MetadataCatalogs, nestedCells);

            GenericArgumentSchema.CheckCompatibility(nestedContext);
        }
    }
}
