using Microsoft.Extensions.Logging;
using SchemaInfoScanner;
using SchemaInfoScanner.Catalogs;
using SchemaInfoScanner.Collectors;
using SchemaInfoScanner.Schemata;

namespace UnitTest.Utility;

public static class SimpleCordParser
{
    public sealed partial record Result(RecordSchemaCatalog RecordSchemaCatalog, IReadOnlyList<RecordSchema> RawRecordSchemata);

    public static Result Parse(string code, ILogger logger)
    {
        var loadResult = RecordSchemaLoader.OnLoad(code, logger);
        var recordSchemaSet = new RecordSchemaSet(loadResult, logger);
        var recordSchemaCatalog = new RecordSchemaCatalog(recordSchemaSet);

        RecordComplianceChecker.Check(recordSchemaCatalog, logger);

        return new(recordSchemaCatalog, recordSchemaCatalog.StaticDataRecordSchemata);
    }
}
