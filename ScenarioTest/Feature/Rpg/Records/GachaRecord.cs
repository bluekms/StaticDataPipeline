using System.Collections.Frozen;
using Sdp.Attributes;

namespace ScenarioTest.Records.Rpg;

// GachaTable.Validate 가 확률 합 100 을 잡는 입력이다. 브랜딩 요소 집합 PickupItemIds 의 시트 헤더는 PickupItemIds[0] 으로 접힌다(.Value 없음).
[StaticDataRecord("Rpg", "Gacha")]
public sealed partial record GachaRecord(
    [Key] GachaGrade Grade,

    [Range(typeof(decimal), "0.001", "100")] decimal Rate,

    [Length(2)]
    [ForeignKey("ItemTable", "Id")]
    FrozenSet<ItemRecord.ItemId> PickupItemIds);
