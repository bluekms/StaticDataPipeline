using System.Collections.Immutable;
using ScenarioTest.Records.School;
using Sdp.Table;

namespace ScenarioTest.Tables;

public sealed partial class RecommendationTable(ImmutableArray<RecommendationRecord> records)
    : StaticDataTable<RecommendationRecord>(records);
