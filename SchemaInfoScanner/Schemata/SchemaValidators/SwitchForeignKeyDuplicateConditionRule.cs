using System.Globalization;
using FluentValidation;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
        IReadOnlyList<AttributeSyntax> attributeList)
    {
        return attributeList
            .Where(a => a.Name.ToString() == SwitchForeignKeyAttributeName)
            .Where(a => a.ArgumentList is not null)
            .Select(a => (
                Column: a.ArgumentList!.Arguments[0].ToString().Trim('"'),
                Value: a.ArgumentList!.Arguments[1].ToString().Trim('"')))
            .GroupBy(c => c)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
    }
}
