using System.Globalization;
using Microsoft.Extensions.Logging;
using ScenarioTest.Records.School;
using ScenarioTest.Support;
using Sdp.Manager;

namespace ScenarioTest.Managers;

public sealed partial class SchoolStaticData(ILogger logger)
    : StaticDataManager<SchoolStaticData.TableSet, SchoolStaticData.ViewSet>(logger)
{
    public sealed partial record TableSet(
        StudentTable? StudentTable,
        ClassRoomTable? ClassRoomTable,
        TeacherTable? TeacherTable,
        TimetableTable? TimetableTable,
        GradeTable? GradeTable,
        CalendarTable? CalendarTable,
        CafeteriaTable? CafeteriaTable,
        ClubTable? ClubTable,
        BookTable? BookTable,
        LoanTable? LoanTable,
        ReservationTable? ReservationTable);

    public sealed partial record ViewSet(
        ReportCardView ReportCardView,
        TeacherScheduleView TeacherScheduleView);

    // 동시 LoadAsync 거부 시나리오가 첫 로드를 붙잡는 자리. 평소에는 null.
    public LoadGate? Gate { get; set; }

    protected override void Validate(TableSet tableSet)
    {
        Gate?.Hold();

        if (tableSet.ClassRoomTable is not null)
        {
            ValidateHomeroomTeachers(tableSet.ClassRoomTable);
        }

        if (tableSet.StudentTable is not null)
        {
            ValidateSeats(tableSet.StudentTable);
        }
    }

    private static void ValidateHomeroomTeachers(ClassRoomTable classRooms)
    {
        var teacherOfTwoClasses = classRooms.Records
            .GroupBy(x => x.HomeroomTeacherId)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .ToList();

        if (teacherOfTwoClasses.Count > 0)
        {
            throw new InvalidOperationException(string.Format(
                CultureInfo.InvariantCulture,
                "교사 {0} 은 둘 이상의 반의 담임이다.",
                teacherOfTwoClasses[0].Value));
        }
    }

    private static void ValidateSeats(StudentTable students)
    {
        var sharedSeats = students.Records
            .GroupBy(x => new ClassSeat(x.ClassRoomId, x.Seat))
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .ToList();

        if (sharedSeats.Count > 0)
        {
            var first = sharedSeats[0];
            throw new InvalidOperationException(string.Format(
                CultureInfo.InvariantCulture,
                "{2} 반의 자리 ({0}, {1}) 에 학생이 둘이다.",
                first.Seat.Row,
                first.Seat.Col,
                first.ClassRoomId.Value));
        }
    }

    private sealed record ClassSeat(ClassRoomRecord.ClassRoomId ClassRoomId, Seat Seat);
}
