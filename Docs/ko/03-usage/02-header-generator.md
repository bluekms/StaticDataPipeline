# 3.2 표준 헤더 생성기

> 이 챕터는 **데이터 작업자 (기획자)** 를 위한 안내입니다. [3.1](./01-record-to-excel.md) 에서 본 객체의 배열 예제처럼 헤더가 한 행으로 길어질 때, 손으로 맞추지 않고 자동으로 채우는 방법을 다룹니다.

## 왜 필요한가

[3.1](./01-record-to-excel.md) 마지막에서 본 `Student` 시트의 헤더는 다음과 같이 한 줄에 8개였습니다.

```
Id  Name  Subjects[0].Subject  Subjects[0].Score  Subjects[1].Subject  Subjects[1].Score  Subjects[2].Subject  Subjects[2].Score
```

`SubjectScore` 의 필드를 바꾸거나 반복 횟수를 조정하면 헤더 줄을 처음부터 다시 맞춰야 합니다. 시트가 여러 개 있다면 그 작업이 곱절로 늘어납니다. **`StaticDataHeaderGenerator`** 는 Record `.cs` 파일을 입력으로 받아 위와 같은 헤더 한 줄을 자동으로 출력해 주는 CLI 도구입니다.

## 실행해 보기

[3.1](./01-record-to-excel.md) 의 `Student` Record 를 그대로 가정합니다. Record `.cs` 가 `./Records` 폴더에 있다면 다음 한 줄로 헤더 줄을 받아낼 수 있습니다.

```bash
StaticDataHeaderGenerator.exe ^
  --record-path ./Records ^
  --record-name Student ^
  --output-file ./Headers/Student.tsv
```

`./Headers/Student.tsv` 파일이 생기고, 그 안에는 다음 한 줄이 들어 있습니다 (구분자는 기본값 탭).

```
Id	Name	Subjects[0].Subject	Subjects[0].Score	Subjects[1].Subject	Subjects[1].Score	Subjects[2].Subject	Subjects[2].Score
```

`--record-name` 을 생략하면 `--record-path` 폴더의 모든 `[StaticDataRecord]` Record 가 한꺼번에 처리됩니다.

## bat 한 번에 모든 시트 정리하기

기획자가 매번 옵션을 외워 입력하지 않도록 **bat 파일 하나** 를 폴더에 같이 두는 방식을 권장합니다. 예를 들어 다음 내용을 `Generate-Headers.bat` 로 저장해 두면 더블 클릭으로 헤더 전체를 새로 뽑을 수 있습니다.

```bat
@echo off
StaticDataHeaderGenerator.exe ^
  --record-path .\Records ^
  --output-file .\Headers\AllHeaders.tsv
pause
```

`pause` 가 있으면 결과 메시지를 확인한 뒤 창이 닫히므로 안심하고 닫을 수 있습니다. 결과 파일은 시트마다 한 줄씩 들어 있는 형태로 만들어집니다.

## Excel 에 붙여넣기

`Student.tsv` 한 줄을 Excel 헤더에 적용해 봅니다.

1. 결과 `.tsv` 파일을 메모장이나 VS Code 같은 단순 편집기로 엽니다.
2. 한 줄 전체를 선택하고 복사합니다 (`Ctrl + A`, `Ctrl + C`).
3. Excel 의 `StudentReport.xlsx` 파일을 열고 `Grades` 시트로 갑니다.
4. 헤더가 시작될 셀 (예: `A1`) 을 한 번 클릭합니다. 이 한 셀만 선택한 상태여야 합니다.
5. `Ctrl + V` 로 붙여넣습니다.

탭 구분자가 자연스럽게 한 칸씩 다른 셀로 들어가므로, 한 번의 붙여넣기로 다음과 같이 채워집니다.

|       | **A** | **B** | **C**                 | **D**               | **E**                 | **F**               | **G**                 | **H**               |
|-------|-------|-------|-----------------------|---------------------|-----------------------|---------------------|-----------------------|---------------------|
| **1** | Id    | Name  | Subjects[0].Subject   | Subjects[0].Score   | Subjects[1].Subject   | Subjects[1].Score   | Subjects[2].Subject   | Subjects[2].Score   |
| **2** |       |       |                       |                     |                       |                     |                       |                     |

