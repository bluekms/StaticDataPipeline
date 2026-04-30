# Sdp 한국어 문서

Excel 데이터를 C# 레코드로 검증·로드하고 메모리에서 고속으로 조회하는 파이프라인 라이브러리 **StaticDataPipeline (Sdp)** 의 한국어 문서입니다.

## 주요 장점

#### Excel 작업과 코드 작업의 병렬화
기획 단계에서 데이터 구조 합의가 끝난 직후부터, 데이터 생산자와 코드 생산자가 서로를 기다리지 않고 독립적으로 작업할 수 있습니다. 한쪽이 다른 쪽의 진행을 기다리며 멈추는 일이 줄어듭니다.

#### 데이터 스키마의 선언적 정의
Excel 시트에는 타입·외래 키 같은 개념이 없습니다. Sdp 를 사용하면 데이터의 타입, 컬럼, 외래 키 관계를 C# Record 와 Attribute 로 직접 선언할 수 있습니다. 별도의 매핑 코드 없이 선언만으로 데이터가 강타입 객체로 로드됩니다.

#### 사전 유효성 검사
외래 키 무결성, null 표현 처리, 필수 Attribute 누락 같은 검증이 빌드 시점 또는 로드 시점에 자동으로 수행됩니다. 데이터가 운영 환경에 도달하기 전에 결함을 미리 잡아낼 수 있어, 잘못된 데이터로 인한 런타임 사고를 줄일 수 있습니다.

#### 타입 브랜딩 지원
단일 파라미터 record 또는 enum 으로 의미가 다른 ID 들을 별개 타입으로 취급할 수 있습니다. 잘못된 ID 대입이 빌드 단계에서 드러나며, Excel 시트에서는 평범한 한 컬럼으로 표현되어 데이터 작업자에게 부담을 주지 않습니다.

#### 로드된 데이터의 불변성 보장
Record 자체가 불변이고, 컬렉션도 `ImmutableArray`, `FrozenSet`, `FrozenDictionary` 같은 불변 타입에 적재됩니다. 로드 이후 의도치 않은 데이터 변경을 원천적으로 차단할 수 있습니다.

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
- [4.3 타입 브랜딩 패턴](./04-advanced/03-type-branding.md)

### 5. [라이선스](./05-license.md)

---

처음 보는 분은 **1 → 3.1 → 3.2** 순으로 읽으면 가장 빠르게 감을 잡을 수 있습니다. 고급 기능은 `Items` 단일 테이블을 마스터한 다음에 살펴보는 것을 권장합니다.
