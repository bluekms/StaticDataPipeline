using ClosedXML.Excel;
using ScenarioTest.Support;

namespace ScenarioTest.Fixtures;

public static class ExcelFixtures
{
    private const string Updated = "최종 수정 2026-02-20";

    public static void Build(string excelRoot)
    {
        var mainDir = Path.Combine(excelRoot, "Main");
        var scienceDir = Path.Combine(excelRoot, "Science");
        Directory.CreateDirectory(mainDir);
        Directory.CreateDirectory(scienceDir);

        BuildSchool(Path.Combine(mainDir, "School.xlsx"));
        BuildLibrary(Path.Combine(mainDir, "Library.xlsx"));
        BuildRpg(Path.Combine(mainDir, "Rpg.xlsx"));
        BuildScience(Path.Combine(scienceDir, "Science.xlsx"));
    }

    private static void BuildSchool(string path)
    {
        using var workbook = new XLWorkbook();

        var students = workbook.AddWorksheet("Students");
        WriteTitled(
            students,
            "학생 명부",
            $"{Updated} 교무부",
            ["Id", "이름", "Gender", "BirthDate", "ClassRoomId", "Seat.Row", "Seat.Col", "ClubId", "Nickname", "Phone", "CardNo", "IsDormitory", "비고"],
            [
                [20240308, "박도윤", "Male", Day(2008, 1, 15), 301, 1, 1, "-", "-", "010-3021-4471", "4000123400010001", "false", null],
                [20250215, "이서준", "Male", Day(2009, 7, 2), 201, 2, 3, 3, "-", "010-5582-1093", "4000123400020002", "TRUE", null],
                [],
                [20260101, "김하늘", "Female", Day(2010, 4, 17), 101, 1, 3, 3, "하늘이", "010-1234-5678", "4000123412341234", "TRUE", null],
                [20260102, "최지우", "Female", Day(2010, 9, 30), 101, 1, 4, "-", "-", "010-7741-2290", "4000123400040004", "false", null],
                [20260103, "정민준", "Male", Day(2010, 2, 11), 101, 2, 1, 5, "-", "010-2210-8834", "4000123400050005", "false", null],
                [20260104, "한서윤", "Female", Day(2010, 12, 3), 102, 1, 1, "-", "-", "010-6650-3317", "4000123400060006", "TRUE", null],
                [null, null, null, null, null, null, null, null, null, null, null, null, "전학 예정"],
            ]);
        students.Cell("A4").Value = "3학년";
        students.Cell("A5").Value = "2학년";
        students.Cell("A7").Value = "1학년";
        students.Row(7).Hide();

        WriteTitled(
            workbook.AddWorksheet("ClassRooms"),
            "학급",
            $"{Updated} 교무부",
            ["Id", "Grade", "Floor", "HomeroomTeacherId"],
            [
                [101, 1, 1, 7],
                [102, 1, 1, 8],
                [201, 2, 2, 9],
                [301, 3, -1, 10],
            ]);

        WriteTitled(
            workbook.AddWorksheet("Teachers"),
            "교원",
            $"{Updated} 교무부",
            ["Id", "Office", "Name", "MainSubject", "Certificates"],
            [
                [7, "2층 교무실", "박서연", "Math", "정교사2급,정보처리기사"],
                [8, null, "김도현", "Korean", "정교사1급"],
                [9, "3층 영어교과실", "이하은", "English", null],
                [10, "과학실", "윤재원", "Science", "정교사2급"],
            ]);

        WriteTitled(
            workbook.AddWorksheet("Timetable"),
            "시간표",
            $"{Updated} 교무부",
            ["Slot.ClassRoomId", "Slot.Day", "Slot.Period", "StartTime", "Subject", "TeacherId"],
            [
                [101, "Mon", 1, "08:40", "Math", 7],
                [101, "Mon", 2, new TimeOnly(9, 40), "Korean", 8],
                [102, "Mon", 1, "08:40", "Korean", 8],
                [102, "Mon", 2, new TimeOnly(9, 40), "Math", 7],
                [201, "Tue", 1, "08:40", "English", 9],
                [301, "Wed", 3, new TimeOnly(10, 40), "Science", 10],
            ]);

        var gradeHeaders = new List<string> { "StudentId.Value" };
        for (var i = 0; i < 5; i++)
        {
            gradeHeaders.Add(FormattableString.Invariant($"Scores[{i}].Subject"));
            gradeHeaders.Add(FormattableString.Invariant($"Scores[{i}].Score"));
            gradeHeaders.Add(FormattableString.Invariant($"Scores[{i}].Rank"));
        }

        gradeHeaders.Add("출석일수(3~7월)");
        WriteTitled(
            workbook.AddWorksheet("Grades"),
            "1학기 성적",
            $"{Updated} 교무부",
            [.. gradeHeaders],
            [
                [
                    20260101,
                    "Korean", 92, "A", "Math", 88, "B", "English", 95, "A", "Science", 81, "B", "Social", 77, "C",
                    "21,19,20,x,18",
                ],
                [
                    20260102,
                    "Korean", 70, "C", "Math", 65, "D", "English", 80, "B", "Science", 72, "C", "Social", 85, "B",
                    "21,19,20,20,18",
                ],
                [
                    20250215,
                    "Korean", 85, "B", "Math", 91, "A", "English", 78, "C", "Science", 88, "B", "History", 90, "A",
                    "20,18,21,19,17",
                ],
                [
                    20240308,
                    "Korean", 95, "A", "Math", 97, "A", "English", 89, "B", "Science", 93, "A", "Social", 84, "B",
                    "22,20,21,20,18",
                ],
            ]);

        WriteTitled(
            workbook.AddWorksheet("Calendar"),
            "2026학년도 학사일정",
            $"{Updated} 교무부",
            ["Id", "Title", "Start", "End", "IsHoliday"],
            [
                [1, "개학", Day(2026, 3, 2), null, "false"],
                [2, "어린이날", Day(2026, 5, 5), null, "true"],
                [3, "전국동시지방선거", Day(2026, 6, 3), null, "true"],
                [4, "추석 연휴", Day(2026, 9, 24), Day(2026, 9, 26), "true"],
                [5, "수능", Day(2026, 11, 19), null, "false"],
            ]);

        WriteTitled(
            workbook.AddWorksheet("Cafeteria"),
            "급식 식단",
            $"{Updated} 행정실",
            ["Slot.Date", "Slot.Meal", "Menu", "Allergens", "Calories"],
            [
                [Day(2026, 3, 3), "Lunch", "현미밥,돼지고기김치찌개,계란말이,깍두기", "1.5.9.10", 812],
                [Day(2026, 3, 3), "Dinner", "쌀밥,된장찌개,고등어구이", "5.6", 690],
                [Day(2026, 3, 4), "Lunch", "카레라이스,단무지", "1.5.10.13", 770],
            ]);

        WriteTitled(
            workbook.AddWorksheet("Clubs"),
            "동아리",
            $"{Updated} 학생부",
            ["Id", "Name", "MeetingDays", "RequiresAudition", "MeetingTime"],
            [
                [3, "로봇공학반", "Tuesday / Thursday", "true", new TimeOnly(16, 30)],
                [5, "합창단", "Monday", "false", null],
                [7, "독서토론", "Wednesday", "미정", new TimeOnly(15, 40)],
            ]);

        workbook.SaveAs(path);
    }

