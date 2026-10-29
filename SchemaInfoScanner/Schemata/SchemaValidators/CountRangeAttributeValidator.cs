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

            RuleFor(x => x)
                .Must(HasNonNegativeMinCount)
                .WithMessage(_ => Messages.CountRangeMinMustBeNonNegative);

            RuleFor(x => x)
                .Must(HasOrderedBounds)
                .WithMessage(_ => Messages.CountRangeMinMustNotExceedMax);
        });
    }

    private static bool HasOrderedBounds(PropertySchemaBase property)
    {
        var minCount = property.GetAttributeValue<CountRangeAttribute, int>(0);
        var maxCount = property.GetAttributeValue<CountRangeAttribute, int>(1);

        return minCount <= maxCount;
    }

    private static bool HasNonNegativeMinCount(PropertySchemaBase property)
    {
        var minCount = property.GetAttributeValue<CountRangeAttribute, int>(0);

        return minCount >= 0;
    }
}
