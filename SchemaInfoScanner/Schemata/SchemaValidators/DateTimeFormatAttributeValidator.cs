using FluentValidation;
using Microsoft.CodeAnalysis;
using SchemaInfoScanner.Extensions;
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
                    $"{x.PropertyName.FullName}({x.GetType().FullName}): {nameof(DateTime)}이 아니므로 {nameof(DateTimeFormatAttribute)}를 사용할 수 없습니다.");
        });

        When(x => x is DateTimePropertySchema, () =>
        {
            RuleFor(x => x)
                .Must(x => x.HasAttribute<DateTimeFormatAttribute>())
                .WithMessage(x =>
                    $"{x.PropertyName.FullName}({x.GetType().FullName}): {nameof(DateTime)} 타입은 반드시 {nameof(DateTimeFormatAttribute)}를 사용해야 합니다.");
        });
    }

    private static bool IsDateTimeCollection(PropertySchemaBase property)
    {
        if (MapTypeChecker.HasDateTimeProperty(property.NamedTypeSymbol))
        {
            return true;
        }

        if (!CollectionTypeChecker.IsPrimitiveCollection(property.NamedTypeSymbol))
        {
            return false;
        }

        var typeArgument = property.NamedTypeSymbol.TypeArguments.Single();
        return PrimitiveTypeChecker.IsDateTimeType(typeArgument);
    }
}
