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

public sealed record Drop(int ItemId, [Range(typeof(decimal), "0", "100")] decimal Rate);

public sealed record Objective(ObjectiveType Type, int TargetId, [Range(1, 999)] int Count);

public sealed record PriceKey(int Currency, PurchaseKind Kind);

public sealed record Price([Key] PriceKey Key, uint Amount, [NullString("")] string? Note);

// 아이템. FK 대상이 가장 많은 테이블(TableSet 에서 유일한 non-nullable). Record 안 브랜딩 키(ItemId),
// enum Range(Common~Epic), nullable enum, uint Range, nullable ushort, 중첩 Stats, [Length] 없이 시트 열 수로 길이를 정하는 컬렉션(Tags),
// [Ignore] 파라미터는 시트에 열이 없어도 된다.
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

// 재화. 한 줄 선언, int Range, long. SwitchFK 와 다중 FK 의 두 번째 대상이다.
[StaticDataRecord("Rpg", "Currencies")]
public sealed partial record CurrencyRecord([Key] CurrencyRecord.CurrencyId Id, string Name, long Cap)
{
    public record struct CurrencyId([Range(900, 999)] int Value);
}

// 스킬. enum 키, nullable TimeSpan 에 TimeSpanFormat, NullString, Range 가 함께 붙는다. Monsters.Skills 의 FK 대상이다.
// 시트의 Id 1003 은 이름 없는 숫자 셀이다. [Key] enum 은 정의되지 않은 값도 받는다.
[StaticDataRecord("Rpg", "Skills")]
public sealed partial record SkillRecord(
    [Key] SkillId Id,
    string Name,
    [TimeSpanFormat("c")] [NullString("-")] [Range(typeof(TimeSpan), "00:00:00", "00:10:00")] TimeSpan? Cooldown);

// 몬스터. 명시 값 enum 키 + Range, 두 단계 중첩(Spawn.Pos), [Length] 고정 길이 컬렉션 둘(nullable enum 요소 / record 요소),
// 쉼표 SingleColumnCollection 에 NullString 과 FK 가 함께 붙은 nullable enum 컬렉션(Skills "-"), 분:초 TimeSpanFormat.
[StaticDataRecord("Rpg", "Monsters")]
public sealed partial record MonsterRecord(
    [Key] [Range(typeof(MonsterId), "1000", "9999")] MonsterId Id,
    string Name,
    Spawn Spawn,
    [Length(2)] [NullString("")] ImmutableArray<Element?> Weakness,

    // #179 가 풀리면 Drop.ItemId 에 FK 를 붙인다.
    [Length(2)] ImmutableArray<Drop> Drops,
    [SingleColumnCollection(",")]
    [NullString("-")]
    [ForeignKey("SkillTable", "Id")]
    ImmutableArray<SkillId?> Skills,
    long Exp,
    [TimeSpanFormat(@"mm\:ss")] TimeSpan RespawnAt);

// enum 조건 SwitchFK. Kind 에 따라 ItemTable 또는 CurrencyTable 을 본다. 대상 키는 브랜딩, 열은 int.
// long 최댓값까지의 Range.
[StaticDataRecord("Rpg", "Rewards")]
public sealed partial record RewardRecord(
    [Key] int Id,
    RewardKind Kind,
    [SwitchForeignKey(nameof(Kind), "Item", "ItemTable", "Id")]
    [SwitchForeignKey(nameof(Kind), "Currency", "CurrencyTable", "Id")]
    int TargetId,
    [Range(typeof(long), "1", "9223372036854775807")] long Amount);

// SeasonPass 의 IsPremium=false 쪽 대상. 자신도 CurrencyTable 로 FK 를 건다.
[StaticDataRecord("Rpg", "FreeRewards")]
public sealed partial record FreeRewardRecord(
    [Key] FreeRewardRecord.FreeRewardId Id,
    [ForeignKey("CurrencyTable", "Id")] CurrencyRecord.CurrencyId CurrencyId,
    [Range(1, 999_999)] uint Amount)
{
    public record struct FreeRewardId(int Value);
}

// SeasonPass 의 IsPremium=true 쪽 대상. 자신도 ItemTable 로 FK 를 건다.
[StaticDataRecord("Rpg", "PremiumRewards")]
public sealed partial record PremiumRewardRecord(
    [Key] PremiumRewardRecord.PremiumRewardId Id,
    [ForeignKey("ItemTable", "Id")] ItemRecord.ItemId ItemId,
    [Range(1, 99)] ushort Count)
{
    public record struct PremiumRewardId(int Value);
}

