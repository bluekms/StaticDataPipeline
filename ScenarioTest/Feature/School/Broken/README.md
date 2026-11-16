# 깨진 워크북 — 추출 단계 실수 시나리오의 입력

정상 워크북(`School/Excel`)의 시트 하나를 한 가지 방식으로 망가뜨린 파일들이다.
틀린 자리는 노란 칸에 굵은 진홍 글씨로 표시했고, 그 칸의 메모에 설명과 전후 값을 적었다.
제목 줄이 있는 시트는 제목 오른쪽에, A1 에서 시작하는 시트는 헤더 행의 Record 밖 열에 `[깨진 예] 이름: 설명` 을 적었다.
열이 지워지거나 밀려 바뀐 칸이 많은 경우는 칸을 칠하지 않고 이 설명만 있다.

폴더는 `<워크북>/<시트>/` 로 나뉘어 정상 워크북과 그 안의 시트 구조를 그대로 따르고, 파일 이름이 사례 이름이다.
예를 들어 `School/Students/StudentGenderLowercase.xlsx` 는 `School/Excel/School.xlsx` 의 Students 시트를 망가뜨린 것이다.
테스트는 `ScenarioWorkspace.UseBroken("<워크북>/<시트>/<사례>")` 로 작업 폴더의 정상 워크북을 그 파일로 덮어쓴 뒤 추출기를 돌린다.
정상 워크북이 바뀌면 여기의 파일들도 같이 고쳐야 한다. Excel 로 열어 고치되 노란 표시와 메모는 유지한다.

