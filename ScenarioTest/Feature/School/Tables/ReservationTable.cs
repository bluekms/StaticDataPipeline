using System.Collections.Immutable;
using ScenarioTest.Records.School;
using Sdp.Table;

namespace ScenarioTest.Tables;

public sealed partial class ReservationTable(ImmutableArray<ReservationRecord> records)
    : StaticDataTable<ReservationRecord>(records);
