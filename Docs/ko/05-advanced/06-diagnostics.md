# 5.6 진단 코드 목록

[5.5 소스 제너레이터](./05-source-generator.md) 가 빌드 시점에 보고하는 진단의 전체 목록입니다. 모두 오류 수준이며, 메시지에는 문제가 된 record, 파라미터, 타입 이름이 들어갑니다. 원본은 `Sdp.SourceGenerator/SdpDiagnostics.cs` 와 `AnalyzerReleases.Unshipped.md` 입니다.

## SDP00xx — Record 와 Attribute

|코드|뜻|조치|
|-|-|-|
|`SDP0001`|`[StaticDataRecord]` record 가 `partial` 이 아니다|record 선언에 `partial` 을 붙인다|
|`SDP0002`|생성 대상 타입의 바깥 타입이 `partial` 이 아니다|바깥 class/record 에 `partial` 을 붙인다|
|`SDP0003`|record 에 `[Key]` 가 둘 이상 있다|`[Key]` 를 하나만 남긴다|
|`SDP0004`|파라미터 타입 또는 Attribute 조합을 CSV 매퍼가 지원하지 않는다|[5.1 지원 타입](./01-schemata.md) 의 타입으로 바꾼다|
|`SDP0005`|`[CountRange]` 와 `[Length]` 가 함께 붙어 있다|둘 중 하나만 둔다|
|`SDP0006`|record 에 주 생성자가 없거나 둘 이상이다|`record X(...)` 형태의 주 생성자 하나로 선언한다|
|`SDP0007`|`[CountRange]` 가 `[SingleColumnCollection]` 없이 붙어 있다|`[SingleColumnCollection]` 을 함께 붙인다|
|`SDP0008`|컬렉션 파라미터 자체가 nullable 이다|`ImmutableArray<int>?` 대신 `ImmutableArray<int?>` 처럼 원소를 nullable 로 둔다|
|`SDP0009`|`FrozenDictionary` 에 `[SingleColumnCollection]` 이 붙어 있다|`[Length]` 로 바꾼다|
|`SDP0010`|`[Range]` 경계값이 파라미터 타입의 표현 범위를 벗어난다|경계값을 타입 범위 안으로 조정한다|
|`SDP0011`|숫자 타입에 `typeof` 형식의 `[Range]` 를 썼다|`[Range(min, max)]` 숫자 형식으로 쓴다|
|`SDP0012`|`[StaticDataRecord]` record 가 제네릭이거나 제네릭 타입 안에 있다|제네릭을 제거한다|
|`SDP0013`|`[Length]` 와 `[SingleColumnCollection]` 이 함께 붙어 있다|둘 중 하나만 둔다|
|`SDP0014`|`[Length]` 값이 1 미만이다|1 이상으로 고친다|
|`SDP0015`|`[Range]` 의 최솟값이 최댓값보다 크다|순서를 바로잡는다|
|`SDP0016`|그 파라미터 타입에 적용할 수 없는 Attribute 가 붙어 있다|[5.2 Attribute 카탈로그](./02-attributes.md) 의 대상 타입을 확인한다|
|`SDP0017`|`FrozenDictionary` value record 에 `[Key]` 가 정확히 하나가 아니다|value record 에 `[Key]` 하나를 둔다|
|`SDP0018`|`FrozenDictionary` 키 타입이 value record 의 `[Key]` 타입과 다르다|키 타입을 맞춘다|
|`SDP0019`|`[CountRange]` 경계값이 음수다|0 이상으로 고친다|

</br></br></br>

## SDP02xx — TableSet, 테이블, 매니저, 외래 키

