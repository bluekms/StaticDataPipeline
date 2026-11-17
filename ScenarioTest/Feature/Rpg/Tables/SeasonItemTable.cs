using System.Collections.Immutable;
using ScenarioTest.Records.Rpg;
using Sdp.Table;

namespace ScenarioTest.Tables;

public sealed partial class SeasonItemTable(ImmutableArray<SeasonItemRecord> records)
    : StaticDataTable<SeasonItemRecord>(records);
