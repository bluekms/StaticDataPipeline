using Microsoft.CodeAnalysis;

namespace SchemaInfoScanner.Schemata.TypedPropertySchemata.RecordTypes;

public sealed record RecordKeyRecordValueMapSchema(
    RecordTypeGenericArgumentSchema KeyGenericArgumentSchema,
    RecordTypeGenericArgumentSchema ValueGenericArgumentSchema,
    INamedTypeSymbol NamedTypeSymbol,
    IReadOnlyList<AttributeInfo> AttributeList)
    : PropertySchemaBase(KeyGenericArgumentSchema.PropertyName, NamedTypeSymbol, AttributeList)
{
    protected override void OnCheckCompatibility(CompatibilityContext context)
    {
        var length = ResolveLength(context);
        MapCompatibilityChecker.Check(context, PropertyName, ColumnName, length, ValueGenericArgumentSchema);
    }
}
