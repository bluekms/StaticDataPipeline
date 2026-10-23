# 4.1 StaticDataHeaderGenerator

`StaticDataHeaderGenerator` 는 C# Record 정의에서 **표준 헤더** 를 뽑아 주는 CLI 도구입니다. 객체 배열처럼 헤더가 길어지는 시트의 헤더 줄을 손으로 맞추지 않고 자동으로 채울 수 있게 해 줍니다.

결과는 **Markdown 문서** 로 만들어집니다. 한 파일 안에 Record 단위로 섹션이 만들어지고, 그 안에 헤더 목록 (List) 과 구분자로 이어 붙인 헤더 줄 (Code block) 이 함께 들어갑니다. 데이터 작업자는 Code block 안의 한 줄을 복사해 Excel 헤더에 붙여 넣습니다 ([3.2 표준 헤더 생성기](../03-usage/02-header-generator.md) 참고).

이 챕터는 도구 자체의 사용법 — 명령 형태, 옵션 전체, 출력 형식, bat 예제 — 에 집중합니다.

네 자리 중에서는 **빌드** 자리의 도구입니다. 입력은 레코드 작업자의 Record `.cs` 폴더, 출력은 데이터 작업자가 받아 가는 Markdown 한 파일입니다. 실행하는 쪽은 CI 또는 레코드 작업자이고, 데이터 작업자는 결과만 소비합니다.

</br></br></br>

## 명령 구조

명령은 두 가지 형태가 있고, 첫 번째 인자로 어떤 형태인지를 지정합니다.

```bash
StaticDataHeaderGenerator.exe header [옵션...]
StaticDataHeaderGenerator.exe all-header [옵션...]
```

- `header` — **Record 하나** 의 표준 헤더를 생성합니다. `--record-name` 으로 대상을 지정해야 합니다.
- `all-header` — `--record-path` 폴더 아래 모든 `[StaticDataRecord]` Record 의 표준 헤더를 한꺼번에 생성합니다.

`header` 는 결과 Markdown 을 콘솔에 출력하며, `--output-file` 을 지정하면 그 파일로도 저장합니다 (콘솔 출력은 그대로 유지). `all-header` 는 폴더 전체를 처리해 출력량이 많을 수 있으므로 콘솔에는 출력하지 않고, `--output-file` 로 지정한 파일에만 저장합니다.

</br></br></br>

## 옵션

### `header` — 단일 Record

|옵션|의미|기본값|
|-|-|-|
|`-r`, `--record-path`|Record `.cs` 파일 또는 디렉터리 경로|필수|
|`-n`, `--record-name`|대상 Record 이름 (클래스 이름, 예: `StudentRecord`)|필수|
|`-s`, `--separator`|Code block 안 헤더 사이에 들어갈 구분자|`\t` (탭)|
|`-o`, `--output-file`|출력 파일 경로 (없으면 콘솔)|없음|
|`-l`, `--log-path`|로그 디렉터리 경로 (그 아래 일자별 `log<날짜>.txt` 가 생성됨)|없음|
|`-m`, `--min-log-level`|최소 로그 레벨 (Verbose, Debug, Information, Warning, Error, Fatal)|Information|

### `all-header` — 폴더 전체

|옵션|의미|기본값|
|-|-|-|
|`-r`, `--record-path`|Record `.cs` 파일 또는 디렉터리 경로|필수|
|`-s`, `--separator`|Code block 안 헤더 사이에 들어갈 구분자|`\t` (탭)|
|`-o`, `--output-file`|출력 파일 경로 (`all-header` 는 콘솔 출력이 없어 생략하면 결과가 파일로 남지 않음)|없음|
|`-l`, `--log-path`|로그 디렉터리 경로 (그 아래 일자별 `log<날짜>.txt` 가 생성됨)|없음|
|`-m`, `--min-log-level`|최소 로그 레벨|Information|

`all-header` 에는 `--record-name` 이 없습니다. 폴더 전체를 처리하므로 대상 지정이 필요하지 않습니다.

`--output-file` 에 확장자를 빼고 경로만 주면 자동으로 `.md` 가 붙습니다. 확장자를 지정해도 출력 내용은 항상 Markdown 입니다.

</br></br></br>

## 출력 형식

결과는 다음 구조의 Markdown 문서입니다.

```markdown
# StaticDataHeaderGenerator Results

## {RecordFullName}
- Excel File: `{ExcelFileName}.xlsx`
- Sheet Name: `{SheetName}`

### Headers (List)
- Id
- Name
- ...

### Headers (TSV)
​```
Id<sep>Name<sep>...
​```
```

