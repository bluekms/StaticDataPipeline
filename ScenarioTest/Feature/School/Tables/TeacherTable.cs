using System.Collections.Immutable;
using ScenarioTest.Records.School;
using Sdp.Table;

namespace ScenarioTest.Tables;

public sealed partial class TeacherTable : StaticDataTable<TeacherRecord>
{
    private readonly UniqueIndex<TeacherRecord, TeacherRecord.TeacherId> byId;

    public TeacherTable(ImmutableArray<TeacherRecord> records)
        : base(records)
    {
        byId = new UniqueIndex<TeacherRecord, TeacherRecord.TeacherId>(records, x => x.Id);
    }

    public TeacherRecord Get(TeacherRecord.TeacherId id)
    {
        return byId.Get(id);
    }
}
