# 3.1 Record 를 먼저 쓰고 Excel 작업하기

> 이 챕터는 **데이터 작업자 (기획자)** 를 위한 안내입니다. C# Record 가 먼저 확정되었고, 이제 그것에 맞춰 Excel 을 채우는 상황을 다룹니다. C# 쪽에서 Record 를 작성하는 흐름은 [3.3 첫 Record 정의하기](./03-first-record.md) 에서 다룹니다.

프로그래머가 Record 를 먼저 확정해 두면, 데이터 작업자는 빈 시트 위에 어디서부터 무엇을 채워야 하는지를 정확히 알 수 있습니다. 이 챕터는 그 약속을 한 자리에 모아 둡니다.

## 파일과 시트 이름

C# Record 에 `[StaticDataRecord("파일이름", "시트이름")]` 이 붙어 있습니다. 추출기는 이 정보로 Excel 파일과 시트를 찾습니다.

예를 들어 다음 Record 는 `GameItems.xlsx` 파일의 `Items` 시트를 가리킵니다.

```csharp
[StaticDataRecord("GameItems", "Items")]
public sealed record Item(...);
```

- **파일 이름과 시트 이름은 달라도 됩니다.** 위 예에서 파일은 `GameItems`, 시트는 `Items` 로 서로 다릅니다.
- **시트 이름은 정확히 일치해야 합니다** (대소문자 포함).
- 추출 결과 CSV 는 `{파일}.{시트}.csv` 규칙으로 만들어집니다 — 위 예에서는 `GameItems.Items.csv`.

프로그래머에게 어떤 파일에 어떤 시트를 만들어야 하는지 한 번만 확인하면 됩니다.

## 헤더가 시작하는 셀

Sdp 의 추출기는 헤더가 어디서 시작하는지를 `--start-cell` 옵션으로 받습니다. 옵션을 생략하면 `A1` 부터 헤더가 시작한다고 가정합니다.

- 옵션이 가리키는 셀 = **첫 번째 컬럼의 헤더 이름이 들어가는 자리**
- 그 다음 행 = **첫 번째 데이터 행이 시작하는 자리**

가장 단순한 형태는 `A1` 부터 헤더를 두고 `A2` 부터 데이터를 채우는 식이며, 이 경우는 옵션을 생략해도 됩니다. 위쪽에 메모나 타이틀을 두고 싶으면 그만큼 내려간 주소를 옵션으로 넘기면 됩니다 (예: `--start-cell B3`).

```bash
ExcelColumnExtractor.exe ^
  --record-path ./Records ^
  --excel-path ./Excels ^
  --output-path ./Csv ^
  --start-cell B3
```

시트마다 시작 위치를 다르게 두고 싶다면 추출 호출을 분리합니다. 같은 호출에 묶인 시트들은 동일한 시작 위치 규칙을 공유합니다.

## 단순한 시트 — Item 예제

다음 Record 가 있다고 합시다.

```csharp
[StaticDataRecord("GameItems", "Items")]
public sealed record Item(
    int Id,
    string Name,
    int Price,
    ItemCategory Category);
```

`GameItems.xlsx` 의 `Items` 시트에 다음과 같이 채워 둡니다. 시작 셀을 `A1` 으로 둔 모습입니다.

|       | **A**  | **B**    | **C**   | **D**        |
|-------|--------|----------|---------|--------------|
| **1** | Id     | Name     | Price   | Category     |
| **2** | 1      | Potion   | 100     | Consumable   |
| **3** | 2      | Sword    | 5000    | Weapon       |
| **4** | 3      | Shield   | 4000    | Armor        |

- **1행이 헤더** — Record 의 파라미터 이름과 정확히 같은 이름을 적습니다.
- **2행부터 데이터** — 각 셀에 한 행의 값을 채웁니다.
- `enum` 컬럼인 `Category` 는 enum 멤버 이름 (`Consumable`, `Weapon`, `Armor`) 으로 적습니다. 정수가 아닙니다.

## 참고 컬럼은 그대로 둬도 된다

기획자 입장에서 비고나 메모처럼 시트 안에서만 보고 싶은 컬럼이 자주 필요합니다. **Record 가 모르는 컬럼은 추출 시 무시되므로 그대로 두어도 됩니다.**

|       | **A**  | **B**    | **C**     | **D**   | **E**        |
|-------|--------|----------|-----------|---------|--------------|
| **1** | Id     | Name     | Memo      | Price   | Category     |
| **2** | 1      | Potion   | 회복 아이템 | 100     | Consumable   |
| **3** | 2      | Sword    | 기본 검    | 5000    | Weapon       |
| **4** | 3      | Shield   | 기본 방패  | 4000    | Armor        |

`Memo` 컬럼은 Record 에 없으므로 추출되지 않습니다. 데이터 작업자끼리 공유할 메모를 시트에 그대로 남길 수 있다는 의미입니다.

반대로 Record 가 요구하는 컬럼이 시트에 **없으면 추출이 실패합니다.** 헤더 이름은 정확한 표기 (대소문자 포함) 가 일치해야 합니다.

## 같은 타입의 값이 여러 개 — 컬렉션

