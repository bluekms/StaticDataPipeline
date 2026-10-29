using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SchemaInfoScanner.Extensions;
using SchemaInfoScanner.Resources;
using Sdp.Attributes;

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
        var length = ResolveLength(context);

        if (ValueGenericArgumentSchema.NestedSchema is not RecordPropertySchema valueRecordSchema)
        {
            throw new InvalidOperationException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.ValueTypeMustBeRecordPropertySchema,
                PropertyName));
        }

        var keyMemberSchema = valueRecordSchema.MemberSchemata
            .SingleOrDefault(m => m.HasAttribute<KeyAttribute>());

        if (keyMemberSchema is null)
        {
            throw new InvalidOperationException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.ValueRecordMustHaveOneKeyMember,
                PropertyName));
        }

        var startPosition = context.Position;
        var keyContext = CompatibilityContext.CreateCollectKey(context.MetadataCatalogs, context.Cells, startPosition, context.PathPrefix);

        for (var i = 0; i < length; i++)
        {
            var previousPath = keyContext.EnterPath(FormattableString.Invariant($"{ColumnName}[{i}]."));
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

            keyContext.RestorePath(previousPath);
        }

        keyContext.ValidateNoDuplicates();
        context.Skip(keyContext.Position - startPosition);
    }
}
