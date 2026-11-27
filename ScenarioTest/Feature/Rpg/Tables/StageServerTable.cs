using System.Collections.Immutable;
using ScenarioTest.Records.Rpg;
using Sdp.Table;

namespace ScenarioTest.Tables;

public sealed partial class StageServerTable(ImmutableArray<StageServerRecord> records)
    : StaticDataTable<StageServerRecord>(records);