한 행에 같은 종류의 값을 여러 개 담아야 할 때가 있습니다. 예를 들어 한 아이템이 보유한 태그 목록처럼 말입니다. 두 가지 표현 방식 중에 데이터의 성격에 맞는 쪽을 고릅니다.

### 방식 A — 한 셀에 구분자로 묶기

태그가 그저 라벨이고 개별 값 검토가 필요 없다면, **한 셀에 쉼표로 묶어 두는** 방식이 가벼워서 좋습니다. Record 쪽에서 `[SingleColumnCollection(",")]` 으로 선언되어 있으면 됩니다.

```csharp
[StaticDataRecord("GameItems", "Items")]
public sealed record Item(
    int Id,
    string Name,
    [SingleColumnCollection(",")] ImmutableArray<string> Tags);
```

엑셀 시트는 다음 모양이 됩니다.

|       | **A**  | **B**    | **C**            |
|-------|--------|----------|------------------|
| **1** | Id     | Name     | Tags             |
| **2** | 1      | Potion   | heal,consumable  |
| **3** | 2      | Sword    | melee,iron       |
| **4** | 3      | Shield   | defense,iron     |

장점은 한 줄이 짧다는 것입니다. 단점은 **개별 태그를 셀 단위로 비교하거나 정렬할 수 없다는 점** 입니다. 셀 안의 문자열 전체가 하나의 값으로 다뤄집니다.

### 방식 B — 여러 셀로 펼치기

태그 하나하나가 검토 대상이거나, 셀 단위로 정렬, 필터링하고 싶다면 **여러 셀로 펼치는** 방식이 유리합니다. Record 쪽에서 `[Length(n)]` 으로 고정 개수가 선언되어 있으면 됩니다.

```csharp
[StaticDataRecord("GameItems", "Items")]
public sealed record Item(
    int Id,
    string Name,
    [Length(3)] ImmutableArray<string> Tags);
```

`[Length(3)]` 이면 헤더가 `Tags[0]`, `Tags[1]`, `Tags[2]` 로 펼쳐집니다.

|       | **A**  | **B**    | **C**     | **D**        | **E**     |
|-------|--------|----------|-----------|--------------|-----------|
| **1** | Id     | Name     | Tags[0]   | Tags[1]      | Tags[2]   |
| **2** | 1      | Potion   | heal      | consumable   | small     |
| **3** | 2      | Sword    | melee     | iron         | starter   |
| **4** | 3      | Shield   | defense   | iron         | starter   |

이렇게 두면 Excel 의 필터, 정렬 기능이 각 태그를 독립적으로 다룰 수 있습니다. **모든 행을 같은 항목 기준으로 보고 싶다면 펼치는 방식이 압도적으로 편합니다.**

선택 기준을 한 줄로 줄이면 다음과 같습니다.

- **셀 단위로 다룰 일이 없다** → 한 셀에 묶기 (`SingleColumnCollection`)
- **셀 단위로 비교, 필터, 검토한다** → 여러 셀로 펼치기 (`Length`)

## 객체 여러 개 — 객체의 배열

태그처럼 단순한 값이 아니라, **여러 필드를 가진 묶음** 이 한 행에 여러 개 들어가는 경우가 있습니다. 예를 들어 학생 한 명이 여러 과목의 성적을 가진 시트입니다.

```csharp
public sealed record SubjectScore(string Subject, int Score);

[StaticDataRecord("StudentReport", "Grades")]
public sealed record Student(
    int Id,
    string Name,
    [Length(3)] ImmutableArray<SubjectScore> Subjects);
```

`SubjectScore` 는 두 개의 필드 (`Subject`, `Score`) 를 갖는 묶음입니다. `[Length(3)]` 이면 그 묶음이 세 번 반복됩니다. 헤더는 다음과 같이 펼쳐집니다.

|       | **A** | **B**   | **C**                 | **D**               | **E**                 | **F**               | **G**                 | **H**               |
|-------|-------|---------|-----------------------|---------------------|-----------------------|---------------------|-----------------------|---------------------|
| **1** | Id    | Name    | Subjects[0].Subject   | Subjects[0].Score   | Subjects[1].Subject   | Subjects[1].Score   | Subjects[2].Subject   | Subjects[2].Score   |
| **2** | 1     | Alice   | Math                  | 90                  | English               | 85                  | Science               | 88                  |
| **3** | 2     | Bob     | Math                  | 70                  | English               | 95                  | Science               | 75                  |
| **4** | 3     | Carol   | Math                  | 80                  | English               | 80                  | Science               | 90                  |

헤더가 빠르게 길어집니다. 묶음이 더 깊거나 반복 횟수가 늘어나면 손으로 헤더를 맞추는 일이 사실상 불가능해집니다.

이 부분을 자동으로 처리해 주는 것이 **표준 헤더 생성기** 입니다. Record `.cs` 파일을 입력으로 받아 위와 같은 헤더 줄을 한 번에 출력해 줍니다. 다음 챕터에서 이어 가겠습니다.

---

[← 이전: 2. 설치](../02-installation.md) | [목차](../README.md) | [다음: 3.2 표준 헤더 생성기 →](./02-header-generator.md)
