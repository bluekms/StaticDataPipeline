# 4.3 기타 공개 API

이 장은 사용법 편에서 다루지 않은 나머지 공개 API 들을 짧게 정리합니다. 대부분은 기본 사용에서 필요하지 않지만, 특정 상황에서 유용합니다.

`UniqueIndex` / `MultiIndex` 는 [3.2 StaticDataTable 구현](../03-usage/02-static-data-table.md) 에서 먼저 다룹니다. 여기서는 나머지만 정리합니다.

## StaticDataManager.LoadAsync 의 파라미터

`LoadAsync(string csvDir, List<string>? disabledTables = null)` 의 두 번째 인자 `disabledTables` 는 TableSet 생성자 파라미터 이름 목록으로, 포함된 테이블의 로드를 건너뜁니다. CI에서 일부 테이블만 빠르게 검증하거나, 아직 준비되지 않은 테이블을 임시 제외할 때 씁니다. 자세한 동작과 주의점은 [3.6 StaticDataManager](../03-usage/06-static-data-manager.md#특정-테이블-건너뛰기) 를 참조하세요.

## StaticDataManager.Current

로드 완료 후 최신 TableSet을 읽는 `protected` 속성입니다.

- `volatile` 필드에 저장되므로, 재로드 중 다른 스레드가 조회하면 직전 스냅샷이 반환됩니다.
- 최초 로드 이전에 접근하면 `NullReferenceException` 이 발생합니다. 현재 명시적 예외로 교체하는 것이 계획되어 있습니다.

## CsvLoader / CsvRecordMapper

Sdp 내부에서 CSV 파싱을 담당하는 타입들입니다. **외부에 공개하지 않습니다** (`internal`). 일반 사용자는 `StaticDataTable.CreateAsync` / `StaticDataManager.LoadAsync` 만 쓰면 됩니다.

## SchemaInfoScanner 공개 API

`SchemaInfoScanner` 자체는 CLI 도구들이 내부에서 쓰는 라이브러리입니다. 일반 애플리케이션 코드가 직접 참조할 일은 드뭅니다. 자체 CLI 를 만들거나 스키마 분석 결과를 소비해야 한다면 프로젝트를 참조해 `RecordSchemaLoader`, `RecordComplianceChecker` 등을 호출할 수 있습니다. 인터페이스는 안정화 전이므로 버전 간 변경이 있을 수 있습니다.

스캔 진입점은 `CLICommonLibrary.RecordScanner.Scan` / `ScanAsync` 이며, 다음 검사들을 차례로 수행합니다.

- `RecordComplianceChecker` — 타입·Attribute 호환성 (예: `[NullString]` 누락, 컬렉션에 `[Length]` 누락 등).
- `ForeignKeySchemaChecker` — `[ForeignKey]` / `[SwitchForeignKey]` 인자가 가리키는 **대상 컬럼**이 참조 Record 에 실제로 선언되어 있는지. `[SwitchForeignKey]` 의 `conditionColumnName` 이 같은 Record 에 존재하는지도 함께 확인.
- `TableSetSchemaChecker` — `[ForeignKey]` / `[SwitchForeignKey]` 의 **TableSetName** 이 어떤 TableSet 파라미터 이름에도 매칭되지 않으면 오류. TableSet 후보는 `StaticDataRecord` / `Ignore` 가 붙지 않고 모든 파라미터가 nullable 인 Record 로 식별합니다.

세 검사 모두 실패 시 누적된 오류를 `AggregateException` 으로 throw 하므로, 빌드 시점 (=스캐너 실행 시점) 에 한 번에 모든 잘못된 선언을 확인할 수 있습니다.

## 앞으로의 변경 예고

아래 항목은 배포(v0.9 이후) 전후로 정리할 예정입니다.

- `AggregateException` → Sdp 전용 예외 계층 (`SdpLoadException`, `SdpValidationException` 등) 으로 정리.
- `StaticDataManager.Current` 최초 로드 전 접근 시 명시적 예외.
- `Sdp.csproj` 에서 분석 전용 `Microsoft.CodeAnalysis.CSharp` 의존성 제거 (런타임 라이브러리의 불필요한 의존성).

---

[← 이전: 4.2 Attribute 카탈로그](./02-attributes.md) | [목차](../README.md)
