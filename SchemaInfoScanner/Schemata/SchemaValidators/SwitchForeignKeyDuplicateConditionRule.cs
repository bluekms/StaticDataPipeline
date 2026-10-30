using System.Globalization;
using FluentValidation;
using SchemaInfoScanner.Extensions;
using SchemaInfoScanner.Resources;
using Sdp.Attributes;

namespace SchemaInfoScanner.Schemata.SchemaValidators;

internal partial class SchemaRuleValidator
{
    private const string SwitchForeignKeyAttributeName = "SwitchForeignKey";

    private void RegisterSwitchForeignKeyDuplicateConditionRule()
    {
        When(x => x.HasAttribute<SwitchForeignKeyAttribute>(), () =>
        {
            RuleFor(x => x)
                .Custom((property, context) =>
                {
                    foreach (var dup in FindDuplicateSwitchForeignKeyConditions(property.AttributeList))
                    {
                        context.AddFailure(string.Format(
                            CultureInfo.CurrentCulture,
                            Messages.Composite.SwitchForeignKeyDuplicateCondition,
                            property.PropertyName.FullName,
                            dup.Column,
                            dup.Value));
                    }
                });
        });
    }

    private static List<(string Column, string Value)> FindDuplicateSwitchForeignKeyConditions(
        IReadOnlyList<AttributeInfo> attributeList)
    {
        return attributeList
            .Where(a => a.Name == SwitchForeignKeyAttributeName)
            .Where(a => a.Arguments.Count >= 2)
            .Select(a => (
                Column: a.Arguments[0].Text,
                Value: a.Arguments[1].Text))
            .GroupBy(c => c)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
    }
}
