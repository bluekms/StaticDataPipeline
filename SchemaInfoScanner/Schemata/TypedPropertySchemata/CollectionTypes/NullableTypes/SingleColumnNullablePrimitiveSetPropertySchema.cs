using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SchemaInfoScanner.Extensions;
using SchemaInfoScanner.Resources;
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
        var arguments = string.IsNullOrWhiteSpace(cell.Value) && !EmptyCellMeansNullElement()
            ? Array.Empty<string>()
            : cell.Value.Split(Separator);

        if (TryGetAttributeValue<CountRangeAttribute, int>(out var minCount) &&
            TryGetAttributeValue<CountRangeAttribute, int>(out var maxCount, 1))
        {
            if (arguments.Length < minCount || arguments.Length > maxCount)
            {
                throw new ArgumentOutOfRangeException(
                    PropertyName.FullName,
                    arguments.Length,
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Messages.Composite.CountOutOfRange,
                        cell.Address,
                        arguments.Length,
                        PropertyName,
                        minCount,
                        maxCount));
            }
        }

        foreach (var rawArgument in arguments)
        {
            var argument = rawArgument.Trim();
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

    private bool EmptyCellMeansNullElement()
    {
        var hasNullString = this.TryGetAttributeValue<NullStringAttribute, string>(0, out var nullString);

        return hasNullString && nullString is { Length: 0 };
    }
}
