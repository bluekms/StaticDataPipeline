using System.Collections.Immutable;
using ScenarioTest.Records.Rpg;
using Sdp.Table;

namespace ScenarioTest.Tables;

public sealed partial class DebuffTable(ImmutableArray<BuffRecord> records)
    : StaticDataTable<BuffRecord>(records.Where(x => x.IsDebuff).ToImmutableArray());
