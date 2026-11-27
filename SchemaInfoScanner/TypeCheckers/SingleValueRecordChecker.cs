using System.Globalization;
using SchemaInfoScanner.Resources;
using SchemaInfoScanner.Schemata;
using SchemaInfoScanner.Schemata.TypedPropertySchemata.RecordTypes;

namespace SchemaInfoScanner.TypeCheckers;

internal static class SingleValueRecordChecker
{
    public static void CheckDirect(PropertySchemaBase property)
    {
        if (property is RecordPropertySchema recordProperty)
        {
            CheckInnerValue(property, recordProperty);
        }
    }

    public static void CheckElement(PropertySchemaBase property)
    {
        var elementSchema = NullableSingleValueRecordChecker.FindElementSchema(property);
        if (elementSchema is not null)
        {
            CheckInnerValue(property, elementSchema);
        }
    }

    private static void CheckInnerValue(PropertySchemaBase property, RecordPropertySchema recordProperty)
    {
        if (!recordProperty.IsSingleValueRecord())
        {
            return;
        }

        var innerValue = recordProperty.MemberSchemata[0];
        if (innerValue.IsNullable())
        {
            throw new NotSupportedException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.SingleValueRecordInnerMustBeNonNullable,
                property.PropertyName.FullName,
                innerValue.PropertyName.FullName));
        }
    }
}