    private static void BuildLibrary(string path)
    {
        using var workbook = new XLWorkbook();

        WriteTitled(
            workbook.AddWorksheet("Books"),
            "장서 목록",
            $"{Updated} 도서관",
            ["Id", "Isbn13", "Title", "CallNumber", "PublishedYear", "ShelfCode"],
            [
                [1, 9788936434120, "\"나무\" 그리고, 숲", "813.7", 2014, "가"],
                [2, null, "삼국유사", "911.03", 1281, "-"],
                [3, 9780000000012, "도서관의\n밤", "843.5", 2020, "나"],
            ]);

        WriteAt(
            workbook.AddWorksheet("Loans"),
            1,
            1,
            ["BookId", "Borrower", "LoanedAt", "Period", "ReceiptId"],
            [
                [1, 20260101, new DateTime(2026, 3, 10, 12, 35, 0), "14.00:00:00", "6f9619ff-8b86-d011-b42d-00cf4fc964ff"],
                [1, 20260101, new DateTime(2026, 4, 2, 13, 10, 0), "14.00:00:00", "{3f2504e0-4f89-11d3-9a0c-0305e82c3301}"],
                [2, 20250215, new DateTime(2026, 3, 11, 9, 5, 0), "7.00:00:00", "A0B1C2D3-E4F5-4678-9ABC-DEF012345678"],
            ]);

        WriteTitled(
            workbook.AddWorksheet("Reservations"),
            "예약",
            $"{Updated} 도서관",
            ["Id", "BookId", "Borrower", "ReservedAt"],
            []);

        workbook.SaveAs(path);
    }

