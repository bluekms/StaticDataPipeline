using System.Globalization;
using Microsoft.CodeAnalysis;
using SchemaInfoScanner.Extensions;
using SchemaInfoScanner.NameObjects;
using SchemaInfoScanner.Resources;
using SchemaInfoScanner.Schemata.AttributeCheckers;
using Sdp.Attributes;

namespace SchemaInfoScanner.Schemata.TypedPropertySchemata.PrimitiveTypes;

public sealed record EnumPropertySchema(
    PropertyName PropertyName,
    INamedTypeSymbol NamedTypeSymbol,
    IReadOnlyList<AttributeInfo> AttributeList)
    : PropertySchemaBase(PropertyName, NamedTypeSymbol, AttributeList)
{
    protected override void OnCheckCompatibility(CompatibilityContext context)
    {
        var cell = context.Consume();
        var value = cell.Value;

        if (!AttributeAccessors.HasAttribute<KeyAttribute>(AttributeList))
        {
            var isMember = NamedTypeSymbol.GetMembers()
                .OfType<IFieldSymbol>()
                .Where(x => x.IsConst)
                .Any(x => x.Name == value);

            if (!isMember)
            {
                var enumName = new EnumName(NamedTypeSymbol);
                throw new InvalidOperationException(string.Format(
                    CultureInfo.CurrentCulture,
                    Messages.Composite.InvalidCellValueForEnum,
                    cell.Value,
                    cell.Address,
                    enumName.FullName));
            }
        }

        if (this.HasAttribute<RangeAttribute>())
        {
            RangeAttributeChecker.CheckEnum(this, value, NamedTypeSymbol);
        }

        context.Collect(value);
    }
}
