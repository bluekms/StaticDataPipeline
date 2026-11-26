using System.Collections.Immutable;
using ScenarioTest.Records.Rpg;
using Sdp.Table;

namespace ScenarioTest.Tables;

public sealed partial class FreeRewardTable(ImmutableArray<FreeRewardRecord> records)
    : StaticDataTable<FreeRewardRecord>(records);
