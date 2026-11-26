using System.Collections.Immutable;
using ScenarioTest.Records.Rpg;
using Sdp.Table;

namespace ScenarioTest.Tables;

public sealed partial class MonsterTable : StaticDataTable<MonsterRecord>
{
    private readonly UniqueIndex<MonsterRecord, MonsterId> byId;

    public MonsterTable(ImmutableArray<MonsterRecord> records)
        : base(records)
    {
        byId = new UniqueIndex<MonsterRecord, MonsterId>(records, x => x.Id);
    }

    public MonsterRecord Get(MonsterId id)
    {
        return byId.Get(id);
    }
}
