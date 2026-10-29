using System.Globalization;
using Microsoft.CodeAnalysis;
using SchemaInfoScanner.Resources;
using SchemaInfoScanner.Schemata.AttributeCheckers;
using Sdp.Attributes;

namespace SchemaInfoScanner.Schemata.TypedPropertySchemata.CollectionTypes.NullableTypes;

public sealed record SingleColumnNullablePrimitiveSetPropertySchema(
    PrimitiveTypeGenericArgumentSchema GenericArgumentSchema,
    INamedTypeSymbol NamedTypeSymbol,
    IReadOnlyList<AttributeInfo> AttributeList,
    string Separator)
    : PropertySchemaBase(GenericArgumentSchema.PropertyName, NamedTypeSymbol, AttributeList)
{
    protected override void OnCheckCompatibility(CompatibilityContext context)
    {
        var cell = context.Consume();
        var parts = string.IsNullOrWhiteSpace(cell.Value) && !EmptyCellMeansNullElement()
            ? Array.Empty<string>()
            : cell.Value.Split(Separator);

        if (TryGetAttributeValue<CountRangeAttribute, int>(out var minCount) &&
            TryGetAttributeValue<CountRangeAttribute, int>(out var maxCount, 1))
        {
            if (parts.Length < minCount || parts.Length > maxCount)
            {
                throw new ArgumentOutOfRangeException(
                    PropertyName.FullName,
                    parts.Length,
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Messages.Composite.CountOutOfRange,
                        cell.Address,
                        parts.Length,
                        PropertyName,
                        minCount,
                        maxCount));
            }
        }

        var nestedCells = parts
            .Select(x => new CellData(cell.Address, x.Trim()))
            .ToArray();

        var nestedContext = CompatibilityContext.CreateCollectKey(context.MetadataCatalogs, nestedCells);
        foreach (var nestedCell in nestedCells)
        {
            var result = NullStringAttributeChecker.Check(this, nestedCell.Value);
            if (result.IsNull)
            {
                nestedContext.ConsumeNull();
                continue;
            }

            nestedContext.BeginKeyScope();
            GenericArgumentSchema.CheckCompatibility(nestedContext);
            nestedContext.EndKeyScope();
        }

        nestedContext.ValidateNoDuplicates();
    }
}
