using System.Globalization;
using SchemaInfoScanner.Extensions;
using SchemaInfoScanner.NameObjects;
using SchemaInfoScanner.Resources;
using Sdp.Attributes;

namespace SchemaInfoScanner.Schemata.TypedPropertySchemata.RecordTypes;

internal static class MapCompatibilityChecker
{
    public static void Check(
        CompatibilityContext context,
        PropertyName propertyName,
        string columnName,
        int length,
        RecordTypeGenericArgumentSchema valueSchema)
    {
        if (valueSchema.NestedSchema is not RecordPropertySchema valueRecordSchema)
        {
            throw new InvalidOperationException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.ValueTypeMustBeRecordPropertySchema,
                propertyName));
        }

        var keyMembers = valueRecordSchema.MemberSchemata
            .Where(m => m.HasAttribute<KeyAttribute>())
            .ToList();
        if (keyMembers.Count != 1)
        {
            throw new InvalidOperationException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.ValueRecordMustHaveOneKeyMember,
                propertyName));
        }

        var keyMemberSchema = keyMembers[0];
        var startPosition = context.Position;
        var keyContext = CompatibilityContext.CreateCollectKey(context.MetadataCatalogs, context.Cells, startPosition, context.PathPrefix);

        for (var i = 0; i < length; i++)
        {
            var previousPath = keyContext.EnterPath(FormattableString.Invariant($"{columnName}[{i}]."));
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
