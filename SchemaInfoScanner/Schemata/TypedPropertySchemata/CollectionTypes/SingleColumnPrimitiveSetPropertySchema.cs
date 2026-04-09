using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SchemaInfoScanner.Resources;
using Sdp.Attributes;

namespace SchemaInfoScanner.Schemata.TypedPropertySchemata.CollectionTypes;

public sealed record SingleColumnPrimitiveSetPropertySchema(
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

        var nestedCells = parts
            .Select(x => new CellData(cell.Address, x))
            .ToArray();

        var nestedContext = CompatibilityContext.CreateCollectKey(context.MetadataCatalogs, nestedCells);
        for (var i = 0; i < parts.Length; i++)
        {
            nestedContext.BeginKeyScope();
            GenericArgumentSchema.CheckCompatibility(nestedContext);
            nestedContext.EndKeyScope();
        }

        nestedContext.ValidateNoDuplicates();
    }
}
