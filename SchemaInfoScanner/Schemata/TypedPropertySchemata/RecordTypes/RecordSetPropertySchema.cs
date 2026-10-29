using Microsoft.CodeAnalysis;

namespace SchemaInfoScanner.Schemata.TypedPropertySchemata.RecordTypes;

public sealed record RecordSetPropertySchema(
    RecordTypeGenericArgumentSchema GenericArgumentSchema,
    INamedTypeSymbol NamedTypeSymbol,
    IReadOnlyList<AttributeInfo> AttributeList)
    : PropertySchemaBase(GenericArgumentSchema.PropertyName, NamedTypeSymbol, AttributeList)
{
    protected override void OnCheckCompatibility(CompatibilityContext context)
    {
        var length = ResolveLength(context);

        var startPosition = context.Position;
        var keyContext = CompatibilityContext.CreateCollectKey(context.MetadataCatalogs, context.Cells, startPosition, context.PathPrefix);

        for (var i = 0; i < length; i++)
        {
            var previousPath = keyContext.EnterPath(FormattableString.Invariant($"{ColumnName}[{i}]."));
            keyContext.BeginKeyScope();
            GenericArgumentSchema.CheckCompatibility(keyContext);
            keyContext.EndKeyScope();
            keyContext.RestorePath(previousPath);
        }

        keyContext.ValidateNoDuplicates();
        context.Skip(keyContext.Position - startPosition);
    }
}
