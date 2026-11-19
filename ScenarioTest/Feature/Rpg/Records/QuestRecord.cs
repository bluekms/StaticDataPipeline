using System.Collections.Frozen;
using Sdp.Attributes;

namespace ScenarioTest.Records.Rpg;

// 퀘스트. PrevQuestId 는 자기 테이블을 가리킨다.
[StaticDataRecord("Rpg", "Quests")]
public sealed partial record QuestRecord(
    [Key] QuestRecord.QuestId Id,
    string Title,

    // nullable 브랜딩은 매퍼가 받지 않아 int? 로 두고, 대상 키 QuestId 는 안쪽 값으로 비교된다.
    [NullString("")] [ForeignKey("QuestTable", "Id")] int? PrevQuestId,
    [NullString("")] int? Giver,
    [Length(2)] FrozenSet<Objective> Objectives,
    [Range(1, 99)] byte RequiredLevel,
    [NullString("")] long? BonusGold)
{
    public record struct QuestId(int Value);
}
