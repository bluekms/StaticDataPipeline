using System.Collections.Immutable;
using ScenarioTest.Records.Rpg;
using Sdp.Table;

namespace ScenarioTest.Tables;

public sealed partial class PremiumRewardTable(ImmutableArray<PremiumRewardRecord> records)
    : StaticDataTable<PremiumRewardRecord>(records);
