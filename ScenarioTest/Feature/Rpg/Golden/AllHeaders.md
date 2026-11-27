# StaticDataHeaderGenerator Results

## ScenarioTest.Records.Rpg.AttendanceRecord
- Excel File: `Rpg.xlsx`
- Sheet Name: `Attendance`

### Headers (List)
- Day
- Kind
- RewardId
- Amount

### Headers (TSV)
```
Day	Kind	RewardId	Amount
```

## ScenarioTest.Records.Rpg.BuffRecord
- Excel File: `Rpg.xlsx`
- Sheet Name: `Buffs`

### Headers (List)
- Id
- Name
- IsDebuff
- DurationSec

### Headers (TSV)
```
Id	Name	IsDebuff	DurationSec
```

## ScenarioTest.Records.Rpg.CurrencyRecord
- Excel File: `Rpg.xlsx`
- Sheet Name: `Currencies`

### Headers (List)
- Id
- Name
- Cap

### Headers (TSV)
```
Id	Name	Cap
```

## ScenarioTest.Records.Rpg.FreeRewardRecord
- Excel File: `Rpg.xlsx`
- Sheet Name: `FreeRewards`

### Headers (List)
- Id
- CurrencyId
- Amount

### Headers (TSV)
```
Id	CurrencyId	Amount
```

## ScenarioTest.Records.Rpg.GachaRecord
- Excel File: `Rpg.xlsx`
- Sheet Name: `Gacha`

### Headers (List)
- Grade
- Rate
- PickupItemIds[0]
- PickupItemIds[1]

### Headers (TSV)
```
Grade	Rate	PickupItemIds[0]	PickupItemIds[1]
```

## ScenarioTest.Records.Rpg.ItemRecord
- Excel File: `Rpg.xlsx`
- Sheet Name: `Items`

### Headers (List)
- Id
- Name
- Description
- Rarity
- Slot
- Price
- MaxStack
- Stats.Atk
- Stats.Def
- Stats.Spd
- Tags[0]

### Headers (TSV)
```
Id	Name	Description	Rarity	Slot	Price	MaxStack	Stats.Atk	Stats.Def	Stats.Spd	Tags[0]
```

## ScenarioTest.Records.Rpg.MonsterRecord
- Excel File: `Rpg.xlsx`
- Sheet Name: `Monsters`

### Headers (List)
- Id
- Name
- Spawn.Map
- Spawn.Pos.X
- Spawn.Pos.Y
- Weakness[0]
- Weakness[1]
- Drops[0].ItemId
- Drops[0].Rate
- Drops[1].ItemId
- Drops[1].Rate
- Skills
- Exp
- RespawnAt

### Headers (TSV)
```
Id	Name	Spawn.Map	Spawn.Pos.X	Spawn.Pos.Y	Weakness[0]	Weakness[1]	Drops[0].ItemId	Drops[0].Rate	Drops[1].ItemId	Drops[1].Rate	Skills	Exp	RespawnAt
```

## ScenarioTest.Records.Rpg.PremiumRewardRecord
- Excel File: `Rpg.xlsx`
- Sheet Name: `PremiumRewards`

### Headers (List)
- Id
- ItemId
- Count

### Headers (TSV)
```
Id	ItemId	Count
```

## ScenarioTest.Records.Rpg.QuestRecord
- Excel File: `Rpg.xlsx`
- Sheet Name: `Quests`

### Headers (List)
- Id
- Title
- PrevQuestId
- Kill.Monster
- Kill.Count
- Collect.Item
- Collect.Count
- Rewards[0].Item
- Rewards[0].Count
- Rewards[1].Item
- Rewards[1].Count
- RequiredLevel
- BonusGold

### Headers (TSV)
```
Id	Title	PrevQuestId	Kill.Monster	Kill.Count	Collect.Item	Collect.Count	Rewards[0].Item	Rewards[0].Count	Rewards[1].Item	Rewards[1].Count	RequiredLevel	BonusGold
```

## ScenarioTest.Records.Rpg.RewardRecord
- Excel File: `Rpg.xlsx`
- Sheet Name: `Rewards`

### Headers (List)
- Id
- Kind
- TargetId
- Amount

### Headers (TSV)
```
Id	Kind	TargetId	Amount
```

## ScenarioTest.Records.Rpg.SeasonItemRecord
- Excel File: `Rpg.xlsx`
- Sheet Name: `OldSeasonItems`

### Headers (List)
- Id
- Name

### Headers (TSV)
```
Id	Name
```

## ScenarioTest.Records.Rpg.SeasonItemRecordV2
- Excel File: `Rpg.xlsx`
- Sheet Name: `SeasonItems`

### Headers (List)
- Id
- Name
- Rarity

### Headers (TSV)
```
Id	Name	Rarity
```

## ScenarioTest.Records.Rpg.SeasonPassRecord
- Excel File: `Rpg.xlsx`
- Sheet Name: `SeasonPass`

### Headers (List)
- Id
- Level
- IsPremium
- RewardId

### Headers (TSV)
```
Id	Level	IsPremium	RewardId
```

## ScenarioTest.Records.Rpg.SeasonRewardRecord
- Excel File: `Rpg.xlsx`
- Sheet Name: `SeasonRewards`

### Headers (List)
- Id
- SeasonStartDay
- ItemId

### Headers (TSV)
```
Id	SeasonStartDay	ItemId
```

## ScenarioTest.Records.Rpg.ShopRecord
- Excel File: `Rpg.xlsx`
- Sheet Name: `Shop`

### Headers (List)
- Id
- ItemIds[0]
- ItemIds[1]
- IconPath
- DisplayOrder
- LimitKind
- LimitCount
- SaleStart
- SaleDuration
- Prices[0].Key.Currency
- Prices[0].Key.Kind
- Prices[0].Amount
- Prices[0].Note
- Prices[1].Key.Currency
- Prices[1].Key.Kind
- Prices[1].Amount
- Prices[1].Note

### Headers (TSV)
```
Id	ItemIds[0]	ItemIds[1]	IconPath	DisplayOrder	LimitKind	LimitCount	SaleStart	SaleDuration	Prices[0].Key.Currency	Prices[0].Key.Kind	Prices[0].Amount	Prices[0].Note	Prices[1].Key.Currency	Prices[1].Key.Kind	Prices[1].Amount	Prices[1].Note
```

## ScenarioTest.Records.Rpg.SkillRecord
- Excel File: `Rpg.xlsx`
- Sheet Name: `Skills`

### Headers (List)
- Id
- Name
- Cooldown

### Headers (TSV)
```
Id	Name	Cooldown
```

## ScenarioTest.Records.Rpg.StageClientRecord
- Excel File: `Rpg.xlsx`
- Sheet Name: `Stages`

### Headers (List)
- Id
- Name
- RequiredLevel
- DailyLimit
- ScenePath
- Bgm
- Boss

### Headers (TSV)
```
Id	Name	RequiredLevel	DailyLimit	ScenePath	Bgm	Boss
```

## ScenarioTest.Records.Rpg.StageServerRecord
- Excel File: `Rpg.xlsx`
- Sheet Name: `Stages`

### Headers (List)
- Id
- RequiredLevel
- DailyLimit
- Boss
- MinClearSeconds

### Headers (TSV)
```
Id	RequiredLevel	DailyLimit	Boss	MinClearSeconds
```

