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
