using System.Collections.Immutable;
using ScenarioTest.Records.School;
using Sdp.Table;

namespace ScenarioTest.Tables;

public sealed partial class ClubTable(ImmutableArray<ClubRecord> records)
    : StaticDataTable<ClubRecord>(records);
