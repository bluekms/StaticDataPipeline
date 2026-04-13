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
        When(IsMultiColumnCollectionType, () =>
        {
            RuleFor(x => x)
                .Must(x => x.HasAttribute<LengthAttribute>())
                .When(x => !x.HasAttribute<CountRangeAttribute>())
                .WithMessage(x =>
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Messages.Composite.LengthAttributeRequired,
                        x.PropertyName.Name,
                        x.GetType().FullName));
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
