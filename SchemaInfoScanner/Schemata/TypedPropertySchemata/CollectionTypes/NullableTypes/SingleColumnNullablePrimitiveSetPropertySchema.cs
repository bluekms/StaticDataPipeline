using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SchemaInfoScanner.Schemata.AttributeCheckers;
using Sdp.Attributes;

namespace SchemaInfoScanner.Schemata.TypedPropertySchemata.CollectionTypes.NullableTypes;

public sealed record SingleColumnNullablePrimitiveSetPropertySchema(
    PrimitiveTypeGenericArgumentSchema GenericArgumentSchema,
    INamedTypeSymbol NamedTypeSymbol,
    IReadOnlyList<AttributeSyntax> AttributeList,
    string Separator)
    : PropertySchemaBase(GenericArgumentSchema.PropertyName, NamedTypeSymbol, AttributeList)
{
    protected override void OnCheckCompatibility(CompatibilityContext context)
    {
        var cell = context.Consume();
        var arguments = cell.Value.Split(Separator);

        if (TryGetAttributeValue<LengthAttribute, int>(out var length))
        {
            if (arguments.Length != length)
            {
                throw new InvalidOperationException(
                    $"Cell {cell.Address} contains {arguments.Length} value(s), but {PropertyName} expects {length}.");
            }
        }

        foreach (var argument in arguments)
        {
            var result = NullStringAttributeChecker.Check(this, argument);
            if (result.IsNull)
            {
                context.ConsumeNull();
            }
            else
            {
                var nestedCells = new[] { new CellData(cell.Address, argument) };
                var nestedContext = CompatibilityContext.CreateCollectKey(context.MetadataCatalogs, nestedCells);

                nestedContext.BeginKeyScope();
                GenericArgumentSchema.CheckCompatibility(nestedContext);
                nestedContext.EndKeyScope();
            }
        }

        context.ValidateNoDuplicates();
    }
}
