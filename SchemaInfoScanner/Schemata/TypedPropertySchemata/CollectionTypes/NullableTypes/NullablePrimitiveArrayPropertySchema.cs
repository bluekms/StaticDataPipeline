using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SchemaInfoScanner.Resources;
using SchemaInfoScanner.Schemata.AttributeCheckers;
using Sdp.Attributes;

namespace SchemaInfoScanner.Schemata.TypedPropertySchemata.CollectionTypes.NullableTypes;

public sealed record NullablePrimitiveArrayPropertySchema(
    PrimitiveTypeGenericArgumentSchema GenericArgumentSchema,
    INamedTypeSymbol NamedTypeSymbol,
    IReadOnlyList<AttributeSyntax> AttributeList)
    : PropertySchemaBase(GenericArgumentSchema.PropertyName, NamedTypeSymbol, AttributeList)
{
    protected override void OnCheckCompatibility(CompatibilityContext context)
    {
        if (!TryGetAttributeValue<LengthAttribute, int>(out var length))
        {
            throw new InvalidOperationException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.ParameterCannotHaveLengthAttributeWithContext,
                PropertyName,
                context));
        }

        for (var i = 0; i < length; i++)
        {
            var result = NullStringAttributeChecker.Check(this, context.Current.Value);
            if (result.IsNull)
            {
                context.ConsumeNull();
            }
            else
            {
                GenericArgumentSchema.CheckCompatibility(context);
            }
        }
    }
}
