namespace SchemaInfoScanner.Catalogs;

public sealed class MetadataCatalogs(RecordSchemaCatalog recordSchemaCatalog)
{
    public RecordSchemaCatalog RecordSchemaCatalog { get; } = recordSchemaCatalog;
}
