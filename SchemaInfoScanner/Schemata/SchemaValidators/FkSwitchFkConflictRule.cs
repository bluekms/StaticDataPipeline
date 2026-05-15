using System.Globalization;
using FluentValidation;
using SchemaInfoScanner.Extensions;
using SchemaInfoScanner.Resources;
using Sdp.Attributes;

namespace SchemaInfoScanner.Schemata.SchemaValidators;

internal partial class SchemaRuleValidator
{
    private void RegisterFkSwitchFkConflictRule()
    {
        When(x => x.HasAttribute<ForeignKeyAttribute>() && x.HasAttribute<SwitchForeignKeyAttribute>(), () =>
        {
            RuleFor(x => x)
                .Must(_ => false)
                .WithMessage(x =>
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Messages.Composite.FkSwitchFkConflict,
                        x.PropertyName.FullName));
        });
    }
}
