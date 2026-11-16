using System.Collections.Immutable;
using ScenarioTest.Records.School;
using Sdp.Table;

namespace ScenarioTest.Tables;

public sealed partial class LoanTable(ImmutableArray<LoanRecord> records)
    : StaticDataTable<LoanRecord>(records);
