using Microsoft.CodeAnalysis;

namespace SchemaInfoScanner.Schemata.TypedPropertySchemata.CollectionTypes;

public sealed record PrimitiveSetPropertySchema(
    PrimitiveTypeGenericArgumentSchema GenericArgumentSchema,
    INamedTypeSymbol NamedTypeSymbol,
    IReadOnlyList<AttributeInfo> AttributeList)
    : PropertySchemaBase(GenericArgumentSchema.PropertyName, NamedTypeSymbol, AttributeList)
{
    protected override void OnCheckCompatibility(CompatibilityContext context)
    {
        var length = ResolveLength(context);

        var startPosition = context.Position;
        var keyContext = CompatibilityContext.CreateCollectKey(context.MetadataCatalogs, context.Cells, startPosition);

        for (var i = 0; i < length; i++)
        {
            keyContext.BeginKeyScope();
            GenericArgumentSchema.CheckCompatibility(keyContext);
            keyContext.EndKeyScope();
        }

        keyContext.ValidateNoDuplicates();
        context.Skip(keyContext.Position - startPosition);
    }
}
