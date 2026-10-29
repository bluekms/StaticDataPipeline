using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SchemaInfoScanner.NameObjects;

namespace SchemaInfoScanner.Schemata.TypedPropertySchemata.RecordTypes;

public sealed record RecordPropertySchema(
    PropertyName PropertyName,
    INamedTypeSymbol NamedTypeSymbol,
    IReadOnlyList<AttributeSyntax> AttributeList,
    IReadOnlyList<PropertySchemaBase> MemberSchemata)
    : PropertySchemaBase(PropertyName, NamedTypeSymbol, AttributeList)
{
    protected override void OnCheckCompatibility(CompatibilityContext context)
    {
        var previousPath = context.EnterPath(ColumnName + ".");
        CheckMembers(context);
        context.RestorePath(previousPath);
    }

    public void CheckMembers(CompatibilityContext context)
    {
        foreach (var schema in MemberSchemata)
        {
            schema.CheckCompatibility(context);
        }
    }
}
