using System.Collections.Frozen;
using System.Collections.Immutable;
using Sdp.Attributes;

namespace ScenarioTest.Records.Rpg;

public enum Rarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary,
}

public enum EquipSlot
{
    Weapon,
    Armor,
    Accessory,
}

public enum SkillId
{
    Fireball = 1001,
    Heal = 1002,
}

public enum MonsterId
{
    Slime = 1001,
    BossDragon = 9001,
}

public enum Element
{
    Fire,
    Ice,
    Holy,
    Dark,
}

public enum RewardKind
{
    Item,
    Currency,
}

public enum ObjectiveType
{
    Kill,
    Collect,
    Talk,
}

public enum PurchaseKind
{
    Normal,
    Sale,
}

public enum GachaGrade
{
    SSR,
    SR,
    R,
}

public record struct MapId(int Value);

public record struct NpcId([NullString("")] int? Value);

public sealed record Stats(int Atk, int Def, [ColumnName("Spd")] float Speed);

public sealed record Position(int X, int Y);

public sealed record Spawn(MapId Map, Position Pos);

public sealed record Drop([ForeignKey("ItemTable", "Id")] int ItemId, [Range(typeof(decimal), "0", "100")] decimal Rate);

public sealed record Objective(ObjectiveType Type, int TargetId, [Range(1, 999)] int Count);

public sealed record PriceKey([ForeignKey("CurrencyTable", "Id")] int Currency, PurchaseKind Kind);

public sealed record Price([Key] PriceKey Key, uint Amount, [NullString("")] string? Note);

// 아이템. TableSet 에서 유일한 non-nullable 테이블이고 FK 대상이 가장 많다.
// Tags 는 [Length] 없이 시트의 열 수(Tags[0..2])로 길이가 정해지고, [Ignore] SortCache 는 시트에 열이 없다.
[StaticDataRecord("Rpg", "Items")]
public sealed partial record ItemRecord(
    [Key] ItemRecord.ItemId Id,
    string Name,
    string Description,
    [Range(typeof(Rarity), "Common", "Epic")] Rarity Rarity,
    [NullString("")] EquipSlot? Slot,
    [Range(0, 1_000_000)] uint Price,
    [NullString("-")] ushort? MaxStack,
    Stats Stats,
    ImmutableArray<string> Tags,
    [Ignore] int SortCache)
{
    public record struct ItemId([Range(10000, 19999)] int Value);
}

// 재화. SwitchFK 와 다중 FK 의 두 번째 대상이다.
[StaticDataRecord("Rpg", "Currencies")]
public sealed partial record CurrencyRecord([Key] CurrencyRecord.CurrencyId Id, string Name, long Cap)
{
    public record struct CurrencyId([Range(900, 999)] int Value);
}

// 스킬. Monsters.Skills 의 FK 대상. 시트의 Id 1003 은 enum 에 이름이 없는 숫자 셀이다([Key] enum 은 정의되지 않은 값도 받는다).
[StaticDataRecord("Rpg", "Skills")]
public sealed partial record SkillRecord(
    [Key] SkillId Id,
    string Name,
    [TimeSpanFormat("c")] [NullString("-")] [Range(typeof(TimeSpan), "00:00:00", "00:10:00")] TimeSpan? Cooldown);

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

// enum 조건 SwitchFK. 두 대상의 키가 서로 다른 브랜딩(ItemId, CurrencyId)이라 열은 int 로 둔다.
[StaticDataRecord("Rpg", "Rewards")]
public sealed partial record RewardRecord(
    [Key] int Id,
    RewardKind Kind,
    [SwitchForeignKey(nameof(Kind), "Item", "ItemTable", "Id")]
    [SwitchForeignKey(nameof(Kind), "Currency", "CurrencyTable", "Id")]
    int TargetId,
    [Range(typeof(long), "1", "9223372036854775807")] long Amount);

// SeasonPass 의 IsPremium=false 쪽 대상.
[StaticDataRecord("Rpg", "FreeRewards")]
public sealed partial record FreeRewardRecord(
    [Key] FreeRewardRecord.FreeRewardId Id,
    [ForeignKey("CurrencyTable", "Id")] CurrencyRecord.CurrencyId CurrencyId,
    [Range(1, 999_999)] uint Amount)
{
    public record struct FreeRewardId(int Value);
}

// SeasonPass 의 IsPremium=true 쪽 대상.
[StaticDataRecord("Rpg", "PremiumRewards")]
public sealed partial record PremiumRewardRecord(
    [Key] PremiumRewardRecord.PremiumRewardId Id,
    [ForeignKey("ItemTable", "Id")] ItemRecord.ItemId ItemId,
    [Range(1, 99)] ushort Count)
{
    public record struct PremiumRewardId(int Value);
}

// bool 조건 SwitchFK. 두 대상의 키가 서로 다른 브랜딩이라 열은 int 로 둔다.
[StaticDataRecord("Rpg", "SeasonPass")]
public sealed partial record SeasonPassRecord(
    [Key] int Id,
    [Range(1, 100)] byte Level,
    bool IsPremium,
    [SwitchForeignKey(nameof(IsPremium), "false", "FreeRewardTable", "Id")]
    [SwitchForeignKey(nameof(IsPremium), "true", "PremiumRewardTable", "Id")]
    int RewardId);

