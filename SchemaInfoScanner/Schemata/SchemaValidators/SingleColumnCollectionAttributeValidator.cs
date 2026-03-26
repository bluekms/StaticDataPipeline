using FluentValidation;
using SchemaInfoScanner.Extensions;
using SchemaInfoScanner.TypeCheckers;
using Sdp.Attributes;

namespace SchemaInfoScanner.Schemata.SchemaValidators;

internal partial class SchemaRuleValidator
{
    private void RegisterSingleColumnCollectionAttributeRule()
    {
        When(x => x.HasAttribute<SingleColumnCollectionAttribute>(), () =>
        {
            RuleFor(x => x)
                .Must(x => CollectionTypeChecker.IsSupportedCollectionType(x.NamedTypeSymbol))
                .WithMessage(x =>
                    $"{x.PropertyName.FullName}({x.GetType().FullName}): 지원되는 컬렉션이 아니기 때문에 {nameof(SingleColumnCollectionAttribute)}를 사용할 수 없습니다.");

            RuleFor(x => x)
                .Must(x => !MapTypeChecker.IsSupportedMapType(x.NamedTypeSymbol))
                .WithMessage(x =>
                    $"{x.PropertyName.FullName}({x.GetType().FullName}): Map 컬렉션에서는 {nameof(SingleColumnCollectionAttribute)}를 사용할 수 없습니다.");
        });
    }
}
