using System.Collections.Immutable;
using ScenarioTest.Records.School;
using Sdp.Table;

namespace ScenarioTest.Tables;

public sealed partial class ClassRoomTable(ImmutableArray<ClassRoomRecord> records)
    : StaticDataTable<ClassRoomRecord>(records);
