using System.Globalization;
using FluentValidation;
using SchemaInfoScanner.Extensions;
using SchemaInfoScanner.Resources;
using Sdp.Attributes;

namespace SchemaInfoScanner.Schemata.SchemaValidators;

internal partial class SchemaRuleValidator
{
    private void RegisterDisallowNullableKeyRule()
    {
        When(x => AttributeAccessors.HasAttribute<KeyAttribute>(x.AttributeList), () =>
        {
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
