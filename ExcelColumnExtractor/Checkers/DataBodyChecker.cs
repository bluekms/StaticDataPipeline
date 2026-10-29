using ExcelColumnExtractor.Aggregator;
using ExcelColumnExtractor.Exceptions;
using ExcelColumnExtractor.Mappings;
using Microsoft.Extensions.Logging;
using SchemaInfoScanner.Catalogs;
using SchemaInfoScanner.Schemata;
using SchemaInfoScanner.Schemata.TypedPropertySchemata;

namespace ExcelColumnExtractor.Checkers;

public static class DataBodyChecker
{
    public static void Check(
        MetadataCatalogs metadataCatalogs,
        ExtractedTableMap extractedTableMap,
        ILogger<Program> logger)
    {
        var failures = new List<ExtractionException.Failure>();

        foreach (var (recordSchema, table) in extractedTableMap.SortedTables)
        {
            try
            {
                CheckTable(metadataCatalogs, recordSchema, table);
            }
            catch (Exception e)
            {
                failures.Add(new(recordSchema, e));
                LogError(logger, recordSchema, e.Message, e);
            }
        }

        if (failures.Count > 0)
        {
            throw new ExtractionException(failures);
        }
    }

    private static void CheckTable(
        MetadataCatalogs metadataCatalogs,
        RecordSchema recordSchema,
        BodyColumnAggregator.ExtractedTable table)
    {
        foreach (var row in table.Rows)
        {
            var context = CompatibilityContext.CreateNoCollect(metadataCatalogs, row.Data);

            foreach (var propertySchema in recordSchema.PropertySchemata)
            {
                propertySchema.CheckCompatibility(context);
            }
        }
    }

    private static readonly Action<ILogger, RecordSchema, string, Exception?> LogError =
        LoggerMessage.Define<RecordSchema, string>(LogLevel.Error, new EventId(0, nameof(DataBodyChecker)), "{RecordSchema}: {ErrorMessage}");
}
