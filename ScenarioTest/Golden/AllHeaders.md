# StaticDataHeaderGenerator Results

## ScenarioTest.Records.Rpg.AttendanceRecord
- Excel File: `Rpg.xlsx`
- Sheet Name: `Attendance`

### Headers (List)
- Day
- RewardId
- Amount

### Headers (TSV)
```
Day	RewardId	Amount
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
- Giver
- Objectives[0].Type
- Objectives[0].TargetId
- Objectives[0].Count
- Objectives[1].Type
- Objectives[1].TargetId
- Objectives[1].Count
- RequiredLevel
- BonusGold

### Headers (TSV)
```
Id	Title	PrevQuestId	Giver	Objectives[0].Type	Objectives[0].TargetId	Objectives[0].Count	Objectives[1].Type	Objectives[1].TargetId	Objectives[1].Count	RequiredLevel	BonusGold
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

## ScenarioTest.Records.Rpg.Season1ItemRecord
- Excel File: `Rpg.xlsx`
- Sheet Name: `Season1Items`

### Headers (List)
- Id
- Name

### Headers (TSV)
```
Id	Name
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
- Season
- ItemId

### Headers (TSV)
```
Id	Season	ItemId
```

## ScenarioTest.Records.Rpg.ShopClientRecord
- Excel File: `Rpg.xlsx`
- Sheet Name: `Shop`

### Headers (List)
- ItemId
- IconPath
- DisplayOrder

### Headers (TSV)
```
ItemId	IconPath	DisplayOrder
```

## ScenarioTest.Records.Rpg.ShopServerRecord
- Excel File: `Rpg.xlsx`
- Sheet Name: `Shop`

### Headers (List)
- ItemId
- Stock
- SaleStart
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
ItemId	Stock	SaleStart	Prices[0].Key.Currency	Prices[0].Key.Kind	Prices[0].Amount	Prices[0].Note	Prices[1].Key.Currency	Prices[1].Key.Kind	Prices[1].Amount	Prices[1].Note
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

## ScenarioTest.Records.School.BookRecord
- Excel File: `Library.xlsx`
- Sheet Name: `Books`

### Headers (List)
- Id
- Isbn13
- Title
- CallNumber
- PublishedYear
- ShelfCode

### Headers (TSV)
```
Id	Isbn13	Title	CallNumber	PublishedYear	ShelfCode
```

## ScenarioTest.Records.School.CafeteriaRecord
- Excel File: `School.xlsx`
- Sheet Name: `Cafeteria`

### Headers (List)
- Slot.Date
- Slot.Meal
- Menu
- Allergens
- Calories

### Headers (TSV)
```
Slot.Date	Slot.Meal	Menu	Allergens	Calories
```

## ScenarioTest.Records.School.CalendarRecord
- Excel File: `School.xlsx`
- Sheet Name: `Calendar`

### Headers (List)
- Id
- Title
- Start
- End
- IsHoliday

### Headers (TSV)
```
Id	Title	Start	End	IsHoliday
```

## ScenarioTest.Records.School.ClassRoomRecord
- Excel File: `School.xlsx`
- Sheet Name: `ClassRooms`

### Headers (List)
- Id
- Grade
- Floor
- HomeroomTeacherId

### Headers (TSV)
```
Id	Grade	Floor	HomeroomTeacherId
```

## ScenarioTest.Records.School.ClubRecord
- Excel File: `School.xlsx`
- Sheet Name: `Clubs`

### Headers (List)
- Id
- Name
- MeetingDays
- RequiresAudition
- MeetingTime

### Headers (TSV)
```
Id	Name	MeetingDays	RequiresAudition	MeetingTime
```

## ScenarioTest.Records.School.GradeRecord
- Excel File: `School.xlsx`
- Sheet Name: `Grades`

### Headers (List)
- StudentId
- Scores[0].Subject
- Scores[0].Score
- Scores[0].Rank
- Scores[1].Subject
- Scores[1].Score
- Scores[1].Rank
- Scores[2].Subject
- Scores[2].Score
- Scores[2].Rank
- Scores[3].Subject
- Scores[3].Score
- Scores[3].Rank
- Scores[4].Subject
- Scores[4].Score
- Scores[4].Rank
- 출석일수(3~7월)

### Headers (TSV)
```
StudentId	Scores[0].Subject	Scores[0].Score	Scores[0].Rank	Scores[1].Subject	Scores[1].Score	Scores[1].Rank	Scores[2].Subject	Scores[2].Score	Scores[2].Rank	Scores[3].Subject	Scores[3].Score	Scores[3].Rank	Scores[4].Subject	Scores[4].Score	Scores[4].Rank	출석일수(3~7월)
```

## ScenarioTest.Records.School.LoanRecord
- Excel File: `Library.xlsx`
- Sheet Name: `Loans`

### Headers (List)
- BookId
- Borrower
- LoanedAt
- Period
- ReceiptId

### Headers (TSV)
```
BookId	Borrower	LoanedAt	Period	ReceiptId
```

## ScenarioTest.Records.School.ReservationRecord
- Excel File: `Library.xlsx`
- Sheet Name: `Reservations`

### Headers (List)
- Id
- BookId
- Borrower
- ReservedAt

### Headers (TSV)
```
Id	BookId	Borrower	ReservedAt
```

## ScenarioTest.Records.School.StudentRecord
- Excel File: `School.xlsx`
- Sheet Name: `Students`

### Headers (List)
- Id
- 이름
- Gender
- BirthDate
- ClassRoomId
- Seat.Row
- Seat.Col
- ClubId
- Nickname
- Phone
- CardNo
- IsDormitory

### Headers (TSV)
```
Id	이름	Gender	BirthDate	ClassRoomId	Seat.Row	Seat.Col	ClubId	Nickname	Phone	CardNo	IsDormitory
```

## ScenarioTest.Records.School.TeacherRecord
- Excel File: `School.xlsx`
- Sheet Name: `Teachers`

### Headers (List)
- Id
- Name
- MainSubject
- Office
- Certificates

### Headers (TSV)
```
Id	Name	MainSubject	Office	Certificates
```

## ScenarioTest.Records.School.TimetableRecord
- Excel File: `School.xlsx`
- Sheet Name: `Timetable`

### Headers (List)
- Slot.ClassRoomId
- Slot.Day
- Slot.Period
- StartTime
- Subject
- TeacherId

### Headers (TSV)
```
Slot.ClassRoomId	Slot.Day	Slot.Period	StartTime	Subject	TeacherId
```

