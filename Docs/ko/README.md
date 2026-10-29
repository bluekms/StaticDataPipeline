# Sdp 한국어 문서

Excel 데이터를 C# 레코드로 검증, 로드하고 메모리에서 고속으로 조회하는 파이프라인 라이브러리 **StaticDataPipeline (Sdp)** 의 한국어 문서입니다.

## 빠른 시작

처음이라면 **[빠른 시작](./quickstart.md)** 부터 — 5분 안에 Record 정의 → 빌드 → Excel → 추출 → 로드·조회까지 끝내는 한 페이지입니다. Sdp 가 어떤 문제를 풀고 어떤 흐름으로 동작하는지는 [1. 소개](./01-introduction.md) 에서 다룹니다.

Sdp 는 **데이터 작업자, 레코드 작업자, 빌드, 런타임** 네 자리에서 쓰이며, 문서도 그 순서를 따라 구성되어 있습니다. 네 자리가 무엇을 주고받는지는 [1. 소개 — 네 자리에서 쓰인다](./01-introduction.md#네-자리에서-쓰인다) 의 그림 한 장으로 볼 수 있습니다.

</br></br></br>

## 목차

### 1. [소개](./01-introduction.md)
Sdp 가 해결하는 문제, 주요 장점, 네 자리 (데이터 작업자 · 레코드 작업자 · 빌드 · 런타임) 에서 무엇이 쓰이고 무엇이 만들어지는지.

### 2. [설치](./02-installation.md)
요구 환경, 자리별로 필요한 것, 설치 방법, 소스 제너레이터 참조.

### 3. 사용법
예제로 익히는 파이프라인. 일부 챕터는 **데이터 작업자** 가 Excel 을 채울 때, 나머지는 **레코드 작업자** 가 Record/Table/Manager 를 구성할 때 펼쳐 보는 안내입니다. 데이터 구조가 합의된 뒤에는 두 작업을 서로 기다리지 않고 병렬로 진행할 수 있습니다.
- [3.1 Excel 작업하기](./03-usage/01-record-to-excel.md) — 데이터 작업자 관점
- [3.2 표준 헤더 생성기](./03-usage/02-header-generator.md) — 데이터 작업자 관점
- [3.3 첫 Record 정의하기](./03-usage/03-first-record.md) — 레코드 작업자 관점
- [3.4 StaticDataTable 구현](./03-usage/04-static-data-table.md)
- [3.5 StaticDataManager 로 여러 테이블 관리](./03-usage/05-static-data-manager.md)
- [3.6 외래 키 (ForeignKey, SwitchForeignKey)](./03-usage/06-foreign-keys.md)
- [3.7 StaticDataView 사전 생성 뷰](./03-usage/07-static-data-view.md)

### 4. CLI 도구
**빌드** 자리에서 호출하는 두 CLI 도구의 사용법 — 명령 형태, 옵션 전체, bat 예제, CI 스텝 구성.
- [4.1 StaticDataHeaderGenerator](./04-cli-tools/01-header-generator.md)
- [4.2 ExcelColumnExtractor](./04-cli-tools/02-column-extractor.md)

### 5. 고급 기능
- [5.1 지원 타입 (Schemata)](./05-advanced/01-schemata.md)
- [5.2 Attribute 카탈로그](./05-advanced/02-attributes.md)
- [5.3 타입 브랜딩 패턴](./05-advanced/03-type-branding.md)
- [5.4 유효성 검사 개요](./05-advanced/04-validation.md)
- [5.5 소스 제너레이터](./05-advanced/05-source-generator.md)
- [5.6 진단 코드 목록](./05-advanced/06-diagnostics.md)

### 6. [라이선스](./06-license.md)

---

[빠른 시작](./quickstart.md) 으로 한 번 끝까지 돌려 본 뒤, 막히는 자리에서 해당 챕터로 돌아오는 흐름을 권장합니다. 정독한다면 역할에 맞춰 다음과 같이 따라가면 자연스럽습니다.

- **데이터 작업자**: 1 → 3.1 → 3.2 — Excel 작성에 필요한 내용은 이 두 챕터로 충분합니다. 푸시 전에 직접 확인해 보고 싶다면 [3.1 — 푸시 전에 직접 확인하기](./03-usage/01-record-to-excel.md#푸시-전에-직접-확인하기).
- **레코드 작업자**: 1 → 2 → 3.3 부터 끝까지 — Record/Table/Manager/View 구성과 CLI 도구 (4장), 고급 기능 (5장) 까지 전부 훑어 두면 좋습니다. 빌드가 `SDPxxxx` 오류로 멈추면 [5.6](./05-advanced/06-diagnostics.md) 에서 찾습니다.
- **빌드 파이프라인 담당**: 4.1 → 4.2 → 5.5 — 표준 헤더 산출, 추출, 소비자 프로젝트 빌드 (유니티라면 생성 코드 반입) 를 CI 스텝으로 묶는 데 필요한 내용입니다. 어느 검사가 어느 단계에서 도는지는 [5.4](./05-advanced/04-validation.md).
- **런타임 통합 담당**: 3.5 → 3.7 — `LoadAsync` 가 무엇을 어떤 순서로 하는지, `Current` 스냅샷을 어떻게 받아 쓰는지, DI 등록 패턴.
