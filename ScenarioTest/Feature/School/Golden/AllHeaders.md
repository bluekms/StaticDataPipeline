# StaticDataHeaderGenerator Results

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

## ScenarioTest.Records.School.BookTalkRecord
- Excel File: `Library.xlsx`
- Sheet Name: `BookTalks`

### Headers (List)
- BookId
- Presenter
- StartsAt
- Duration
- RoomId

### Headers (TSV)
```
BookId	Presenter	StartsAt	Duration	RoomId
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

## ScenarioTest.Records.School.RecommendationRecord
- Excel File: `Library.xlsx`
- Sheet Name: `Recommendations`

### Headers (List)
- Id
- BookId
- Recommender
- PostedAt

### Headers (TSV)
```
Id	BookId	Recommender	PostedAt
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

