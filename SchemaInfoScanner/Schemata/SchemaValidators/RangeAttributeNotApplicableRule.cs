using System.Globalization;
using FluentValidation;
using SchemaInfoScanner.Extensions;
using SchemaInfoScanner.Resources;
using SchemaInfoScanner.Schemata.TypedPropertySchemata.PrimitiveTypes;
using SchemaInfoScanner.Schemata.TypedPropertySchemata.PrimitiveTypes.NullableTypes;
using Sdp.Attributes;

namespace SchemaInfoScanner.Schemata.SchemaValidators;

internal partial class SchemaRuleValidator
{
    private void RegisterRangeAttributeNotApplicableRule()
    {
        When(x => x.HasAttribute<RangeAttribute>(), () =>
        {
            RuleFor(x => x)
                .Must(IsRangeApplicable)
                .WithMessage(x =>
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Messages.Composite.RangeAttributeNotApplicable,
                        x.PropertyName.FullName,
                        x.GetType().FullName));
        });
    }

    private static bool IsRangeApplicable(PropertySchemaBase property)
    {
        return property
            is BytePropertySchema or NullableBytePropertySchema
            or SBytePropertySchema or NullableSBytePropertySchema
            or Int16PropertySchema or NullableInt16PropertySchema
            or UInt16PropertySchema or NullableUInt16PropertySchema
            or Int32PropertySchema or NullableInt32PropertySchema
            or UInt32PropertySchema or NullableUInt32PropertySchema
            or Int64PropertySchema or NullableInt64PropertySchema
            or UInt64PropertySchema or NullableUInt64PropertySchema
            or FloatPropertySchema or NullableFloatPropertySchema
            or DoublePropertySchema or NullableDoublePropertySchema
            or DecimalPropertySchema or NullableDecimalPropertySchema
            or CharPropertySchema or NullableCharPropertySchema
            or DateTimePropertySchema or NullableDateTimePropertySchema
            or DateTimeOffsetPropertySchema or NullableDateTimeOffsetPropertySchema
            or DateOnlyPropertySchema or NullableDateOnlyPropertySchema
            or TimeOnlyPropertySchema or NullableTimeOnlyPropertySchema
            or TimeSpanPropertySchema or NullableTimeSpanPropertySchema
            or StringPropertySchema or NullableStringPropertySchema
            or EnumPropertySchema or NullableEnumPropertySchema;
    }
}
