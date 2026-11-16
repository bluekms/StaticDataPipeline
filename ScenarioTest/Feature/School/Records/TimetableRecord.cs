using Sdp.Attributes;

namespace ScenarioTest.Records.School;

// 시간표. StartTime 은 픽스처에 Excel 시간 셀과 텍스트 셀을 섞어 넣어 같은 CSV 값이 되는지 본다. TimetableTable.Validate 가 교사의 같은 시간 중복 배정을 잡는다.
[StaticDataRecord("School", "Timetable")]
public sealed partial record TimetableRecord(
    [Key] Slot Slot,
    [DateTimeFormat("HH:mm")] [Range(typeof(TimeOnly), "08:00", "17:00")] TimeOnly StartTime,
    SubjectCode Subject,
    [ForeignKey("TeacherTable", "Id")] TeacherRecord.TeacherId TeacherId);
