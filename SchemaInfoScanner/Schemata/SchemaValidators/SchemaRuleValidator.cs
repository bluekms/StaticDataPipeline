using FluentValidation;

namespace SchemaInfoScanner.Schemata.SchemaValidators;

internal partial class SchemaRuleValidator : AbstractValidator<PropertySchemaBase>
{
    public SchemaRuleValidator()
    {
        // Supported Type Validators
        RegisterDisallowNullableKeyRule();
        RegisterDisallowNullableCollectionRule();

        // Attribute Validators
        RegisterColumnNameAttributeRule();
        RegisterCountRangeAttributeRule();
        RegisterDateTimeFormatAttributeRule();
        RegisterFkSwitchFkConflictRule();
        RegisterLengthAttributeRule();
        RegisterSwitchForeignKeyDuplicateConditionRule();
        RegisterNullStringAttributeRule();
        RegisterRangeAttributeNotApplicableRule();
        RegisterRegularExpressionAttributeRule();
        RegisterSingleColumnCollectionAttributeRule();
        RegisterTimeSpanFormatAttributeRule();
    }
}
