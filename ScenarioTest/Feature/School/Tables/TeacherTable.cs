using System.Collections.Immutable;
using ScenarioTest.Records.School;
using Sdp.Table;

namespace ScenarioTest.Tables;

public sealed partial class TeacherTable(ImmutableArray<TeacherRecord> records)
    : StaticDataTable<TeacherRecord>(records);
