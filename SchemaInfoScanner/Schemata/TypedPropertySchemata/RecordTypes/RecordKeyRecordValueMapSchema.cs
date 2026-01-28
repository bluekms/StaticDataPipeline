using Eds.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SchemaInfoScanner.Extensions;

namespace SchemaInfoScanner.Schemata.TypedPropertySchemata.RecordTypes;

public sealed record RecordKeyRecordValueMapSchema(
    RecordTypeGenericArgumentSchema KeyGenericArgumentSchema,
    RecordTypeGenericArgumentSchema ValueGenericArgumentSchema,
    INamedTypeSymbol NamedTypeSymbol,
    IReadOnlyList<AttributeSyntax> AttributeList)
    : PropertySchemaBase(KeyGenericArgumentSchema.PropertyName, NamedTypeSymbol, AttributeList)
{
    protected override void OnCheckCompatibility(CompatibilityContext context)
    {
        if (!TryGetAttributeValue<LengthAttribute, int>(out var length))
        {
            throw new InvalidOperationException($"Parameter {PropertyName} cannot have LengthAttribute in the argument: {context}");
        }

        if (ValueGenericArgumentSchema.NestedSchema is not RecordPropertySchema valueRecordSchema)
        {
            throw new InvalidOperationException($"Value type of RecordKeyRecordValueMapSchema must be a RecordPropertySchema: {PropertyName}");
        }

        var keyMemberSchema = valueRecordSchema.MemberSchemata
            .SingleOrDefault(m => m.HasAttribute<KeyAttribute>());

        if (keyMemberSchema is null)
        {
            throw new InvalidOperationException($"Value record must have exactly one [Key] member: {PropertyName}");
        }

        var startPosition = context.Position;
        var keyContext = CompatibilityContext.CreateCollectKey(context.MetadataCatalogs, context.Cells, startPosition);

        for (var i = 0; i < length; i++)
        {
            foreach (var memberSchema in valueRecordSchema.MemberSchemata)
            {
                if (memberSchema == keyMemberSchema)
                {
                    keyContext.BeginKeyScope();
                    memberSchema.CheckCompatibility(keyContext);
                    keyContext.EndKeyScope();
                }
                else
                {
                    memberSchema.CheckCompatibility(keyContext);
                }
            }
        }

        keyContext.ValidateNoDuplicates();
        context.Skip(keyContext.Position - startPosition);
    }
}
