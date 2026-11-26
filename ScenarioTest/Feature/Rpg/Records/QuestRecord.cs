using System.Collections.Frozen;
using Sdp.Attributes;

namespace ScenarioTest.Records.Rpg;

// 퀘스트. PrevQuestId 는 자기 테이블을 가리킨다.
[StaticDataRecord("Rpg", "Quests")]
public sealed partial record QuestRecord(
    [Key] QuestRecord.QuestId Id,

    string Title,

    [NullString("")]
    [ForeignKey("QuestTable", "Id")]
    QuestRecord.QuestId? PrevQuestId,

    KillGoal Kill,

    CollectGoal Collect,

    [Length(2)] FrozenSet<QuestReward> Rewards,

    [Range(1, 99)] byte RequiredLevel,

    [NullString("")] long? BonusGold)
{
    public record struct QuestId(int Value);
}
