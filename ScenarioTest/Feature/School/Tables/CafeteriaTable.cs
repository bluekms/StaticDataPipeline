using System.Collections.Immutable;
using ScenarioTest.Records.School;
using Sdp.Table;

namespace ScenarioTest.Tables;

public sealed partial class CafeteriaTable(ImmutableArray<CafeteriaRecord> records)
    : StaticDataTable<CafeteriaRecord>(records);
