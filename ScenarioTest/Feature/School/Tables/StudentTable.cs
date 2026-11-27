using System.Collections.Immutable;
using ScenarioTest.Records.School;
using Sdp.Table;

namespace ScenarioTest.Tables;

public sealed partial class StudentTable(ImmutableArray<StudentRecord> records)
    : StaticDataTable<StudentRecord>(records);
