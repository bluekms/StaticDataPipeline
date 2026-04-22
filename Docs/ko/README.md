# Sdp 한국어 문서

Excel 데이터를 C# 레코드로 검증·로드하고 메모리에서 고속으로 조회하는 파이프라인 라이브러리 **StaticDataPipeline (Sdp)** 의 한국어 문서입니다.

## 목차

### 1. [소개](./01-introduction.md)
Sdp가 해결하는 문제, 설계 철학, 데이터 흐름.

### 2. [설치](./02-installation.md)
요구 환경과 설치 방법.

### 3. 사용법
예제로 익히는 파이프라인.
- [3.1 첫 Record 정의하기 — Excel에서 시작](./03-usage/01-first-record.md)
- [3.2 StaticDataTable 구현](./03-usage/02-static-data-table.md)
- [3.3 Record를 먼저 쓰고 Excel 작업하기](./03-usage/03-record-to-excel.md)
- [3.4 표준 헤더 생성기](./03-usage/04-header-generator.md)
- [3.5 복잡한 Record: 타입·컬렉션·FK](./03-usage/05-complex-record.md)
- [3.6 StaticDataManager로 여러 테이블 관리](./03-usage/06-static-data-manager.md)

### 4. 고급 기능
- [4.1 지원 타입 (Schemata)](./04-advanced/01-schemata.md)
- [4.2 Attribute 카탈로그](./04-advanced/02-attributes.md)
- [4.3 기타 공개 API](./04-advanced/03-public-api.md)

### 5. [라이선스](./05-license.md)

---

처음 보는 분은 **1 → 3.1 → 3.2** 순으로 읽으면 가장 빠르게 감을 잡을 수 있습니다. 고급 기능은 `Items` 단일 테이블을 마스터한 다음에 살펴보는 것을 권장합니다.
