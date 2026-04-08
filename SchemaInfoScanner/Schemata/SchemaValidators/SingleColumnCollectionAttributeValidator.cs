using System.Globalization;
using FluentValidation;
using SchemaInfoScanner.Extensions;
using SchemaInfoScanner.Resources;
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
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Messages.Composite.SingleColumnCollectionNotForUnsupportedCollection,
                        x.PropertyName.FullName,
                        x.GetType().FullName));

            RuleFor(x => x)
                .Must(x => !MapTypeChecker.IsSupportedMapType(x.NamedTypeSymbol))
                .WithMessage(x =>
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Messages.Composite.SingleColumnCollectionNotForMap,
                        x.PropertyName.FullName,
                        x.GetType().FullName));
        });
    }
}
