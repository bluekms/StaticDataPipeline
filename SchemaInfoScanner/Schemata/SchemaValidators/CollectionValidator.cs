using System.Globalization;
using FluentValidation;
using SchemaInfoScanner.Resources;
using SchemaInfoScanner.TypeCheckers;

namespace SchemaInfoScanner.Schemata.SchemaValidators;

internal partial class SchemaRuleValidator
{
    private void RegisterDisallowNullableCollectionRule()
    {
        When(x => CollectionTypeChecker.IsSupportedCollectionType(x.NamedTypeSymbol), () =>
        {
            RuleFor(x => x)
                .Must(x => !x.IsNullable())
                .WithMessage(x =>
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Messages.Composite.NullableCollectionNotSupported,
                        x.PropertyName.FullName,
                        x.GetType().FullName));
        });
    }
}
