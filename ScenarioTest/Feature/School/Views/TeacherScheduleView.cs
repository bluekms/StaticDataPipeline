using System.Globalization;
using ScenarioTest.Managers;
using ScenarioTest.Records.School;
using ScenarioTest.Tables;
using Sdp.Table;
using Sdp.View;

namespace ScenarioTest.Views;

public sealed partial class TeacherScheduleView : StaticDataView<SchoolStaticData.TableSet>
{
    private readonly TeacherTable teachers;
    private readonly MultiIndex<TimetableRecord, TeacherRecord.TeacherId> byTeacher;

    public TeacherScheduleView(SchoolStaticData.TableSet tables)
        : base(tables)
    {
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
