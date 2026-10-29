using System.Globalization;
using FluentValidation;
using SchemaInfoScanner.Extensions;
using SchemaInfoScanner.Resources;
using SchemaInfoScanner.TypeCheckers;
using Sdp.Attributes;

namespace SchemaInfoScanner.Schemata.SchemaValidators;

internal partial class SchemaRuleValidator
{
    private void RegisterLengthAttributeRule()
    {
        When(x => x.HasAttribute<LengthAttribute>(), () =>
        {
            RuleFor(x => x)
                .Must(x => CollectionTypeChecker.IsSupportedCollectionType(x.NamedTypeSymbol))
                .WithMessage(x =>
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Messages.Composite.LengthAttributeNotApplicable,
                        x.PropertyName.FullName,
                        x.GetType().FullName));

            RuleFor(x => x)
                .Must(x => x.GetAttributeValue<LengthAttribute, int>(0) >= 1)
                .WithMessage(_ => Messages.LengthMustBePositive);
        });
    }
}
