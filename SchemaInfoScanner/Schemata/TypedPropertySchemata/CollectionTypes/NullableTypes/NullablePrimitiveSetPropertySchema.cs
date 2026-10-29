using Microsoft.CodeAnalysis;
using SchemaInfoScanner.Schemata.AttributeCheckers;

namespace SchemaInfoScanner.Schemata.TypedPropertySchemata.CollectionTypes.NullableTypes;

public sealed record NullablePrimitiveSetPropertySchema(
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
            var result = NullStringAttributeChecker.Check(this, keyContext.Current.Value);
            if (result.IsNull)
            {
                keyContext.ConsumeNull();
            }
            else
            {
                keyContext.BeginKeyScope();
                GenericArgumentSchema.CheckCompatibility(keyContext);
                keyContext.EndKeyScope();
            }
        }

        keyContext.ValidateNoDuplicates();
        context.Skip(keyContext.Position - startPosition);
    }
}
