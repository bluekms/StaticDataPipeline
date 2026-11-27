using System.Globalization;
using System.Text;
using SchemaInfoScanner.Exceptions;
using SchemaInfoScanner.Extensions;
using SchemaInfoScanner.Resources;
using SchemaInfoScanner.Schemata;
using SchemaInfoScanner.Schemata.TypedPropertySchemata.RecordTypes;
using Sdp.Attributes;

namespace SchemaInfoScanner.TypeCheckers;

// 생성기의 ParameterEmittability.IsNullableBrandingEmittable 과 같은 규칙이어야 한다.
internal static class NullableSingleValueRecordChecker
{
    public static void CheckDirect(PropertySchemaBase property)
    {
        if (property is not RecordPropertySchema recordProperty || !recordProperty.IsNullable())
        {
            throw new NotSupportedException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.NullableRecordNotSupported,
                property.PropertyName.FullName));
        }
    }

    public static void CheckElement(PropertySchemaBase property, CompositeFormat notSupportedMessage)
    {
        var elementSchema = FindElementSchema(property);
        if (elementSchema is null || !elementSchema.IsNullable())
        {
            throw new NotSupportedException(string.Format(
                CultureInfo.CurrentCulture,
                notSupportedMessage,
                property.PropertyName.FullName));
        }

        if (!property.HasAttribute<NullStringAttribute>())
        {
            throw new InvalidAttributeUsageException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.NullStringAttributeRequiredForNullable,
                property.PropertyName.FullName,
                property.GetType().FullName));
        }
    }

    public static RecordPropertySchema? FindElementSchema(PropertySchemaBase property)
    {
        var genericArgumentSchema = property switch
        {
            RecordArrayPropertySchema array => array.GenericArgumentSchema,
            RecordSetPropertySchema set => set.GenericArgumentSchema,
            _ => null,
        };

        return genericArgumentSchema?.NestedSchema as RecordPropertySchema;
    }
}
