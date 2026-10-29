using Microsoft.CodeAnalysis;
using SchemaInfoScanner.Schemata.AttributeCheckers;

namespace SchemaInfoScanner.Schemata.TypedPropertySchemata.CollectionTypes.NullableTypes;

public sealed record NullablePrimitiveArrayPropertySchema(
    PrimitiveTypeGenericArgumentSchema GenericArgumentSchema,
    INamedTypeSymbol NamedTypeSymbol,
    IReadOnlyList<AttributeInfo> AttributeList)
    : PropertySchemaBase(GenericArgumentSchema.PropertyName, NamedTypeSymbol, AttributeList)
{
    protected override void OnCheckCompatibility(CompatibilityContext context)
    {
        var length = ResolveLength(context);

        for (var i = 0; i < length; i++)
        {
            var result = NullStringAttributeChecker.Check(this, context.Current.Value);
            if (result.IsNull)
            {
                context.ConsumeNull();
            }
            else
            {
                GenericArgumentSchema.CheckCompatibility(context);
            }
        }
    }
}
