using System.Globalization;
using Microsoft.CodeAnalysis;
using SchemaInfoScanner.Resources;
using SchemaInfoScanner.Schemata.AttributeCheckers;
using Sdp.Attributes;

namespace SchemaInfoScanner.Schemata.TypedPropertySchemata.CollectionTypes.NullableTypes;

public sealed record SingleColumnNullablePrimitiveArrayPropertySchema(
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

        foreach (var rawPart in parts)
        {
            var part = rawPart.Trim();
            var result = NullStringAttributeChecker.Check(this, part);
            if (result.IsNull)
            {
                continue;
            }

            var nestedCells = new[] { new CellData(cell.Address, part) };
            var nestedContext = CompatibilityContext.CreateNoCollect(context.MetadataCatalogs, nestedCells);
            GenericArgumentSchema.CheckCompatibility(nestedContext);
        }
    }
}