|코드|뜻|조치|
|-|-|-|
|`SDP0202`|TableSet record 가 `partial` 이 아니다|`partial` 을 붙인다|
|`SDP0203`|`StaticDataTable` 서브클래스가 `partial` 이 아니다|`partial` 을 붙인다|
|`SDP0204`|같은 파라미터에 `[ForeignKey]` 와 `[SwitchForeignKey]` 가 함께 붙어 있다|하나만 남긴다|
|`SDP0205`|FK 타겟으로 지정한 이름이 TableSet 멤버에 없다|TableSet 파라미터 이름과 맞춘다|
|`SDP0206`|FK 타겟 컬럼이 대상 Record 에 없다|Record 파라미터 이름과 맞춘다|
|`SDP0207`|FK 타겟 컬럼이 `[SingleColumnCollection]` 이다|단일 값 컬럼을 타겟으로 지정한다|
|`SDP0208`|`[SwitchForeignKey]` 의 조건 (컬럼, 값) 조합이 중복된다|중복 분기를 제거한다|
|`SDP0209`|`[SwitchForeignKey]` 의 조건 컬럼이 같은 Record 에 없다|조건 컬럼 이름을 맞춘다|
|`SDP0210`|한 파라미터의 `[SwitchForeignKey]` 들이 서로 다른 조건 컬럼을 쓴다|같은 조건 컬럼으로 통일한다|
|`SDP0211`|FK 파라미터 타입이 타겟 컬럼 타입과 다르다|타입을 맞춘다|
|`SDP0212`|enum 조건 컬럼에 대해 `[SwitchForeignKey]` 조건값이 어떤 멤버와도 맞지 않는다|멤버 이름 또는 정수 값으로 고친다|
|`SDP0213`|TableSet 파라미터가 `StaticDataTable` 서브클래스가 아니다|테이블 타입으로 바꾼다|
|`SDP0214`|FK 타겟 컬럼이 컬렉션 타입이다|단일 값 컬럼을 타겟으로 지정한다|
|`SDP0215`|테이블의 Record 타입에 `[StaticDataRecord]` 가 없다|Record 에 `[StaticDataRecord]` 를 붙인다|
|`SDP0216`|TableSet 이 현재 컴파일레이션에 선언된 record 가 아니다|매니저와 같은 프로젝트에 record 로 선언한다|
|`SDP0217`|TableSet 멤버의 테이블 타입이 현재 컴파일레이션에 없다|테이블을 같은 프로젝트에 선언한다|
|`SDP0218`|매니저의 타입 인자가 타입 파라미터다|닫힌 타입으로 직접 상속한다|
|`SDP0219`|`StaticDataManager` 서브클래스가 `partial` 이 아니다|`partial` 을 붙인다|
|`SDP0220`|정수 조건 컬럼에 대해 `[SwitchForeignKey]` 조건값을 정수로 해석할 수 없다|정수 표기로 고친다|
|`SDP0221`|bool 조건 컬럼에 대해 `[SwitchForeignKey]` 조건값을 bool 로 해석할 수 없다|`true` / `false` 로 고친다|
|`SDP0222`|char 조건 컬럼에 대해 `[SwitchForeignKey]` 조건값이 한 글자가 아니다|한 글자로 고친다|
|`SDP0223`|Guid 조건 컬럼에 대해 `[SwitchForeignKey]` 조건값을 Guid 로 해석할 수 없다|Guid 표기로 고친다|
|`SDP0224`|`[SwitchForeignKey]` 조건 컬럼이 컬렉션 타입이다|단일 값 컬럼을 조건으로 쓴다|
|`SDP0225`|`[SwitchForeignKey]` 조건값이 표기만 다른 같은 값으로 중복된다 (예: `1` 과 `01`)|중복 분기를 제거한다|
|`SDP0226`|같은 파라미터에 같은 (TableSet, 컬럼) 타겟의 `[ForeignKey]` 가 두 번 붙어 있다|하나만 남긴다|

</br></br></br>

## SDP03xx — ViewSet 과 View

|코드|뜻|조치|
|-|-|-|
|`SDP0301`|ViewSet record 가 `partial` 이 아니다|`partial` 을 붙인다|
|`SDP0302`|ViewSet 파라미터가 nullable 이다|non-nullable 로 선언한다|
|`SDP0303`|`StaticDataView` 서브클래스가 `partial` 이 아니다|`partial` 을 붙인다|
|`SDP0304`|View 에 TableSet 하나만 받는 단일 생성자가 없다|`View(TableSet tables)` 생성자 하나만 둔다|
|`SDP0305`|ViewSet 파라미터가 `StaticDataView` 서브클래스가 아니다|View 타입으로 바꾼다|
|`SDP0306`|ViewSet 이 현재 컴파일레이션에 선언된 record 가 아니다|매니저와 같은 프로젝트에 record 로 선언한다|
|`SDP0307`|서로 다른 TableSet 을 쓰는 매니저들이 같은 ViewSet 을 공유한다|매니저마다 ViewSet 을 따로 둔다|
|`SDP0308`|View 의 `StaticDataView<T>` 의 T 가 매니저의 TableSet 과 다르다|매니저의 TableSet 을 타입 인자로 쓴다|
|`SDP0309`|ViewSet 파라미터의 View 타입이 현재 컴파일레이션에 없다|View 를 같은 프로젝트에 선언한다|

---

[← 이전: 5.5 소스 제너레이터](./05-source-generator.md) | [목차](../README.md) | [다음: 6. 라이선스 →](../06-license.md)
