# 1. 소개

## Sdp 소개

**StaticDataPipeline (Sdp)** 은 Excel 에 정의된 정적 데이터를 C# Record 로 로드하고, 검증된 상태의 불변 컬렉션을 메모리에 올려 빠르게 조회하도록 도와주는 파이프라인 라이브러리입니다.

게임 서버와 클라이언트, 시뮬레이션 도구처럼 "한 번 로드해 두고 읽기만 하는" 데이터에 적합합니다.

</br></br></br>

## 주요 장점

#### Excel 작업과 코드 작업의 병렬화
데이터 구조가 합의된 시점부터 데이터 작업자와 레코드 작업자가 서로를 기다리지 않고 독립적으로 진행할 수 있습니다.

</br>

#### 선언적 스키마 정의
타입, 컬럼, 외래 키 관계를 C# Record 와 Attribute 로 직접 선언합니다. 별도 매핑 코드 없이 선언만으로 강타입 객체에 로드됩니다.

</br>

#### 사전 유효성 검사
외래 키 무결성, null 표현 처리, 필수 Attribute 누락 같은 검사를 빌드 단계와 로드 단계에서 자동으로 수행합니다. 잘못된 데이터가 운영 환경까지 흘러가는 일을 줄일 수 있습니다.

</br>

#### 타입 브랜딩 지원
단일 파라미터 record 나 enum 으로 의미가 다른 ID 들을 별개 타입으로 취급할 수 있습니다. 잘못된 ID 대입은 빌드 단계에서 드러나고, Excel 시트에서는 평범한 한 컬럼으로 보입니다 (자세한 내용은 [5.3](./05-advanced/03-type-branding.md)).

</br>

#### 로드된 데이터의 불변성 보장
Record 가 불변이고 컬렉션도 `ImmutableArray`, `FrozenSet`, `FrozenDictionary` 같은 불변 타입에 적재됩니다. 로드 이후 데이터가 의도치 않게 바뀌는 경로 자체가 막혀 있습니다.

</br></br></br>

## 데이터 흐름

```mermaid
flowchart TB
    Excel["Excel 파일<br/>(데이터 작업자)"]
    Record["Record 정의 *.cs<br/>(레코드 작업자)"]
    Extractor["ExcelColumnExtractor<br/>(Roslyn 분석)"]
    Csv["CSV<br/>(필요한 컬럼만)"]
    Table["StaticDataTable&lt;TRecord&gt;<br/>(ImmutableArray 적재)"]
    Manager["StaticDataManager&lt;TTableSet&gt;<br/>(여러 테이블 + FK 검증)"]
    App["애플리케이션 조회<br/>(Get / TryGet)"]

    Excel --> Extractor
    Record --> Extractor
    Extractor --> Csv
    Csv --> Table
    Table --> Manager
    Manager --> App
```

</br></br></br>

## 일단 한번 돌려 보고 싶다면

가장 빠른 길은 [빠른 시작](./quickstart.md) 한 페이지를 끝까지 따라가 보는 것입니다. Record → Table → Manager → 로드 → 조회까지의 골격만 다루며, FK 와 뷰는 빠져 있어 5분이면 끝납니다. 본격적인 사용은 그 다음에 [3.1](./03-usage/01-record-to-excel.md) 부터 따라가면 됩니다.

---

[목차](./README.md) | [다음: 2. 설치 →](./02-installation.md)
