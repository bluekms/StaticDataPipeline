using System.Globalization;
using FluentValidation;
using Microsoft.CodeAnalysis;
using SchemaInfoScanner.Extensions;
using SchemaInfoScanner.Resources;
using SchemaInfoScanner.Schemata.TypedPropertySchemata.PrimitiveTypes;
using SchemaInfoScanner.Schemata.TypedPropertySchemata.PrimitiveTypes.NullableTypes;
using SchemaInfoScanner.TypeCheckers;
using Sdp.Attributes;

namespace SchemaInfoScanner.Schemata.SchemaValidators;

internal partial class SchemaRuleValidator
{
    private void RegisterDateTimeFormatAttributeRule()
    {
        When(x => x.HasAttribute<DateTimeFormatAttribute>(), () =>
        {
            RuleFor(x => x)
                .Must(x =>
                    IsDateTimeFormatCollection(x) ||
                    IsDateTimeFormatPropertySchema(x))
                .WithMessage(x =>
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Messages.Composite.DateTimeFormatAttributeNotApplicable,
                        x.PropertyName.FullName,
                        x.GetType().FullName));
        });

        When(x => IsDateTimeFormatRequired(x), () =>
        {
            RuleFor(x => x)
                .Must(x => x.HasAttribute<DateTimeFormatAttribute>())
                .WithMessage(x =>
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Messages.Composite.DateTimeFormatAttributeRequired,
                        x.PropertyName.FullName,
                        x.GetType().FullName));
        });
    }

    private static bool IsDateTimeFormatRequired(PropertySchemaBase property)
    {
        if (IsDateTimeFormatPropertySchema(property))
        {
            return true;
        }

        return IsPrimitiveDateTimeFormatCollection(property);
    }

    private static bool IsDateTimeFormatPropertySchema(PropertySchemaBase property)
    {
        return property
            is DateTimePropertySchema or NullableDateTimePropertySchema
            or DateTimeOffsetPropertySchema or NullableDateTimeOffsetPropertySchema
            or DateOnlyPropertySchema or NullableDateOnlyPropertySchema
            or TimeOnlyPropertySchema or NullableTimeOnlyPropertySchema;
    }

    private static bool IsDateTimeFormatCollection(PropertySchemaBase property)
    {
        if (MapTypeChecker.HasDateTimeFormatProperty(property.NamedTypeSymbol))
        {
            return true;
        }

        return IsPrimitiveDateTimeFormatCollection(property);
    }

    private static bool IsPrimitiveDateTimeFormatCollection(PropertySchemaBase property)
    {
        if (!CollectionTypeChecker.IsPrimitiveCollection(property.NamedTypeSymbol))
        {
            return false;
        }

        var typeArgument = property.NamedTypeSymbol.TypeArguments.Single();

        return PrimitiveTypeChecker.IsDateTimeFormatType(typeArgument);
    }
}
