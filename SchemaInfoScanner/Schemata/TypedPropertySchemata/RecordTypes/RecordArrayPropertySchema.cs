using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SchemaInfoScanner.Schemata.TypedPropertySchemata.RecordTypes;

public sealed record RecordArrayPropertySchema(
    RecordTypeGenericArgumentSchema GenericArgumentSchema,
    INamedTypeSymbol NamedTypeSymbol,
    IReadOnlyList<AttributeSyntax> AttributeList)
    : PropertySchemaBase(GenericArgumentSchema.PropertyName, NamedTypeSymbol, AttributeList)
{
    protected override void OnCheckCompatibility(CompatibilityContext context)
    {
        var length = ResolveLength(context);

        for (var i = 0; i < length; i++)
        {
            var previousPath = context.EnterPath(FormattableString.Invariant($"{ColumnName}[{i}]."));
            GenericArgumentSchema.CheckCompatibility(context);
            context.RestorePath(previousPath);
        }
    }
}
