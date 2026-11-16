using System.Collections.Immutable;
using ScenarioTest.Records.School;
using Sdp.Table;

namespace ScenarioTest.Tables;

public sealed partial class StudentTable : StaticDataTable<StudentRecord>
{
    private readonly UniqueIndex<StudentRecord, StudentRecord.StudentId> byId;

    public StudentTable(ImmutableArray<StudentRecord> records)
        : base(records)
    {
        byId = new UniqueIndex<StudentRecord, StudentRecord.StudentId>(records, x => x.Id);
    }

    public StudentRecord Get(StudentRecord.StudentId id)
    {
        return byId.Get(id);
    }
}
