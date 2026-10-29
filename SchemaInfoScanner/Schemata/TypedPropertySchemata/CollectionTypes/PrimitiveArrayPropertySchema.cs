using Microsoft.CodeAnalysis;

namespace SchemaInfoScanner.Schemata.TypedPropertySchemata.CollectionTypes;

public sealed record PrimitiveArrayPropertySchema(
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
            GenericArgumentSchema.CheckCompatibility(context);
        }
    }
}
