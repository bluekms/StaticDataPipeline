using System.Collections.Immutable;
using ScenarioTest.Records.School;
using Sdp.Table;

namespace ScenarioTest.Tables;

public sealed partial class ClassRoomTable : StaticDataTable<ClassRoomRecord>
{
    private readonly UniqueIndex<ClassRoomRecord, ClassRoomRecord.ClassRoomId> byId;

    public ClassRoomTable(ImmutableArray<ClassRoomRecord> records)
        : base(records)
    {
        byId = new UniqueIndex<ClassRoomRecord, ClassRoomRecord.ClassRoomId>(records, x => x.Id);
    }

    public ClassRoomRecord Get(ClassRoomRecord.ClassRoomId id)
    {
        return byId.Get(id);
    }
}
