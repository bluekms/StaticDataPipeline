using Sdp.Attributes;

namespace ScenarioTest.Records.School;

// 교실. 담임 교사가 두 반을 맡는 경우를 SchoolStaticData.Validate 가 잡는다.
[StaticDataRecord("School", "ClassRooms")]
public sealed partial record ClassRoomRecord(
    [Key] ClassRoomRecord.ClassRoomId Id,
    [Range(1, 3)] byte Grade,
    [Range(-1, 5)] sbyte Floor,
    [ForeignKey("TeacherTable", "Id")] TeacherRecord.TeacherId HomeroomTeacherId)
{
    public record struct ClassRoomId(int Value);
}
