using System.Collections.Immutable;
using ScenarioTest.Records.Rpg;
using Sdp.Table;

namespace ScenarioTest.Tables;

public sealed partial class SeasonPassTable(ImmutableArray<SeasonPassRecord> records)
    : StaticDataTable<SeasonPassRecord>(records);
