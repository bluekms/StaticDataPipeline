using System.Globalization;
using FluentValidation;
using SchemaInfoScanner.Extensions;
using SchemaInfoScanner.Resources;
using SchemaInfoScanner.Schemata.TypedPropertySchemata.PrimitiveTypes;
using SchemaInfoScanner.Schemata.TypedPropertySchemata.PrimitiveTypes.NullableTypes;
using SchemaInfoScanner.TypeCheckers;
using Sdp.Attributes;

namespace SchemaInfoScanner.Schemata.SchemaValidators;

internal partial class SchemaRuleValidator
{
    private void RegisterTimeSpanFormatAttributeRule()
    {
        When(x => x.HasAttribute<TimeSpanFormatAttribute>(), () =>
        {
            RuleFor(x => x)
                .Must(x =>
                    IsTimeSpanCollection(x) ||
                    x is TimeSpanPropertySchema or NullableTimeSpanPropertySchema)
                .WithMessage(x =>
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Messages.Composite.TimeSpanFormatAttributeNotApplicable,
                        x.PropertyName.FullName,
                        x.GetType().FullName));
        });

        When(x => IsTimeSpanFormatRequired(x), () =>
        {
            RuleFor(x => x)
                .Must(x => x.HasAttribute<TimeSpanFormatAttribute>())
                .WithMessage(x =>
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Messages.Composite.TimeSpanFormatAttributeRequired,
                        x.PropertyName.FullName,
                        x.GetType().FullName));
        });
    }

    private static bool IsTimeSpanFormatRequired(PropertySchemaBase property)
    {
        if (property is TimeSpanPropertySchema or NullableTimeSpanPropertySchema)
        {
            return true;
        }

        return IsPrimitiveTimeSpanCollection(property);
    }

    private static bool IsTimeSpanCollection(PropertySchemaBase property)
    {
        if (MapTypeChecker.HasTimeSpanProperty(property.NamedTypeSymbol))
        {
            return true;
        }

        return IsPrimitiveTimeSpanCollection(property);
    }

    private static bool IsPrimitiveTimeSpanCollection(PropertySchemaBase property)
    {
        if (!CollectionTypeChecker.IsPrimitiveCollection(property.NamedTypeSymbol))
        {
            return false;
        }

        var typeArgument = property.NamedTypeSymbol.TypeArguments.Single();

        return PrimitiveTypeChecker.IsTimeSpanType(typeArgument);
    }
}