    private static void BuildScience(string path)
    {
        using var workbook = new XLWorkbook();

        WriteAt(
            workbook.AddWorksheet("Elements"),
            1,
            1,
            [
                "Number", "Symbol", "Name", "AtomicMass", "Density", "Electronegativity", "MeltingPointK",
                "Group", "CommonOxidation", "DiscoveredYear", "Category",
            ],
            [
                [1, "H", "수소", 1.008, 0.00008988, 2.20, 13.99, 1, 1, 1766, "Nonmetal"],
                [2, "He", "헬륨", 4.0026, 0.0001785, "N/A", 0.95, 18, null, 1895, "NobleGas"],
                [6, "C", "탄소", 12.011, 2.267, 2.55, 3823, 14, 4, "고대", "Nonmetal"],
                [8, "O", "산소", 15.999, 0.001429, 3.44, 54.36, 16, -2, 1774, "Nonmetal"],
                [26, "Fe", "철", 55.845, 7.874, 1.83, 1811, 8, 3, "고대", "TransitionMetal"],
                [79, "Au", "금", 196.97, 19.3, 2.54, 1337.33, 11, 3, "고대", "TransitionMetal"],
                [57, "La", "란타넘", 138.91, 6.145, 1.10, 1193, null, 3, 1839, "Lanthanide"],
                [118, "Og", "오가네손", 294, "N/A", "N/A", "N/A", 18, null, 2002, "NobleGas"],
            ]);

        workbook.SaveAs(path);
    }

