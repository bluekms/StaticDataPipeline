using FluentValidation;
using SchemaInfoScanner.Extensions;
using SchemaInfoScanner.Schemata.TypedPropertySchemata.PrimitiveTypes;
using Sdp.Attributes;

namespace SchemaInfoScanner.Schemata.SchemaValidators;

internal partial class SchemaRuleValidator
{
    private void RegisterRegularExpressionAttributeRule()
    {
        When(x => x.HasAttribute<RegularExpressionAttribute>(), () =>
        {
            RuleFor(x => x)
                .Must(x => x is StringPropertySchema)
                .WithMessage(x =>
                    $"{x.PropertyName.FullName}({x.GetType().FullName}): string이 아니므로 {nameof(RegularExpressionAttribute)}를 사용할 수 없습니다.");
        });
    }
}
