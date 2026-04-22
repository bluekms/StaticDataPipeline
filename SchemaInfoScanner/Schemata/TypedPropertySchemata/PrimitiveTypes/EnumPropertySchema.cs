using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SchemaInfoScanner.Extensions;
using SchemaInfoScanner.NameObjects;
using SchemaInfoScanner.Resources;
using Sdp.Attributes;

namespace SchemaInfoScanner.Schemata.TypedPropertySchemata.PrimitiveTypes;

public sealed record EnumPropertySchema(
    PropertyName PropertyName,
    INamedTypeSymbol NamedTypeSymbol,
    IReadOnlyList<AttributeSyntax> AttributeList)
    : PropertySchemaBase(PropertyName, NamedTypeSymbol, AttributeList)
{
    protected override void OnCheckCompatibility(CompatibilityContext context)
    {
        var cell = context.Consume();
        var value = cell.Value;

        if (!AttributeAccessors.HasAttribute<KeyAttribute>(AttributeList))
        {
            var enumName = new EnumName(NamedTypeSymbol);
            var enumMembers = context.MetadataCatalogs.EnumMemberCatalog.GetEnumMembers(enumName);

            if (!enumMembers.Contains(value))
            {
                throw new InvalidOperationException(string.Format(
                    CultureInfo.CurrentCulture,
                    Messages.Composite.InvalidCellValueForEnum,
                    cell.Value,
                    cell.Address,
                    enumName.FullName));
            }
        }

        context.Collect(value);
    }
}
