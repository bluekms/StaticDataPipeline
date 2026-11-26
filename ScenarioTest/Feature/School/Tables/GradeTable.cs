using System.Collections.Immutable;
using ScenarioTest.Records.School;
using Sdp.Table;

namespace ScenarioTest.Tables;

public sealed partial class GradeTable(ImmutableArray<GradeRecord> records)
    : StaticDataTable<GradeRecord>(records);