- 최상단 `# StaticDataHeaderGenerator Results` 한 줄.
- Record 마다 `## {RecordFullName}` 섹션이 하나씩. `{RecordFullName}` 은 Record 가 네임스페이스 안에 선언되어 있으면 `네임스페이스.타입이름` 형태가 됩니다 (이 문서의 예제 Record 들은 네임스페이스 없이 정의된 것으로 가정해 단순 이름으로 표기).
  - `Excel File`, `Sheet Name` — `[StaticDataRecord]` 의 두 인자.
  - `### Headers (List)` — 헤더를 한 줄에 하나씩 bullet 으로.
  - `### Headers (TSV)` — `--separator` 로 이어 붙인 한 줄을 코드 블록 안에 표기.

`--separator` 가 영향을 주는 자리는 **`### Headers` 섹션의 라벨과 그 아래 코드 블록 안의 한 줄** 뿐입니다. 라벨은 탭이면 `(TSV)`, 쉼표면 `(CSV)`, 그 외엔 괄호 없이 `### Headers` 로 결정됩니다. 헤더 목록 (List) 과 다른 메타 정보는 그대로 유지됩니다.

</br></br></br>

## 실행 예시

이 섹션에서는 `./Records` 폴더 안에 다음 두 Record 가 있다고 가정합니다.

```csharp
using System.Collections.Immutable;
using Sdp.Attributes;

[StaticDataRecord("StudentReport", "Grades")]
public sealed partial record StudentRecord(
    int Id,
    string Name,
    [Length(3)] ImmutableArray<SubjectScore> Subjects);

public sealed record SubjectScore(string Subject, int Score);

[StaticDataRecord("GameItems", "Items")]
public sealed partial record ItemRecord(
    int Id,
    string Name,
    int Price,
    ItemCategory Category);
```

</br></br>

### 단일 Record 의 헤더 — 파일 출력

```bash
StaticDataHeaderGenerator.exe header ^
  --record-path ./Records ^
  --record-name StudentRecord ^
  --output-file ./Headers/Student.md
```

결과 `./Headers/Student.md`:

````markdown
# StaticDataHeaderGenerator Results

## StudentRecord
- Excel File: `StudentReport.xlsx`
- Sheet Name: `Grades`

### Headers (List)
- Id
- Name
- Subjects[0].Subject
- Subjects[0].Score
- Subjects[1].Subject
- Subjects[1].Score
- Subjects[2].Subject
- Subjects[2].Score

### Headers (TSV)
```
Id	Name	Subjects[0].Subject	Subjects[0].Score	Subjects[1].Subject	Subjects[1].Score	Subjects[2].Subject	Subjects[2].Score
```
````

