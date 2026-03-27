using FluentValidation;
using SchemaInfoScanner.Extensions;
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
                    $"{x.PropertyName.FullName}({x.GetType().FullName}): {nameof(TimeSpan)}이 아니므로 {nameof(TimeSpanFormatAttribute)}를 사용할 수 없습니다.");
        });

        When(x => x is TimeSpanPropertySchema, () =>
        {
            RuleFor(x => x)
                .Must(x => x.HasAttribute<TimeSpanFormatAttribute>())
                .WithMessage(x =>
                    $"{x.PropertyName.FullName}({x.GetType().FullName}): {nameof(TimeSpan)} 타입은 {nameof(TimeSpanFormatAttribute)}를 사용해야 합니다.");
        });
    }

    private static bool IsTimeSpanCollection(PropertySchemaBase property)
    {
        if (MapTypeChecker.HasTimeSpanProperty(property.NamedTypeSymbol))
        {
            return true;
        }

        if (!CollectionTypeChecker.IsPrimitiveCollection(property.NamedTypeSymbol))
        {
            return false;
        }

        var typeArgument = property.NamedTypeSymbol.TypeArguments.Single();
        return PrimitiveTypeChecker.IsTimeSpanType(typeArgument);
    }
}
