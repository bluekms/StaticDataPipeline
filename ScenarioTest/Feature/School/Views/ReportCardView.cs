using System.Diagnostics.CodeAnalysis;
using ScenarioTest.Managers;
using ScenarioTest.Records.School;
using Sdp.Table;
using Sdp.View;

namespace ScenarioTest.Views;

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
