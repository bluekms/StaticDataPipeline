using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SchemaInfoScanner.Schemata.TypedPropertySchemata.CollectionTypes;

namespace SchemaInfoScanner.Schemata.TypedPropertySchemata.RecordTypes;

public sealed record PrimitiveKeyRecordValueMapSchema(
    PrimitiveTypeGenericArgumentSchema KeySchema,
    RecordTypeGenericArgumentSchema ValueSchema,
    INamedTypeSymbol NamedTypeSymbol,
    IReadOnlyList<AttributeSyntax> AttributeList)
    : PropertySchemaBase(KeySchema.PropertyName, NamedTypeSymbol, AttributeList)
{
    protected override void OnCheckCompatibility(CompatibilityContext context)
    {
        var length = ResolveLength(context);

        var startPosition = context.Position;
        var keyContext = CompatibilityContext.CreateCollectKey(context.MetadataCatalogs, context.Cells, startPosition, context.PathPrefix);
        var valueContext = CompatibilityContext.CreateNoCollect(context.MetadataCatalogs, context.Cells, startPosition, context.PathPrefix);

        for (var i = 0; i < length; i++)
        {
            var groupPath = FormattableString.Invariant($"{ColumnName}[{i}].");
            var previousKeyPath = keyContext.EnterPath(groupPath);
            var previousValuePath = valueContext.EnterPath(groupPath);
            keyContext.BeginKeyScope();
            var keyStartPosition = keyContext.Position;
            KeySchema.CheckCompatibility(keyContext);
            keyContext.EndKeyScope();

            var valueStartPosition = valueContext.Position;
            ValueSchema.CheckCompatibility(valueContext);

            var valueConsumed = valueContext.Position - valueStartPosition;
            var keyConsumed = keyContext.Position - keyStartPosition;
            keyContext.Skip(valueConsumed - keyConsumed);
            keyContext.RestorePath(previousKeyPath);
            valueContext.RestorePath(previousValuePath);
        }

        keyContext.ValidateNoDuplicates();
        context.Skip(keyContext.Position - startPosition);
    }
}
