using FluentValidation;
using SchemaInfoScanner.Extensions;
using SchemaInfoScanner.TypeCheckers;
using Sdp.Attributes;

namespace SchemaInfoScanner.Schemata.SchemaValidators;

internal partial class SchemaRuleValidator
{
    private void RegisterLengthAttributeRule()
    {
        When(IsMultiColumnCollectionType, () =>
        {
            RuleFor(x => x)
                .Must(x => x.HasAttribute<LengthAttribute>())
                .WithMessage(x => $"{x.PropertyName.Name}({x.GetType().FullName}): 컬렉션 타입에는 반드시 {nameof(LengthAttribute)}를 사용해야 합니다.");
        });
    }

    private static bool IsMultiColumnCollectionType(PropertySchemaBase property)
    {
        if (!CollectionTypeChecker.IsSupportedCollectionType(property.NamedTypeSymbol))
        {
            return false;
        }

        return !property.HasAttribute<SingleColumnCollectionAttribute>();
    }
}
