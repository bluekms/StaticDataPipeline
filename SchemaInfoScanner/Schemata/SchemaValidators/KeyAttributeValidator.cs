using System.Globalization;
using FluentValidation;
using SchemaInfoScanner.Extensions;
using SchemaInfoScanner.Resources;
using SchemaInfoScanner.TypeCheckers;
using Sdp.Attributes;

namespace SchemaInfoScanner.Schemata.SchemaValidators;

internal partial class SchemaRuleValidator
{
    private void RegisterDisallowNullableKeyRule()
    {
        When(x => AttributeAccessors.HasAttribute<KeyAttribute>(x.AttributeList), () =>
        {
            RuleFor(x => x)
                .Must(x => !CollectionTypeChecker.IsSupportedCollectionType(x.NamedTypeSymbol))
                .WithMessage(x =>
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Messages.Composite.KeyAttributeNotForCollection,
                        x.PropertyName.FullName,
                        x.GetType().FullName));

            RuleFor(x => x)
                .Must(x => !x.IsNullable())
                .WithMessage(x =>
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Messages.Composite.KeyAttributeMustBeNonNullable,
                        x.PropertyName.FullName,
                        x.GetType().FullName));
        });
    }
}
