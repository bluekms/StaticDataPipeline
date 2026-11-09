using System.Collections.Immutable;
using System.Globalization;
using ScenarioTest.Records.School;
using Sdp.Table;

namespace ScenarioTest.Managers;

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

public sealed partial class TimetableTable(ImmutableArray<TimetableRecord> records)
    : StaticDataTable<TimetableRecord>(records)
{
    protected override void Validate()
    {
        var doubleBooked = Records
            .GroupBy(x => new TeacherSlot(x.TeacherId, x.Slot.Day, x.Slot.Period))
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .ToList();

        if (doubleBooked.Count > 0)
        {
            var first = doubleBooked[0];
            throw new InvalidOperationException(FormattableString.Invariant(
                $"교사 {first.TeacherId.Value} 은 {first.Day} {first.Period} 교시에 수업이 둘이다."));
        }
    }

    private sealed record TeacherSlot(TeacherRecord.TeacherId TeacherId, Weekday Day, byte Period);
}

public sealed partial class GradeTable(ImmutableArray<GradeRecord> records)
    : StaticDataTable<GradeRecord>(records);

public sealed partial class CalendarTable(ImmutableArray<CalendarRecord> records)
    : StaticDataTable<CalendarRecord>(records)
{
    protected override void Validate()
    {
        foreach (var record in Records)
        {
            if (record.End is null)
            {
                continue;
            }

            if (record.End.Value < record.Start)
            {
                throw new InvalidOperationException(string.Format(
                    CultureInfo.InvariantCulture,
                    "일정 {0} 의 종료일({1:yyyy-MM-dd})이 시작일({2:yyyy-MM-dd})보다 앞선다.",
                    record.Id,
                    record.End.Value,
                    record.Start));
            }
        }
    }
}

public sealed partial class CafeteriaTable(ImmutableArray<CafeteriaRecord> records)
    : StaticDataTable<CafeteriaRecord>(records);

public sealed partial class ClubTable(ImmutableArray<ClubRecord> records)
    : StaticDataTable<ClubRecord>(records);

public sealed partial class BookTable(ImmutableArray<BookRecord> records)
    : StaticDataTable<BookRecord>(records);

public sealed partial class LoanTable(ImmutableArray<LoanRecord> records)
    : StaticDataTable<LoanRecord>(records);

public sealed partial class ReservationTable(ImmutableArray<ReservationRecord> records)
    : StaticDataTable<ReservationRecord>(records);
