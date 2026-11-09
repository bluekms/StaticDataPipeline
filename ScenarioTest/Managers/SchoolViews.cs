using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using ScenarioTest.Records.School;
using Sdp.Table;
using Sdp.View;

namespace ScenarioTest.Managers;

public sealed record ReportCard(StudentRecord Student, ClassRoomRecord ClassRoom, GradeRecord? Grade);

public sealed partial class ReportCardView : StaticDataView<SchoolStaticData.TableSet>
{
    private readonly UniqueIndex<ReportCard, StudentRecord.StudentId> byStudent;
    private readonly MultiIndex<ReportCard, ClassRoomRecord.ClassRoomId> byClassRoom;

    public ReportCardView(SchoolStaticData.TableSet tables)
        : base(tables)
    {
        var cards = Build(tables);
        byStudent = new UniqueIndex<ReportCard, StudentRecord.StudentId>(cards, x => x.Student.Id);
        byClassRoom = new MultiIndex<ReportCard, ClassRoomRecord.ClassRoomId>(cards, x => x.ClassRoom.Id);
    }

    public ReportCard Get(StudentRecord.StudentId studentId)
    {
        return byStudent.Get(studentId);
    }

    public bool TryGet(StudentRecord.StudentId studentId, [NotNullWhen(true)] out ReportCard? reportCard)
    {
        return byStudent.TryGet(studentId, out reportCard);
    }

    public IReadOnlyList<ReportCard> GetByClassRoom(ClassRoomRecord.ClassRoomId classRoomId)
    {
        return byClassRoom.Get(classRoomId);
    }

    private static List<ReportCard> Build(SchoolStaticData.TableSet tables)
    {
        if (tables.StudentTable is null || tables.ClassRoomTable is null || tables.GradeTable is null)
        {
            throw new InvalidOperationException("ReportCardView 에는 StudentTable, ClassRoomTable, GradeTable 이 필요하다.");
        }

        var gradeByStudent = new UniqueIndex<GradeRecord, StudentRecord.StudentId>(tables.GradeTable.Records, x => x.StudentId);

        var cards = new List<ReportCard>();
        foreach (var student in tables.StudentTable.Records)
        {
            gradeByStudent.TryGet(student.Id, out var grade);
            var classRoom = tables.ClassRoomTable.Get(student.ClassRoomId);
            cards.Add(new ReportCard(student, classRoom, grade));
        }

        return cards;
    }
}

public sealed partial class TeacherScheduleView : StaticDataView<SchoolStaticData.TableSet>
{
    private readonly TeacherTable teachers;
    private readonly MultiIndex<TimetableRecord, TeacherRecord.TeacherId> byTeacher;

    public TeacherScheduleView(SchoolStaticData.TableSet tables)
        : base(tables)
    {
        if (tables.TeacherTable is null || tables.TimetableTable is null)
        {
            throw new InvalidOperationException("TeacherScheduleView 에는 TeacherTable, TimetableTable 이 필요하다.");
        }

        teachers = tables.TeacherTable;
        byTeacher = new MultiIndex<TimetableRecord, TeacherRecord.TeacherId>(tables.TimetableTable.Records, x => x.TeacherId);
    }

    public IReadOnlyList<TimetableRecord> Get(TeacherRecord.TeacherId teacherId)
    {
        return byTeacher.Get(teacherId);
    }

    protected override void Validate()
    {
        var idleTeachers = teachers.Records
            .Where(x => byTeacher.Get(x.Id).Count == 0)
            .Select(x => x.Name)
            .ToList();

        if (idleTeachers.Count > 0)
        {
            throw new InvalidOperationException(string.Format(
                CultureInfo.InvariantCulture,
                "수업이 없는 교사: {0}",
                string.Join(", ", idleTeachers)));
        }
    }
}
