using Sdp.Attributes;

namespace ScenarioTest.Records.Rpg;

// 스킬. Monsters.Skills 의 FK 대상. 시트의 Id 1003 은 enum 에 이름이 없는 숫자 셀이다([Key] enum 은 enum 에 없는 숫자도 받는다).
[StaticDataRecord("Rpg", "Skills")]
public sealed partial record SkillRecord(
    [Key]
    SkillId Id,

    string Name,

    [NullString("-")]
    [TimeSpanFormat("c")]
    [Range(typeof(TimeSpan), "00:00:00", "00:10:00")]
    TimeSpan? Cooldown);
