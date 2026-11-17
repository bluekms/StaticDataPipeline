using System.Collections.Immutable;
using ScenarioTest.Records.Rpg;
using Sdp.Table;

namespace ScenarioTest.Tables;

public sealed partial class StageServerTable : StaticDataTable<StageServerRecord>
{
    private readonly UniqueIndex<StageServerRecord, StageServerRecord.StageId> byId;

    public StageServerTable(ImmutableArray<StageServerRecord> records)
        : base(records)
    {
        byId = new UniqueIndex<StageServerRecord, StageServerRecord.StageId>(records, x => x.Id);
    }

    public StageServerRecord Get(StageServerRecord.StageId id)
    {
        return byId.Get(id);
    }
}
