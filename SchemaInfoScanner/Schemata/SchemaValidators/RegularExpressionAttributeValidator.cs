using System.Globalization;
using FluentValidation;
using SchemaInfoScanner.Extensions;
using SchemaInfoScanner.Resources;
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
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Messages.Composite.RegularExpressionAttributeOnlyForString,
                        x.PropertyName.FullName,
                        x.GetType().FullName));
        });
    }
}
