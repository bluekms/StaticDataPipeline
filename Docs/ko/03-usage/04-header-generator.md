# 3.4 표준 헤더 생성기

`StaticDataHeaderGenerator` 는 Record `.cs` 를 읽어서 **Excel 에 붙여넣을 수 있는 표준 헤더** 를 출력해 주는 CLI 도구입니다.

세트 A의 `Item` 처럼 단순한 Record에는 큰 이점이 없지만, 컬렉션이나 중첩 Record가 들어간 Record에서는 헤더를 손으로 맞추기 어려워집니다. 이 도구는 그 헤더를 자동으로 조립합니다.

## 왜 필요한가

예를 들어 다음과 같은 Record가 있다고 합시다.

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

`Scores` 는 키 3개짜리 Dictionary이고, 값 Record인 `SubjectScore` 는 필드 2개(`Subject`, `Score`)를 갖습니다. Excel 헤더는 이 구조를 펼친 모양이어야 합니다.

```
Name | SubjectScores[0].Subject | SubjectScores[0].Score | SubjectScores[1].Subject | SubjectScores[1].Score | SubjectScores[2].Subject | SubjectScores[2].Score
```

헤더가 7개짜리로 길어지며, 중첩 Record가 더 깊어지면 손으로 맞추기가 현실적으로 어렵습니다. 이 도구가 이 조립을 자동화합니다.

## 사용법

기본 실행:

```bash
StaticDataHeaderGenerator.exe ^
  --record-path ./Records ^
  --output-file ./Headers/Student.tsv
```

Record 디렉터리에 여러 Record가 있을 때 하나만 선택하려면:

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

출력 파일은 탭(또는 지정한 구분자)으로 나뉜 한 줄짜리 헤더입니다. Excel의 A1 셀에 붙여넣으면 각 헤더가 한 컬럼씩 자동으로 분리됩니다. 그 밑에 데이터 행을 채워 넣으면 `ExcelColumnExtractor` 가 읽을 수 있는 시트가 됩니다.

## 권장 작업 순서

1. C# 쪽에서 Record를 먼저 확정한다.
2. `StaticDataHeaderGenerator` 로 헤더를 뽑아 Excel A1 에 붙여넣는다.
3. Excel 담당자가 데이터 행을 채운다.
4. `ExcelColumnExtractor` 로 CSV를 뽑는다.
5. 애플리케이션이 `StaticDataTable.CreateAsync` 로 CSV를 로드한다.

---

[← 이전: 3.3 Record를 먼저 쓰고 Excel 작업하기](./03-record-to-excel.md) | [목차](../README.md) | [다음: 3.5 복잡한 Record →](./05-complex-record.md)
