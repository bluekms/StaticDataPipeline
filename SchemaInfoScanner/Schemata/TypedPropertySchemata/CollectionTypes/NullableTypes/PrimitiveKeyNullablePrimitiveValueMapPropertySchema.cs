using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SchemaInfoScanner.NameObjects;
using SchemaInfoScanner.Schemata.AttributeCheckers;
using Sdp.Attributes;

namespace SchemaInfoScanner.Schemata.TypedPropertySchemata.CollectionTypes.NullableTypes;

public sealed record PrimitiveKeyNullablePrimitiveValueMapPropertySchema(
    PropertyName PropertyName,
    INamedTypeSymbol NamedTypeSymbol,
    IReadOnlyList<AttributeSyntax> AttributeList,
    PrimitiveTypeGenericArgumentSchema KeySchema,
    PrimitiveTypeGenericArgumentSchema ValueSchema)
    : PropertySchemaBase(PropertyName, NamedTypeSymbol, AttributeList)
{
    protected override void OnCheckCompatibility(CompatibilityContext context)
    {
        if (!TryGetAttributeValue<LengthAttribute, int>(out var length))
        {
            throw new InvalidOperationException($"Parameter {PropertyName} cannot have LengthAttribute in the argument: {context}");
        }

        var keys = new List<object?>();
        for (var i = 0; i < length; i++)
        {
            KeySchema.CheckCompatibility(context);

            var result = NullStringAttributeChecker.Check(this, context.Current.Value);
            if (!result.IsNull)
            {
                ValueSchema.CheckCompatibility(context);
            }
        }

        var hs = new HashSet<object?>();
        foreach (var key in keys)
        {
            if (!hs.Add(key))
            {
                throw new InvalidOperationException($"Parameter {PropertyName} has duplicate key: {key} in context {context}.");
            }
        }
    }
}