    private static void BuildRpg(string path)
    {
        using var workbook = new XLWorkbook();
        var team = $"{Updated} 기획팀";

        WriteTitled(
            workbook.AddWorksheet("Items"),
            "아이템",
            team,
            [
                "Id", "Name", "Description", "Rarity", "Slot", "Price", "MaxStack",
                "Stats.Atk", "Stats.Def", "Stats.Spd", "Tags[0]", "Tags[1]", "Tags[2]",
            ],
            [
                [
                    10001, "회복 물약", "HP를 50, MP를 10 회복", "Common", null, 150, 99,
                    0, 0, 1.0, "healing", "consumable", "starter",
                ],
                [
                    10002, "철 검", "대장간에서 갓 나온 한손검", "Uncommon", "Weapon", 300, "-",
                    12, 0, 1.15, "melee", "iron", "starter",
                ],
                [
                    10003, "가죽 갑옷", "질긴 소가죽을 덧댄 갑옷", "Rare", "Armor", 450, "-",
                    0, 8, 0.95, "armor", "leather", null,
                ],
            ]);

        WriteTitled(
            workbook.AddWorksheet("Currencies"),
            "재화",
            team,
            ["Id", "Name", "Cap"],
            [
                [901, "골드", 9999999999],
                [902, "보석", 99999],
            ]);

        WriteTitled(
            workbook.AddWorksheet("Skills"),
            "스킬",
            team,
            ["Id", "Name", "Cooldown"],
            [
                ["Fireball", "화염구", "00:00:05"],
                ["Heal", "치유", "00:00:08"],
                [1003, "패시브 강타", "-"],
            ]);

        WriteTitled(
            workbook.AddWorksheet("Monsters"),
            "몬스터",
            team,
            [
                "Id", "Name", "Spawn.Map", "Spawn.Pos.X", "Spawn.Pos.Y", "Weakness[0]", "Weakness[1]",
                "Drops[0].ItemId", "Drops[0].Rate", "Drops[1].ItemId", "Drops[1].Rate", "Skills", "Exp", "RespawnAt",
            ],
            [
                ["Slime", "슬라임", 3, 120, 45, "Fire", null, 10001, 35.5, 10002, 5.0, "-", 12, "00:30"],
                [1002, "고블린", 3, 200, 80, "Fire", "Holy", 10002, 10, 10001, 50, "Fireball", 40, "01:00"],
                ["BossDragon", "화룡", 9, 512, 512, "Ice", "Holy", 10003, 100, 10002, 100, "Fireball,-,Heal", 5000, "30:00"],
            ]);

        WriteTitled(
            workbook.AddWorksheet("Rewards"),
            "보상",
            team,
            ["Id", "Kind", "TargetId", "Amount"],
            [
                [1, "Item", 10001, 1],
                [2, "Currency", 901, 500],
                [3, "Currency", 902, 10],
            ]);

        WriteTitled(
            workbook.AddWorksheet("FreeRewards"),
            "시즌 패스 무료 보상",
            team,
            ["Id", "CurrencyId", "Amount"],
            [
                [1, 901, 100],
                [2, 901, 300],
                [3, 902, 5],
            ]);

        WriteTitled(
            workbook.AddWorksheet("PremiumRewards"),
            "시즌 패스 유료 보상",
            team,
            ["Id", "ItemId", "Count"],
            [
                [1, 10001, 5],
                [2, 10002, 1],
                [3, 10003, 1],
            ]);

        WriteTitled(
            workbook.AddWorksheet("SeasonPass"),
            "시즌 패스",
            team,
            ["Id", "Level", "IsPremium", "RewardId"],
            [
                [1, 1, "false", 1],
                [2, 1, "true", 1],
                [3, 5, "false", 2],
                [4, 5, "true", 2],
                [5, 10, "false", 3],
                [6, 10, "true", 3],
            ]);

        WriteTitled(
            workbook.AddWorksheet("SeasonRewards"),
            "시즌 보상",
            team,
            ["Id", "Season", "ItemId"],
            [
                [1, Day(2026, 6, 1), 20001],
                [2, Day(2026, 11, 1), 10001],
            ]);

        WriteTitled(
            workbook.AddWorksheet("Season1Items"),
            "시즌1 한정 아이템",
            team,
            ["Id", "Name"],
            [
                [20001, "시즌1 기념 검"],
                [20002, "시즌1 망토"],
                [20003, "시즌1 휘장"],
            ]);

        WriteTitled(
            workbook.AddWorksheet("Quests"),
            "퀘스트",
            team,
            [
                "Id", "Title", "PrevQuestId", "Giver",
                "Objectives[0].Type", "Objectives[0].TargetId", "Objectives[0].Count",
                "Objectives[1].Type", "Objectives[1].TargetId", "Objectives[1].Count",
                "RequiredLevel", "BonusGold",
            ],
            [
                [1, "슬라임 퇴치", null, 501, "Kill", 1001, 5, "Talk", 501, 1, 1, 100],
                [2, "고블린 소탕", 1, 501, "Kill", 1002, 3, "Collect", 10001, 2, 5, null],
                [3, "용의 둥지", 2, null, "Kill", 9001, 1, "Collect", 10003, 1, 30, 50000],
            ]);

        WriteTitled(
            workbook.AddWorksheet("Attendance"),
            "7일 출석",
            team,
            ["Day", "RewardId", "Amount"],
            [
                [1, 901, 100],
                [2, 901, 200],
                [3, 10001, 3],
                [4, 902, 10],
                [5, 901, 500],
                [6, 10002, 1],
                [7, 902, 30],
            ]);

        WriteTitled(
            workbook.AddWorksheet("Shop"),
            "상점",
            team,
            [
                "ItemId", "Stock", "SaleStart",
                "Prices[0].Key.Currency", "Prices[0].Key.Kind", "Prices[0].Amount", "Prices[0].Note",
                "Prices[1].Key.Currency", "Prices[1].Key.Kind", "Prices[1].Amount", "Prices[1].Note",
                "IconPath", "DisplayOrder",
            ],
            [
                [
                    10001, "무제한", "2026-03-01 00:00:00 +09:00",
                    901, "Normal", 150, null, 901, "Sale", 120, "봄 이벤트",
                    "icons/potion.png", 1,
                ],
                [
                    10002, 50, "2026-03-01 10:00:00 +09:00",
                    901, "Normal", 300, null, 902, "Normal", 3, null,
                    "icons/iron_sword.png", 2,
                ],
            ]);

        WriteTitled(
            workbook.AddWorksheet("Buffs"),
            "버프",
            team,
            ["Id", "Name", "IsDebuff", "DurationSec"],
            [
                [1, "공격력 증가", "false", 600],
                [2, "방어력 증가", "false", 600],
                [3, "독", "true", 30],
                [4, "둔화", "true", 15],
            ]);

        WriteTitled(
            workbook.AddWorksheet("Gacha"),
            "가챠 확률",
            team,
            ["Grade", "Rate", "PickupItemIds[0]", "PickupItemIds[1]"],
            [
                ["SSR", 2, 10003, 10002],
                ["SR", 18, 10002, 10001],
                ["R", 80, 10001, 10003],
            ]);

        workbook.SaveAs(path);
    }

    private static DateTime Day(int year, int month, int day)
        => new(year, month, day);

    private static void WriteTitled(
        IXLWorksheet sheet,
        string title,
        string description,
        string[] headers,
        object?[][] rows)
    {
        sheet.Cell(1, SheetCells.TitledStartColumn).Value = title;
        sheet.Cell(2, SheetCells.TitledStartColumn).Value = description;
        WriteAt(sheet, SheetCells.TitledHeaderRow, SheetCells.TitledStartColumn, headers, rows);
    }

    private static void WriteAt(IXLWorksheet sheet, int headerRow, int startColumn, string[] headers, object?[][] rows)
    {
        for (var i = 0; i < headers.Length; i++)
        {
            sheet.Cell(headerRow, startColumn + i).Value = headers[i];
        }

        for (var r = 0; r < rows.Length; r++)
        {
            for (var c = 0; c < rows[r].Length; c++)
            {
                CellWriter.Write(sheet.Cell(headerRow + 1 + r, startColumn + c), rows[r][c]);
            }
        }
    }
}