| 파일 | 설명 | 추출기가 말해야 하는 것 |
|---|---|---|
| `School/Students/StudentGenderLowercase.xlsx` | Gender 에 enum 이름이 아닌 소문자 female 을 적었다. 대소문자까지 같아야 한다. | `Invalid value 'female'` / `Gender` |
| `School/Students/StudentGenderNumber.xlsx` | Gender 에 enum 이름 대신 숫자 1 을 적었다. 이름만 받는다. | `Invalid value '1'` / `Gender` |
| `School/ClassRooms/ClassRoomGradeOutOfRange.xlsx` | 학년(Grade)은 1~3 인데 4 를 적었다. | `Value(4) must be between 1 and 3` |
| `School/ClassRooms/ClassRoomFloorOutOfRange.xlsx` | 층(Floor)은 지하 1층(-1)~5층인데 -2 를 적었다. | `Value(-2) must be between -1 and 5` |
| `School/Students/StudentPhoneMissingDigit.xlsx` | 전화번호 뒷자리가 세 자리다. 010-0000-0000 꼴만 받는다. | `Invalid value '010-1234-567'` / `Expected to match pattern` |
| `School/Grades/GradesAttendanceFourMonths.xlsx` | 출석일수는 3~7월 다섯 달이라 값이 다섯 개여야 하는데 네 개만 적었다. | `contains 4 item(s)` / `between 5 and 5` |
| `School/Grades/GradeRankBeyondE.xlsx` | 등급(Rank)은 A~E 인데 F 를 적었다. | `Value(F) must be between A and E` |
| `School/Clubs/ClubMeetingDayNotDayOfWeek.xlsx` | 요일 목록에 Funday 가 들어갔다. System.DayOfWeek 의 이름만 받는다. | `'Funday'` |
| `School/Calendar/CalendarStartSlashText.xlsx` | 날짜를 2026/03/02 처럼 적었다. 이 열의 형식은 yyyy-MM-dd 다. | `'2026/03/02'` |
| `School/Timetable/TimetableStartTimeNoLeadingZero.xlsx` | 시각을 8:40 처럼 적었다. 이 열의 형식은 HH:mm 이라 08:40 이어야 한다. | `'8:40'` |
| `School/Timetable/TimetableStartTimeAfterSchool.xlsx` | 수업 시작 시각은 08:00~17:00 인데 18:00 을 적었다. | `Value(18:00) must be between 08:00 and 17:00` |
| `School/Cafeteria/CafeteriaMenuDoubleComma.xlsx` | 메뉴 목록에 쉼표가 두 번 이어져 빈 항목이 생겼다. | `contains an empty value` |
| `School/Cafeteria/CafeteriaMenuBlank.xlsx` | 메뉴 칸을 비웠다. 메뉴는 1~8 개여야 한다. | `contains 0 item(s)` / `between 1 and 8` |
| `School/Timetable/TimetableStartTimeWithSeconds.xlsx` | 시간 셀에 초(08:40:30)가 들어 있다. 이 열의 형식 HH:mm 은 초를 담지 못해 잘라 내지 않고 거부한다. | `1899-12-31 08:40:30` |
| `School/Students/StudentBirthDateWithTime.xlsx` | 생년월일 날짜 셀에 시각(10:30)이 들어 있다. 이 열의 형식 yyyy-MM-dd 는 시각을 담지 못한다. | `2010-04-17 10:30:00` |
| `Library/Loans/LoanReceiptIdNotGuid.xlsx` | 영수증 번호(ReceiptId)가 Guid 꼴이 아니다. | `'not-a-guid'` |
| `School/Teachers/TeacherSubjectWithSpaces.xlsx` | 담당 과목 앞뒤에 공백이 있다. 공백은 경고로 알리고, 그 값은 enum 이름과 달라 실패한다. | `Invalid value ' Math '` |
| `School/Students/PhoneHeaderLowercase.xlsx` | 헤더 Phone 을 phone 으로 적었다. 헤더는 Record 의 이름과 대소문자까지 같아야 한다. | `Record header: 'Phone', sheet header: 'phone'` |
| `School/Grades/GradesBothStudentIdForms.xlsx` | StudentId.Value 열을 복사해 StudentId 헤더를 하나 더 두었다. 브랜딩 키 헤더는 둘 중 한 꼴만 있어야 한다. | `Both 'StudentId' and 'StudentId.Value' exist` |
| `School/Students/PhoneColumnCopiedTwice.xlsx` | Phone 열을 복사해 같은 헤더가 둘이 됐다. | `Phone` |
| `School/Students/PhoneColumnDeleted.xlsx` | Phone 열을 통째로 지웠다. Record 에 있는 열은 시트에 있어야 한다. | `Header not found: Phone` |
| `School/Students/StudentIdCopied.xlsx` | 다른 학생의 학번을 그대로 복사해 Id 가 둘이 같다. | `Primary key 'Id' has duplicate value '20260101'` |
| `School/Timetable/TimetableSlotDuplicated.xlsx` | 같은 반, 같은 요일의 교시를 다른 행과 같게 적어 복합 키(Slot)가 중복됐다. | `Primary key` / `duplicate value` |
| `School/Students/MemoColumnFormulaAndPercent.xlsx` | Record 에 없는 메모 열(비고)에 수식과 퍼센트 서식을 넣었다. Record 밖의 열은 검사하지 않으므로 아무 말도 없어야 한다. | 실패하지 않는다(경고만 남기거나 다음 단계에서 걸린다) |
| `School/Students/NicknameWithSpaces.xlsx` | 별명 앞뒤에 공백이 있다. 문자열 열은 공백을 경고로 알리되 값은 그대로 둔다. | 실패하지 않는다(경고만 남기거나 다음 단계에서 걸린다) |
| `Library/Reservations/ReservationRowsBackgroundOnly.xlsx` | 데이터 행 없이 B4:E12 에 배경색만 칠했다. 서식만 있는 행은 데이터로 보지 않는다. | 실패하지 않는다(경고만 남기거나 다음 단계에서 걸린다) |
| `Library/Loans/LoansStartAtA1.xlsx` | Loans 표가 다른 시트와 달리 A1 에서 시작한다. 테스트가 LoanRecord 속성에 시작 셀 "A1" 을 적어 넣고 돌리면 속성이 `--start-cell B3` 보다 우선해 그대로 읽히고, 둘이 달라 경고 하나가 남는다. | 실패하지 않는다(경고만 남기거나 다음 단계에서 걸린다) |
| `School/Students/TransferStudentAdded.xlsx` | 전학생 한 명(20260105 윤하린)을 마지막 행 아래에 추가했다. 다음 버전 폴더로 추출해 리로드하면 7명이 된다. | 실패하지 않는다(경고만 남기거나 다음 단계에서 걸린다) |