// bool 조건 SwitchFK. "false"/"true" 조건 값과 생성 코드의 bool 리터럴 비교를 본다. 대상 키는 브랜딩, 열은 int.
[StaticDataRecord("Rpg", "SeasonPass")]
public sealed partial record SeasonPassRecord(
    [Key] int Id,
    [Range(1, 100)] byte Level,
    bool IsPremium,
    [SwitchForeignKey(nameof(IsPremium), "false", "FreeRewardTable", "Id")]
    [SwitchForeignKey(nameof(IsPremium), "true", "PremiumRewardTable", "Id")]
    int RewardId);

// DateOnly 조건 SwitchFK. 조건 값이 DateTimeFormat 으로 파싱되고 오류 메시지에도 그 포맷(Season=2026-07-01)으로 나오는지 본다.
// 대상 키는 브랜딩, 열은 int.
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

// 퀘스트. 자기 테이블을 가리키는 nullable 선행 퀘스트, 브랜딩 안쪽 nullable(NpcId), FrozenSet<record> 고정 길이, nullable long.
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

// 같은 파라미터에 [ForeignKey] 둘. 대상 키가 서로 다른 브랜딩이라 열은 int 로 두고 안쪽 값으로 비교한다.
[StaticDataRecord("Rpg", "Attendance")]
public sealed partial record AttendanceRecord(
    [Key] [Range(1, 7)] byte Day,
    [ForeignKey("ItemTable", "Id")] [ForeignKey("CurrencyTable", "Id")] int RewardId,
    [Range(1, 9999)] uint Amount);

// 같은 시트(Rpg.Shop)를 두 Record 가 나눠 읽는다. 추출 CSV 는 두 Record 열의 합집합이다.
// 서버 쪽은 키가 곧 FK 이고, Map 의 키가 record(PriceKey)이며, 한글 NullString("무제한")을 쓴다.
// SaleStart 는 오프셋이 든 DateTimeOffset 이라 날짜 셀 대신 텍스트 셀로 넣는다.
[StaticDataRecord("Rpg", "Shop")]
public sealed partial record ShopServerRecord(
    [Key] [ForeignKey("ItemTable", "Id")] ItemRecord.ItemId ItemId,
    [NullString("무제한")] uint? Stock,
    [DateTimeFormat("yyyy-MM-dd HH:mm:ss zzz")] DateTimeOffset SaleStart,

    // #179 가 풀리면 PriceKey.Currency 에 FK 를 붙인다.
    [Length(2)] FrozenDictionary<PriceKey, Price> Prices);

// 같은 Shop 시트의 클라이언트 쪽 열. 두 Record 의 [Key] 는 같은 열이다. RegularExpression(아이콘 경로)과 byte 정렬 순서.
[StaticDataRecord("Rpg", "Shop")]
public sealed partial record ShopClientRecord(
    [Key] int ItemId,
    [RegularExpression(@"^icons/[a-z_]+\.png$")] string IconPath,
    byte DisplayOrder);

// 한 시트에서 두 테이블(BuffTable, DebuffTable)을 IsDebuff 로 갈라 만드는 입력. ushort Range.
[StaticDataRecord("Rpg", "Buffs")]
public sealed partial record BuffRecord(
    [Key] BuffRecord.BuffId Id,
    string Name,
    bool IsDebuff,
    [Range(1, 3600)] ushort DurationSec)
{
    public record struct BuffId(int Value);
}

// [Ignore] 가 붙은 StaticDataRecord. 같은 시트를 가리켜도 헤더 생성, 추출, 로드 어디에도 나타나지 않는다.
[Ignore]
[StaticDataRecord("Rpg", "Buffs")]
public sealed partial record DraftBuffRecord([Key] int Id, string Name);

// enum 키, 소수 decimal Range, 브랜딩 요소 FrozenSet<ItemRecord.ItemId> 고정 길이에 FK(헤더는 PickupItemIds[0] 으로 접힘).
// GachaTable.Validate 가 확률 합 100 을 잡는다.
[StaticDataRecord("Rpg", "Gacha")]
public sealed partial record GachaRecord(
    [Key] GachaGrade Grade,
    [Range(typeof(decimal), "0.001", "100")] decimal Rate,
    [Length(2)] [ForeignKey("ItemTable", "Id")] FrozenSet<ItemRecord.ItemId> PickupItemIds);
