# 5.5 소스 제너레이터

Sdp 2.0 부터 CSV 매핑, 테이블 로드, FK 검증, View 빌드 코드는 런타임 리플렉션이 아니라 **`Sdp.SourceGenerator`** 가 컴파일 시점에 생성합니다. 이 장은 무엇이 생성되는지, 선언 쪽에서 지켜야 할 규칙, 진단을 읽는 법, 생성기를 실행할 수 없는 환경에서 쓰는 법을 정리합니다. 참조 방법은 [2. 설치](../02-installation.md) 에 있습니다.

## 무엇이 생성되나

생성기는 아래 여섯 종류의 선언을 찾아 각각 `partial` 짝 파일을 만듭니다. 사용자 코드가 직접 호출하는 멤버는 없고, 모두 `StaticDataManager.LoadAsync` 가 안에서 씁니다.

|선언|생성되는 멤버|생성 파일|
|-|-|-|
|`[StaticDataRecord]` 가 붙은 record|`MapFromCsvRow(CsvHeaderIndex, string[])` — CSV 한 행을 Record 로 변환. 타입 변환, `[Range]` / `[RegularExpression]` / `[CountRange]` 검증, `[NullString]` 치환이 여기에 들어 있다|`{Record}.CsvMapper.g.cs`|
|`StaticDataTable<TRecord>` 서브클래스|`LoadAsync(csvDir, logger)` — `{파일}.{시트}.csv` 를 읽어 테이블을 만들고 `Validate()` 를 호출|`{Table}.TableFactory.g.cs`|
|TableSet record|`LoadAsync(csvDir, disabledTables, logger)` — 모든 테이블을 병렬 로드. `ValidateForeignKeys(tableSet)` — `[ForeignKey]` / `[SwitchForeignKey]` 값 검증|`{TableSet}.TableSetLoader.g.cs`|
|`StaticDataManager` 서브클래스|추상 훅 `LoadTableSetAsync`, `ValidateForeignKeys`, `BuildViewSet` 의 override — 위 생성 코드로 연결|`{Manager}.StaticDataManager.g.cs`|
|`StaticDataView<TTableSet>` 서브클래스|`BuildView(tableSet)` — 생성자를 호출하고 `Validate()` 실행|`{View}.ViewFactory.g.cs`|
|ViewSet record|`Build(tableSet, logger)` — 각 View 를 만들어 ViewSet 으로 조립|`{ViewSet}.ViewSetBuilder.g.cs`|

생성 코드는 소비자 어셈블리에 들어가므로 `Sdp.dll` 에는 리플렉션 경로가 없습니다. 어떤 타입을 어떻게 읽는지가 전부 컴파일된 코드로 존재하기 때문에 IL2CPP 같은 AOT 환경과 코드 스트리핑에서도 그대로 동작합니다.

</br></br></br>

## 네 자리에서의 위치

생성기는 Sdp 가 쓰이는 네 자리 ([1. 소개](../01-introduction.md#네-자리에서-쓰인다)) 중 두 곳에서 보이고, 나머지 두 곳에는 없습니다.

|자리|생성기의 역할|
|-|-|
|**레코드 작업자**|IDE 가 빌드할 때마다 실행되어 선언 결함을 `SDPxxxx` 오류로 그 자리에 표시한다. 레코드 작업자에게는 실시간 검사기로 보인다|
|**빌드**|`dotnet build` 안에서 실행되어 위 표의 여섯 종류 `*.g.cs` 를 만들고 함께 컴파일한다. 산출물은 생성 코드가 포함된 어셈블리. 유니티 2021 처럼 생성기를 돌릴 수 없는 소비자가 있다면 이 자리에서 `*.g.cs` 를 떨어뜨려 반입한다 (아래 절)|
|**데이터 작업자**|없음|
|**런타임**|없음. 어셈블리 안의 생성 코드만 `LoadAsync` 가 호출한다|

</br></br></br>

## 선언 규칙

생성기가 같은 타입에 코드를 덧붙이므로 **여섯 종류 모두 `partial`** 이어야 합니다. 다른 타입 안에 중첩되어 있다면 바깥 타입도 `partial` 이어야 합니다. 빠뜨리면 아래 진단이 납니다.

|선언|진단|
|-|-|
|Record|`SDP0001`|
|바깥 타입|`SDP0002`|
|TableSet|`SDP0202`|
|테이블|`SDP0203`|
|매니저|`SDP0219`|
|ViewSet|`SDP0301`|
|View|`SDP0303`|

그 밖의 규칙은 각 장에서 다룹니다. TableSet 과 ViewSet 은 주 생성자가 하나여야 하고 (`SDP0006`), 매니저의 타입 인자는 현재 컴파일레이션에 선언된 닫힌 타입이어야 하며 (`SDP0216` ~ `SDP0218`), View 는 매니저와 같은 TableSet 을 대상으로 해야 합니다 (`SDP0308`).

</br></br></br>

## 진단 읽기

생성기는 선언의 결함을 빌드 출력에 `SDPxxxx` 오류로 보고합니다. IDE 에서는 해당 선언 위치에 바로 표시됩니다. 번호대는 세 묶음입니다.

|번호대|대상|
|-|-|
|`SDP00xx`|Record 와 파라미터의 Attribute (매핑 가능 여부)|
|`SDP02xx`|TableSet, 테이블, 매니저, FK 선언|
|`SDP03xx`|ViewSet 과 View|

전체 목록은 [5.6 진단 코드 목록](./06-diagnostics.md) 에 있습니다. 모두 오류 수준이라 빌드가 멈추며, 생성 코드 쪽에서 연쇄 컴파일 오류가 나지 않도록 결함이 있는 선언에는 생성을 건너뛰거나 호출 시 즉시 실패하는 껍데기만 둡니다.

</br></br></br>

## 생성 코드 확인

생성 파일은 기본적으로 메모리에만 있습니다. 디스크에 떨어뜨려 보려면 소비자 프로젝트에 다음을 둡니다.

```xml
<PropertyGroup>
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
  <CompilerGeneratedFilesOutputPath>$(BaseIntermediateOutputPath)Generated</CompilerGeneratedFilesOutputPath>
</PropertyGroup>
```

`obj/Generated/Sdp.SourceGenerator/Sdp.SourceGenerator.SdpIncrementalGenerator/` 아래에 위 표의 이름으로 파일이 생깁니다.

</br></br></br>

## 생성기를 실행할 수 없는 환경

생성기는 Roslyn 4.3 이상의 컴파일러에서 동작합니다. Unity 2021 LTS 처럼 그보다 오래된 컴파일러를 쓰는 환경에서는 생성기를 직접 실행할 수 없으므로, 다음 방식으로 씁니다.

1. 같은 Record, Table, Manager 선언을 가진 별도의 .NET 프로젝트에서 위 설정으로 `*.g.cs` 를 디스크에 떨어뜨린다.
2. 그 파일들을 Unity 프로젝트에 복사한다. 생성 코드는 C# 9 문법만 쓰므로 그대로 컴파일된다.
3. `Sdp.dll` 은 `netstandard2.1` 빌드를 반입한다.

Record 선언이 바뀔 때마다 1 ~ 2 를 다시 돌려야 하므로 빌드 파이프라인의 한 단계로 두는 편이 안전합니다. Unity 6 처럼 Roslyn 4.3 이상을 쓰는 버전은 생성기를 분석기로 넣어 직접 실행할 수 있습니다.

---

[← 이전: 5.4 유효성 검사 개요](./04-validation.md) | [목차](../README.md) | [다음: 5.6 진단 코드 목록 →](./06-diagnostics.md)
