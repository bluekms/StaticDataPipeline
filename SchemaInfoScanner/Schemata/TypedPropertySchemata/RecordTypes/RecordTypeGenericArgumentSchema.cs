using SchemaInfoScanner.NameObjects;

namespace SchemaInfoScanner.Schemata.TypedPropertySchemata.RecordTypes;

public sealed record RecordTypeGenericArgumentSchema(
    RecordTypeGenericArgumentSchema.CollectionKind ContainingType,
    PropertySchemaBase NestedSchema)
{
    public enum CollectionKind
    {
        Array,
        HashSet,
        DictionaryKey,
        DictionaryValue,
    }

    public PropertyName PropertyName { get; } = NestedSchema.PropertyName;
    public string Name { get; } = $"{NestedSchema.PropertyName.Name}'s <{NestedSchema.GetType().Name}>";
    public string FullName { get; } = $"{NestedSchema.PropertyName.FullName}'s <{NestedSchema.GetType().Name}>";

    public void CheckCompatibility(CompatibilityContext context)
    {
        if (NestedSchema is RecordPropertySchema recordSchema)
        {
            recordSchema.CheckMembers(context);

            return;
        }

        NestedSchema.CheckCompatibility(context);
    }
}
