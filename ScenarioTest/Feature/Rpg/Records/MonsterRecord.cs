using System.Collections.Immutable;
using Sdp.Attributes;

namespace ScenarioTest.Records.Rpg;

// 몬스터. Drops 요소 안의 FK(Drops[i].ItemId)와 한 셀 배열 Skills("-" 가 null)가 FK 메시지의 원소 번호 시나리오 입력이다.
[StaticDataRecord("Rpg", "Monsters")]
public sealed partial record MonsterRecord(
    [Key] [Range(typeof(MonsterId), "1000", "9999")] MonsterId Id,
    string Name,
    Spawn Spawn,
    [Length(2)] [NullString("")] ImmutableArray<Element?> Weakness,
    [Length(2)] ImmutableArray<Drop> Drops,
    [SingleColumnCollection(",")]
    [NullString("-")]
    [ForeignKey("SkillTable", "Id")]
    ImmutableArray<SkillId?> Skills,
    long Exp,
    [TimeSpanFormat(@"mm\:ss")] TimeSpan RespawnAt);
