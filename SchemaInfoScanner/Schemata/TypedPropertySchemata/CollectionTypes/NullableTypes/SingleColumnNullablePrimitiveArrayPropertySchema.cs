using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SchemaInfoScanner.Schemata.AttributeCheckers;
using Sdp.Attributes;

namespace SchemaInfoScanner.Schemata.TypedPropertySchemata.CollectionTypes.NullableTypes;

public sealed record SingleColumnNullablePrimitiveArrayPropertySchema(
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
                throw new InvalidOperationException(
                    $"Cell {cell.Address} contains {parts.Length} value(s), but {PropertyName} expects {length}.");
            }
        }

        foreach (var part in parts)
        {
            var result = NullStringAttributeChecker.Check(this, part);

            if (result.IsNull)
            {
                context.ConsumeNull();
            }
            else
            {
                var nestedCells = new[] { new CellData(cell.Address, part) };
                var nestedContext = CompatibilityContext.CreateNoCollect(
                    context.MetadataCatalogs,
                    nestedCells);

                GenericArgumentSchema.CheckCompatibility(nestedContext);
            }
        }
    }
}
