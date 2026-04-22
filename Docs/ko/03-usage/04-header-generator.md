# 3.4 표준 헤더 생성기

`StaticDataHeaderGenerator` 는 Record `.cs` 를 읽어서 **Excel 의 헤더 셀에 붙여넣을 수 있는 표준 헤더** 를 출력해 주는 CLI 도구입니다. 컬렉션이나 중첩 Record 가 섞이면 헤더가 빠르게 길어지는데, 이 도구가 그 조립을 자동으로 처리합니다.

## 왜 필요한가

예를 들어 다음과 같은 Record 가 있다고 합시다.

```csharp
public sealed record SubjectScore(
    [Key] string Subject,
    int Score);

[StaticDataRecord("Scores", "Students")]
public sealed record Student(
    string Name,

    [ColumnName("SubjectScores")]
    [Length(3)]
    FrozenDictionary<string, SubjectScore> Scores);
```

`Scores` 는 키 3개짜리 Dictionary 이고, 값 Record 인 `SubjectScore` 는 필드 2개(`Subject`, `Score`) 를 갖습니다. Excel 헤더는 이 구조를 펼친 모양이어야 합니다.

```
Name | SubjectScores[0].Subject | SubjectScores[0].Score | SubjectScores[1].Subject | SubjectScores[1].Score | SubjectScores[2].Subject | SubjectScores[2].Score
```

헤더가 7개짜리로 길어지며, 중첩 Record 가 더 깊어지면 손으로 맞추기가 현실적으로 어렵습니다. 이 도구가 이 조립을 자동화합니다.

## 사용법

기본 실행:

```bash
StaticDataHeaderGenerator.exe ^
  --record-path ./Records ^
  --output-file ./Headers/Student.tsv
```

Record 디렉터리에 여러 Record 가 있을 때 하나만 선택하려면:

```bash
StaticDataHeaderGenerator.exe ^
  --record-path ./Records ^
  --record-name Student ^
  --output-file ./Headers/Student.tsv
```

## 옵션 전체

|옵션|의미|기본값|
|-|-|-|
|`-r`, `--record-path`|Record `.cs` 파일(또는 디렉터리) 경로|필수|
|`-n`, `--record-name`|대상 Record 이름 (없으면 경로의 모든 Record 대상)|없음|
|`-s`, `--separator`|헤더 간 구분자|`\t`|
|`-o`, `--output-file`|출력 파일 경로 (없으면 콘솔 출력)|없음|
|`-l`, `--log-path`|로그 파일 경로|없음|
|`-v`|최소 로그 레벨|Information|

## 결과 붙여넣기

출력 파일은 탭(또는 지정한 구분자) 으로 나뉜 한 줄짜리 헤더입니다. **`ExcelColumnExtractor` 의 `--start-cell` 로 합의한 시작 셀** ([3.3](./03-record-to-excel.md) 참조) 에 그대로 붙여넣으면 각 헤더가 한 컬럼씩 자동으로 분리됩니다. 그 아래 행부터 데이터를 채워 넣으면 `ExcelColumnExtractor` 가 읽을 수 있는 시트가 됩니다.

## 권장 작업 순서 — 임시 헤더로 병렬 진행

표준 헤더가 확정되기 전에도 Excel 작업자는 데이터를 채울 수 있습니다. **임시 헤더** 를 한 행 위에 두고 작업을 시작하면 Record 작업과 데이터 입력을 병렬로 진행할 수 있습니다.

예를 들어 추출기 옵션을 `--start-cell B3` 으로 합의했다고 합시다. 이때 시트 구성은 다음과 같이 잡습니다.

|셀|용도|
|-|-|
|`A1` ~ `B1`|자유 영역 — 메모, 시트 설명 등을 적어 둘 수 있다 (추출기는 읽지 않음)|
|`B2`|임시 헤더 — Excel 작업자가 작업 편의를 위해 직접 적는 헤더|
|`B3`|표준 헤더 — Record 작업이 끝나면 생성기 출력으로 채울 자리 (작업 중에는 비워 둔다)|
|`B4` 이하|데이터 — 임시 헤더(B2) 를 보면서 채운다|

작업 흐름:

1. C# 작업자와 Excel 작업자가 컬럼 구조와 시작 셀(`B3`) 을 합의한다 (이 시점에는 Record 가 아직 미확정이어도 됨).
2. Excel 작업자는 B2 에 임시 헤더를 적어 두고 B4 부터 데이터를 채워 나간다.
3. C# 작업자가 Record 를 확정한 뒤 `StaticDataHeaderGenerator` 로 표준 헤더를 뽑는다.
4. 생성된 표준 헤더를 B3 에 붙여넣는다.
5. `ExcelColumnExtractor --start-cell B3` 으로 CSV 를 추출한다 (CI 단계에서 자동 실행하는 것을 권장).

임시 헤더(B2) 는 추출기가 읽지 않으므로 그대로 둬도 되고, 깔끔하게 지워도 됩니다. 헤더의 실제 시작은 옵션으로 넘긴 `B3` 이기 때문입니다.

---

[← 이전: 3.3 Record를 먼저 쓰고 Excel 작업하기](./03-record-to-excel.md) | [목차](../README.md) | [다음: 3.5 복잡한 Record →](./05-complex-record.md)
