using Sdp.Resources;

namespace Sdp.Manager;

internal static class ReferenceValidator
{
    internal static void Validate<TTableSet>(TTableSet tableSet)
        where TTableSet : class
    {
        var tableMap = ForeignKeyResolver.BuildTableMap(tableSet);
        var schemaErrors = new List<Exception>();
        var fkChecksByType = new Dictionary<Type, List<ForeignKeyValidator.FkCheck>>();
        var switchFkChecksByType = new Dictionary<Type, List<SwitchForeignKeyValidator.SwitchFkCheck>>();

        foreach (var table in tableMap.Values)
        {
            var recordType = table.RecordType;
            if (fkChecksByType.ContainsKey(recordType))
            {
                continue;
            }

            fkChecksByType[recordType] = ForeignKeyValidator.BuildChecks(recordType, tableMap, schemaErrors);
            switchFkChecksByType[recordType] = SwitchForeignKeyValidator.BuildChecks(recordType, tableMap, schemaErrors);
        }

        if (schemaErrors.Count > 0)
        {
            throw new AggregateException(Messages.FkValidationFailed, schemaErrors);
        }

        var dataErrors = new List<Exception>();
        var cache = new Dictionary<ForeignKeyResolver.TargetColumn, HashSet<object?>>();

        foreach (var table in tableMap.Values)
        {
            var recordType = table.RecordType;
            var fkChecks = fkChecksByType[recordType];
            var switchFkChecks = switchFkChecksByType[recordType];

            if (fkChecks.Count is 0 && switchFkChecks.Count is 0)
            {
                continue;
            }

            foreach (var record in table.GetAllRecords())
            {
                foreach (var fkCheck in fkChecks)
                {
                    ForeignKeyValidator.ValidateRecord(record, fkCheck, recordType, dataErrors, cache);
                }

                foreach (var switchFkCheck in switchFkChecks)
                {
                    SwitchForeignKeyValidator.ValidateRecord(record, switchFkCheck, recordType, dataErrors, cache);
                }
            }
        }

        if (dataErrors.Count > 0)
        {
            throw new AggregateException(Messages.FkValidationFailed, dataErrors);
        }
    }
}
