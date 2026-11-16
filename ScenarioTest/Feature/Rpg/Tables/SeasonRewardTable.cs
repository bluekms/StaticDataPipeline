using System.Collections.Immutable;
using ScenarioTest.Records.Rpg;
using Sdp.Table;

namespace ScenarioTest.Tables;

public sealed partial class SeasonRewardTable(ImmutableArray<SeasonRewardRecord> records)
    : StaticDataTable<SeasonRewardRecord>(records);
