using FluentValidation;
using SchemaInfoScanner.Extensions;
using SchemaInfoScanner.Resources;
using Sdp.Attributes;

namespace SchemaInfoScanner.Schemata.SchemaValidators;

internal partial class SchemaRuleValidator
{
    private void RegisterCountRangeAttributeRule()
    {
        When(x => x.HasAttribute<CountRangeAttribute>(), () =>
        {
            RuleFor(x => x)
                .Must(x => x.HasAttribute<SingleColumnCollectionAttribute>())
                .WithMessage(_ => Messages.CountRangeAttributeOnlyForSingleColumnCollection);

            RuleFor(x => x)
                .Must(x => !x.HasAttribute<LengthAttribute>())
                .WithMessage(_ => Messages.CountRangeAndLengthMutuallyExclusive);
        });
    }
}
