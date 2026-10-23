using System.Buffers;
using System.Globalization;
using FluentValidation;
using SchemaInfoScanner.Extensions;
using SchemaInfoScanner.Resources;
using Sdp.Attributes;

namespace SchemaInfoScanner.Schemata.SchemaValidators;

internal partial class SchemaRuleValidator
{
    private static readonly SearchValues<char> ForbiddenColumnNameChars =
        SearchValues.Create([',', '"', '.', '[', ']', '\n', '\r']);

    private void RegisterColumnNameAttributeRule()
    {
        When(x => x.HasAttribute<ColumnNameAttribute>(), () =>
        {
            RuleFor(x => x)
                .Must(x => !ContainsForbiddenCharacter(GetColumnName(x)))
                .WithMessage(x =>
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Messages.Composite.ColumnNameContainsForbiddenCharacter,
                        x.PropertyName.FullName,
                        GetColumnName(x)));
        });
    }

    private static string GetColumnName(PropertySchemaBase property)
        => property.GetAttributeValue<ColumnNameAttribute, string>(0);

    private static bool ContainsForbiddenCharacter(string columnName)
        => columnName.AsSpan().ContainsAny(ForbiddenColumnNameChars);
}