// DateOnly 조건 SwitchFK. 조건 값은 DateTimeFormat 으로 파싱되고 오류 메시지에도 그 포맷(Season=2026-07-01)으로 찍힌다. 열은 int(대상 키가 서로 다른 브랜딩).
[StaticDataRecord("Rpg", "SeasonRewards")]
public sealed partial record SeasonRewardRecord(
    [Key] int Id,
    [DateTimeFormat("yyyy-MM-dd")] DateOnly Season,
    [SwitchForeignKey(nameof(Season), "2026-06-01", "Season1ItemTable", "Id")]
    [SwitchForeignKey(nameof(Season), "2026-11-01", "ItemTable", "Id")]
    int ItemId);

// SeasonRewards 의 과거 시즌 대상. Id 대역을 ItemTable(10000대)과 겹치지 않게 20000대로 둔다.
[StaticDataRecord("Rpg", "Season1Items")]
public sealed partial record Season1ItemRecord([Key] Season1ItemRecord.Season1ItemId Id, string Name)
{
    public record struct Season1ItemId([Range(20000, 20999)] int Value);
}

// 퀘스트. PrevQuestId 는 자기 테이블을 가리킨다.
[StaticDataRecord("Rpg", "Quests")]
public sealed partial record QuestRecord(
    [Key] QuestRecord.QuestId Id,
    string Title,

    // nullable 브랜딩은 매퍼가 받지 않아 int? 로 두고, 대상 키 QuestId 는 안쪽 값으로 비교된다.
    [NullString("")] [ForeignKey("QuestTable", "Id")] int? PrevQuestId,
    NpcId Giver,
    [Length(2)] FrozenSet<Objective> Objectives,
    [Range(1, 99)] byte RequiredLevel,
    [NullString("")] long? BonusGold)
{
    public record struct QuestId(int Value);
}

// 대상 키가 서로 다른 브랜딩(ItemId, CurrencyId)이라 열은 int 로 두고 안쪽 값으로 비교한다.
[StaticDataRecord("Rpg", "Attendance")]
public sealed partial record AttendanceRecord(
    [Key] [Range(1, 7)] byte Day,
    [ForeignKey("ItemTable", "Id")] [ForeignKey("CurrencyTable", "Id")] int RewardId,
    [Range(1, 9999)] uint Amount);

// Rpg.Shop 시트를 ShopClientRecord 와 나눠 읽는다. 추출 CSV 는 두 Record 열의 합집합이고 각 로더는 자기 열만 읽는다.
// SaleStart 는 오프셋이 든 DateTimeOffset 이라 픽스처에 날짜 셀 대신 텍스트 셀로 넣는다.
[StaticDataRecord("Rpg", "Shop")]
public sealed partial record ShopServerRecord(
    [Key] [ForeignKey("ItemTable", "Id")] ItemRecord.ItemId ItemId,
    [NullString("무제한")] uint? Stock,
    [DateTimeFormat("yyyy-MM-dd HH:mm:ss zzz")] DateTimeOffset SaleStart,
    [Length(2)] FrozenDictionary<PriceKey, Price> Prices);

// 같은 Shop 시트의 클라이언트 쪽 열. 두 Record 의 [Key] 는 같은 열(ItemId)이다.
[StaticDataRecord("Rpg", "Shop")]
public sealed partial record ShopClientRecord(
    [Key] int ItemId,
    [RegularExpression(@"^icons/[a-z_]+\.png$")] string IconPath,
    byte DisplayOrder);

// 한 시트에서 BuffTable 과 DebuffTable 을 IsDebuff 로 갈라 만드는 입력이다.
[StaticDataRecord("Rpg", "Buffs")]
public sealed partial record BuffRecord(
    [Key] BuffRecord.BuffId Id,
    string Name,
    bool IsDebuff,
    [Range(1, 3600)] ushort DurationSec)
{
    public record struct BuffId(int Value);
}

// [Ignore] 가 붙은 Record 가 헤더 생성, 추출, 로드 어디에도 나타나지 않는지 보는 입력이다. Buffs 시트를 함께 가리킨다.
[Ignore]
[StaticDataRecord("Rpg", "Buffs")]
public sealed partial record DraftBuffRecord([Key] int Id, string Name);

// GachaTable.Validate 가 확률 합 100 을 잡는 입력이다. 브랜딩 요소 집합 PickupItemIds 의 시트 헤더는 PickupItemIds[0] 으로 접힌다(.Value 없음).
[StaticDataRecord("Rpg", "Gacha")]
public sealed partial record GachaRecord(
    [Key] GachaGrade Grade,
    [Range(typeof(decimal), "0.001", "100")] decimal Rate,
    [Length(2)] [ForeignKey("ItemTable", "Id")] FrozenSet<ItemRecord.ItemId> PickupItemIds);
