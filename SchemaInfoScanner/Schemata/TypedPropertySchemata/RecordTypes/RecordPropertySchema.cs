using Microsoft.CodeAnalysis;
using SchemaInfoScanner.NameObjects;
using SchemaInfoScanner.Schemata.AttributeCheckers;
using SchemaInfoScanner.TypeCheckers;

namespace SchemaInfoScanner.Schemata.TypedPropertySchemata.RecordTypes;

public sealed record RecordPropertySchema(
    PropertyName PropertyName,
    INamedTypeSymbol NamedTypeSymbol,
    IReadOnlyList<AttributeInfo> AttributeList,
    IReadOnlyList<PropertySchemaBase> MemberSchemata,
    bool AllowsNull = false)
    : PropertySchemaBase(PropertyName, NamedTypeSymbol, AttributeList)
{
    // 값이 여럿인 record 의 ? 는 SupportedTypeChecker 가 NullableRecordNotSupported 로 거부한다.
    public override bool IsNullable()
    {
        return AllowsNull && IsSingleValueRecord();
    }

    public bool IsSingleValueRecord()
    {
        return RecordTypeChecker.IsSingleValueRecord(NamedTypeSymbol, MemberSchemata);
    }

    protected override void OnCheckCompatibility(CompatibilityContext context)
    {
        var previousPath = context.EnterPath(ColumnName + ".");
        CheckMembers(context);
        context.RestorePath(previousPath);
    }

    public void CheckMembers(CompatibilityContext context)
    {
        if (IsNullable())
        {
            var result = NullStringAttributeChecker.Check(this, context.Current.Value);
            if (result.IsNull)
            {
                context.ConsumeNull();

                return;
            }
        }

        foreach (var schema in MemberSchemata)
        {
            schema.CheckCompatibility(context);
        }
    }
}
