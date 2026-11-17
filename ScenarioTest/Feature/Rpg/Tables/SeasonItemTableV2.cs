using System.Collections.Immutable;
using ScenarioTest.Records.Rpg;
using Sdp.Table;

namespace ScenarioTest.Tables;

public sealed partial class SeasonItemTableV2(ImmutableArray<SeasonItemRecordV2> records)
    : StaticDataTable<SeasonItemRecordV2>(records);
