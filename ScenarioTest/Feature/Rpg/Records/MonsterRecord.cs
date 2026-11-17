using System.Collections.Immutable;
using Sdp.Attributes;

namespace ScenarioTest.Records.Rpg;

// 몬스터. Drops 요소 안의 FK(Drops[i].ItemId)와 한 셀 배열 Skills("-" 가 null)가 FK 메시지의 원소 번호 시나리오 입력이다.
// 고블린의 Id 칸은 이름 Goblin 대신 숫자 1002 로 적었다. enum 칸은 멤버 이름과 숫자를 둘 다 받는다.
[StaticDataRecord("Rpg", "Monsters")]
public sealed partial record MonsterRecord(
    [Key]
    [Range(typeof(MonsterId), "1000", "9999")]
    MonsterId Id,

    string Name,

    Spawn Spawn,

    [Length(2)]
    [NullString("")]
    ImmutableArray<Element?> Weakness,

    [Length(2)] ImmutableArray<Drop> Drops,

    [NullString("-")]
    [SingleColumnCollection(",")]
    [ForeignKey("SkillTable", "Id")]
    ImmutableArray<SkillId?> Skills,

    long Exp,

    [TimeSpanFormat(@"mm\:ss")] TimeSpan RespawnAt);
