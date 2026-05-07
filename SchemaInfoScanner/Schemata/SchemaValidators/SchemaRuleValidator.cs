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
        RegisterCountRangeAttributeRule();
        RegisterDateTimeFormatAttributeRule();
        RegisterFkSfkConflictRule();
        RegisterLengthAttributeRule();
        RegisterNullStringAttributeRule();
        RegisterRegularExpressionAttributeRule();
        RegisterSingleColumnCollectionAttributeRule();
        RegisterTimeSpanFormatAttributeRule();
    }
}