이제 2행부터 데이터를 채우면 됩니다.

|       | **A** | **B**   | **C**                 | **D**               | **E**                 | **F**               | **G**                 | **H**               |
|-------|-------|---------|-----------------------|---------------------|-----------------------|---------------------|-----------------------|---------------------|
| **1** | Id    | Name    | Subjects[0].Subject   | Subjects[0].Score   | Subjects[1].Subject   | Subjects[1].Score   | Subjects[2].Subject   | Subjects[2].Score   |
| **2** | 1     | Alice   | Math                  | 90                  | English               | 85                  | Science               | 88                  |
| **3** | 2     | Bob     | Math                  | 70                  | English               | 95                  | Science               | 75                  |

## 권장 작업 흐름

표준 헤더가 확정되기 전에도 데이터 입력을 멈출 필요는 없습니다. **임시 헤더** 를 한 행 위에 두면 Record 정의와 데이터 입력을 병렬로 진행할 수 있습니다.

추출기 옵션을 `--start-cell B3` 으로 합의했다고 합시다. 이때 시트 구성은 다음과 같이 잡습니다.

|       | **A**       | **B**       | **C**       | **D**       |
|-------|-------------|-------------|-------------|-------------|
| **1** | (자유)       | (자유)       | (자유)       | (자유)       |
| **2** |             | 임시 헤더    | 임시 헤더    | 임시 헤더    |
| **3** |             | 표준 헤더    | 표준 헤더    | 표준 헤더    |
| **4** |             | 데이터       | 데이터       | 데이터       |

- `A1` ~ `A2` 영역은 메모, 시트 설명을 적어 두는 자유 영역입니다. 추출기는 읽지 않습니다.
- `B2` 행에는 데이터 작업자가 알아보기 쉬운 임시 이름을 적어 둡니다 (예: "ID", "이름", "수학 점수"). 추출기는 이 행을 보지 않습니다.
- `B3` 행은 표준 헤더 자리입니다. Record 정의가 끝나기 전에는 비워 두고, `StaticDataHeaderGenerator` 출력이 나오면 그대로 붙여넣습니다.
- `B4` 부터 데이터를 채웁니다.

기획자 입장의 흐름은 다음과 같이 흘러갑니다.

1. 프로그래머와 컬럼 구성, 시작 셀 (`B3`) 을 합의합니다. 이 시점에는 Record `.cs` 가 아직 미완성이어도 됩니다.
2. `B2` 에 임시 헤더를 적고, `B4` 부터 데이터를 입력해 나갑니다.
3. 프로그래머가 Record 를 확정하면 `StaticDataHeaderGenerator` 를 돌려 표준 헤더를 받아옵니다.
4. 그 결과를 `B3` 에 붙여넣습니다. 임시 헤더는 그대로 둬도 되고, 깔끔하게 지워도 됩니다.
5. 이후 추출은 `ExcelColumnExtractor --start-cell B3` 으로 진행됩니다 (CI 에서 자동 실행을 권장).

## 옵션 전체

|옵션|의미|기본값|
|-|-|-|
|`-r`, `--record-path`|Record `.cs` 파일 또는 디렉터리 경로|필수|
|`-n`, `--record-name`|대상 Record 이름 (없으면 경로의 모든 Record 대상)|없음|
|`-s`, `--separator`|헤더 간 구분자|`\t` (탭)|
|`-o`, `--output-file`|출력 파일 경로 (없으면 콘솔 출력)|없음|
|`-l`, `--log-path`|로그 파일 경로|없음|
|`-v`|최소 로그 레벨|Information|

---

[← 이전: 3.1 Record 를 먼저 쓰고 Excel 작업하기](./01-record-to-excel.md) | [목차](../README.md) | [다음: 3.3 첫 Record 정의하기 →](./03-first-record.md)
