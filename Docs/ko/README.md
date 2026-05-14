# Sdp 한국어 문서

Excel 데이터를 C# 레코드로 검증, 로드하고 메모리에서 고속으로 조회하는 파이프라인 라이브러리 **StaticDataPipeline (Sdp)** 의 한국어 문서입니다.

## 빠른 시작

처음이라면 **[빠른 시작](./quickstart.md)** 부터 — 5분 안에 Record 정의 → 로드 → 조회까지 끝내는 한 페이지입니다. Sdp 가 어떤 문제를 풀고 어떤 흐름으로 동작하는지는 [1. 소개](./01-introduction.md) 에서 다룹니다.

## 목차

### 1. [소개](./01-introduction.md)
Sdp가 해결하는 문제, 설계 철학, 데이터 흐름.

### 2. [설치](./02-installation.md)
요구 환경과 설치 방법.

### 3. 사용법
예제로 익히는 파이프라인. 앞쪽 두 챕터는 **데이터 작업자** 가 Excel 을 채워 나갈 때, 뒤쪽 챕터는 **프로그래머** 가 Record/Table/Manager 를 구성할 때 펼쳐 보는 흐름입니다.
- [3.1 Excel 작업하기](./03-usage/01-record-to-excel.md) — 데이터 작업자 관점
- [3.2 표준 헤더 생성기](./03-usage/02-header-generator.md) — 데이터 작업자 관점
- [3.3 첫 Record 정의하기](./03-usage/03-first-record.md) — 프로그래머 관점
- [3.4 StaticDataTable 구현](./03-usage/04-static-data-table.md)
- [3.5 StaticDataManager 로 여러 테이블 관리](./03-usage/05-static-data-manager.md)
- [3.6 외래 키 (ForeignKey, SwitchForeignKey)](./03-usage/06-foreign-keys.md)
- [3.7 StaticDataView 로 사전 생성 뷰 합성](./03-usage/07-static-data-view.md)

### 4. 고급 기능
- [4.1 지원 타입 (Schemata)](./04-advanced/01-schemata.md)
- [4.2 Attribute 카탈로그](./04-advanced/02-attributes.md)
- [4.3 타입 브랜딩 패턴](./04-advanced/03-type-branding.md)

### 5. [라이선스](./05-license.md)

---

[빠른 시작](./quickstart.md) 으로 한 번 끝까지 돌려 본 뒤, 막히는 자리에서 해당 챕터로 돌아오는 흐름을 권장합니다. 정독한다면 **1 → 3.3 → 3.4 → 3.5 → 3.6 → 3.7** 순서가 자연스럽고, 고급 기능은 그다음에 봐도 늦지 않습니다.