`### Headers (TSV)` 코드 블록 안의 한 줄을 복사해 Excel 헤더 첫 셀에 붙여 넣으면 자동으로 펼쳐집니다 ([3.2 — Excel 에 붙여넣기](../03-usage/02-header-generator.md#excel-에-붙여넣기)).

</br></br>

### 단일 Record 의 헤더 — 콘솔 출력

```bash
StaticDataHeaderGenerator.exe header ^
  --record-path ./Records ^
  --record-name StudentRecord
```

`--output-file` 을 생략하면 위 Markdown 문서가 콘솔에만 출력됩니다.

</br></br>

### 폴더 전체 — 한 파일로

```bash
StaticDataHeaderGenerator.exe all-header ^
  --record-path ./Records ^
  --output-file ./Headers/AllHeaders.md
```

`./Records` 아래 모든 `[StaticDataRecord]` Record 가 한 Markdown 파일 안에 시트별 섹션으로 정리됩니다. 결과 `./Headers/AllHeaders.md`:

````markdown
# StaticDataHeaderGenerator Results

## ItemRecord
- Excel File: `GameItems.xlsx`
- Sheet Name: `Items`

### Headers (List)
- Id
- Name
- Price
- Category

### Headers (TSV)
```
Id	Name	Price	Category
```

## StudentRecord
- Excel File: `StudentReport.xlsx`
- Sheet Name: `Grades`

### Headers (List)
- Id
- Name
- Subjects[0].Subject
- Subjects[0].Score
- Subjects[1].Subject
- Subjects[1].Score
- Subjects[2].Subject
- Subjects[2].Score

### Headers (TSV)
```
Id	Name	Subjects[0].Subject	Subjects[0].Score	Subjects[1].Subject	Subjects[1].Score	Subjects[2].Subject	Subjects[2].Score
```
````

데이터 작업자는 자신의 시트에 해당하는 섹션을 찾아, `### Headers (TSV)` 안의 한 줄을 복사해 Excel 에 붙여 넣습니다.

</br></br>

### 구분자 변경

기본 구분자는 탭이지만, 쉼표나 다른 글자로 바꿀 수 있습니다.

```bash
StaticDataHeaderGenerator.exe header ^
  --record-path ./Records ^
  --record-name StudentRecord ^
  --separator , ^
  --output-file ./Headers/Student.md
```

같은 `StudentRecord` 를 두 구분자로 뽑은 결과를 비교해 보면, **`### Headers` 섹션의 라벨과 그 아래 코드 블록 안의 한 줄** 만 바뀝니다.

탭 구분자 (`--separator` 생략, 기본):

````markdown
### Headers (TSV)
```
Id	Name	Subjects[0].Subject	Subjects[0].Score	...
```
````

쉼표 구분자 (`--separator ,`):

````markdown
### Headers (CSV)
```
Id,Name,Subjects[0].Subject,Subjects[0].Score,...
```
````

문서 나머지 부분 (제목, `Excel File`, `Sheet Name`, `Headers (List)`) 은 그대로입니다. 탭/쉼표가 아닌 다른 구분자를 쓰면 라벨이 `### Headers` (괄호 없이) 로 표기됩니다.

Excel 에 붙여 넣을 때는 **탭 구분자가 가장 편합니다** — 한 셀에 붙여 넣으면 자동으로 옆 칸으로 펼쳐집니다. 쉼표 등 다른 구분자는 Excel 의 "텍스트 나누기" 같은 변환 단계가 한 번 더 필요할 수 있습니다.

</br></br>

#### 헤더에 구분자가 들어 있으면 생성이 차단됩니다

선택한 구분자가 어떤 헤더 이름 안에 그대로 들어 있으면 (예: `--separator ,` 인데 `Sub,Total` 같은 헤더가 만들어지는 경우), 붙여 넣은 뒤 컬럼이 잘못 쪼개져 데이터 정합성이 깨집니다. 헤더 생성기는 이런 충돌을 발견하면 `InvalidOperationException` 으로 즉시 중단하고, 충돌한 헤더 목록을 메시지에 담아 보고합니다. 보통 record 파라미터 이름이나 `[ColumnName]` 값에 구분자가 들어가지 않도록 정리하면 해결됩니다.

</br></br></br>

## bat 으로 묶어 두기

매번 옵션을 외워 입력하지 않도록 **bat 파일 하나** 를 Record 폴더 옆에 두면 사용이 편리합니다.

```bat
@echo off
StaticDataHeaderGenerator.exe all-header ^
  --record-path .\Records ^
  --output-file .\Headers\AllHeaders.md
pause
```

`pause` 가 있으면 결과 메시지를 확인한 뒤 창이 닫히므로 더블 클릭으로 안심하고 실행할 수 있습니다. 결과 파일은 모든 시트의 헤더 섹션을 모은 Markdown 문서입니다.

로컬에서는 bat 이 편하지만, 같은 명령을 GitHub Actions 같은 CI 의 한 스텝으로 등록해 두면 빌드 과정에 그대로 통합할 수 있습니다.

</br></br></br>

## 권장 워크플로우

1. Record `.cs` 가 어느 정도 확정되면 `all-header` 로 전체 헤더를 한 Markdown 파일에 뽑습니다.
2. 데이터 작업자가 해당 파일에서 자신의 시트에 해당하는 섹션을 찾아 `### Headers (TSV)` 안의 한 줄을 복사해 Excel 에 붙여 넣습니다 ([3.2](../03-usage/02-header-generator.md#excel-에-붙여넣기)).
3. Record 가 바뀔 때마다 bat 으로 다시 뽑으면 됩니다 — 같은 위치에서 새 Markdown 파일이 만들어지고, 데이터 작업자는 같은 자리에서 갱신된 헤더를 받습니다.

CI 환경에서 헤더 생성기를 실행해 결과 Markdown 파일을 산출물로 올려 두면, 데이터 작업자는 항상 최신 버전을 같은 자리에서 받을 수 있습니다.

---

[← 이전: 3.7 StaticDataView 사전 생성 뷰](../03-usage/07-static-data-view.md) | [목차](../README.md) | [다음: 4.2 ExcelColumnExtractor →](./02-column-extractor.md)
