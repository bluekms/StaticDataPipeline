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
                    IsDateTimeCollection(x) ||
                    x is DateTimePropertySchema or NullableDateTimePropertySchema)
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
        if (property is DateTimePropertySchema or NullableDateTimePropertySchema)
        {
            return true;
        }

        return IsPrimitiveDateTimeCollection(property);
    }

    private static bool IsDateTimeCollection(PropertySchemaBase property)
    {
        if (MapTypeChecker.HasDateTimeProperty(property.NamedTypeSymbol))
        {
            return true;
        }

        return IsPrimitiveDateTimeCollection(property);
    }

    private static bool IsPrimitiveDateTimeCollection(PropertySchemaBase property)
    {
        if (!CollectionTypeChecker.IsPrimitiveCollection(property.NamedTypeSymbol))
        {
            return false;
        }

        var typeArgument = property.NamedTypeSymbol.TypeArguments.Single();

        return PrimitiveTypeChecker.IsDateTimeType(typeArgument);
    }
}
