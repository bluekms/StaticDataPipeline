# 2. 설치

## 요구 환경

- **.NET SDK** 필요 — 정확한 버전은 리포지토리의 `global.json` 에 명시되어 있습니다

</br></br></br>

## 자리별로 필요한 것

Sdp 가 쓰이는 네 자리 ([1. 소개](./01-introduction.md#네-자리에서-쓰인다)) 마다 설치할 것이 다릅니다. 자기 자리에 해당하는 줄만 준비하면 됩니다.

|자리|필요한 것|필요 없는 것|
|-|-|-|
|**데이터 작업자**|Excel. 표준 헤더는 빌드가 만든 Markdown 을 받아 쓴다. 푸시 전에 직접 검증하고 싶다면 `ExcelColumnExtractor` 실행 파일 (.NET 설치 불필요)|`Sdp.dll`, 소스 제너레이터, .NET SDK|
|**레코드 작업자**|.NET SDK, `Sdp.dll` 참조, `Sdp.SourceGenerator` 분석기 참조 (아래 3)|CLI 두 도구 (로컬에서 추출을 확인하고 싶을 때만)|
|**빌드**|CLI 두 실행 파일, 소비자 프로젝트를 빌드할 .NET SDK|Excel 설치 (추출기는 Excel 없이 `.xlsx` 를 읽는다)|
|**런타임**|`Sdp.dll`, 빌드된 소비자 어셈블리, 추출된 CSV 폴더|CLI 두 도구, 소스 제너레이터, Excel, Record 소스|

</br></br></br>

## 설치 방법

### 1. GitHub Releases에서 받기

[Releases 페이지](https://github.com/bluekms/StaticDataPipeline/releases) 에서 단일 실행 파일을 내려받아 원하는 경로에 배치하거나 PATH 에 추가합니다.

|파일|용도|
|-|-|
|`ExcelColumnExtractor-v<버전>-win-x64.exe` / `-linux-x64`|Excel → CSV 추출 CLI|
|`StaticDataHeaderGenerator-v<버전>-win-x64.exe` / `-linux-x64`|표준 헤더 생성 CLI|
|`Sdp.dll`|런타임 라이브러리 (net10.0 빌드) — 프로젝트에서 참조|

CLI 두 도구는 `--self-contained` 단일 실행 파일이라 .NET 런타임 설치가 따로 필요하지 않습니다. `Sdp.dll` 은 현재 프로젝트에서 직접 참조해야 합니다 (NuGet 패키지 배포는 추후 지원 예정). 아래 3 의 소스 제너레이터도 함께 참조합니다. `Sdp.SourceGenerator.dll` 은 Releases 에 올라가지 않으므로 아래 2 의 소스 빌드 결과물 `Sdp.SourceGenerator/bin/Release/netstandard2.0/Sdp.SourceGenerator.dll` 에서 가져옵니다. 유니티용 netstandard2.1 `Sdp.dll` 도 Releases 에 없어 소스 빌드 결과물 `Sdp/bin/Release/netstandard2.1/Sdp.dll` 을 씁니다.

### 2. 소스 빌드

```bash
git clone https://github.com/bluekms/StaticDataPipeline.git
cd StaticDataPipeline
dotnet build -c Release
```

### 3. 소스 제너레이터 참조

CSV 매핑, 테이블 로드, FK 검증, View 빌드 코드는 `Sdp.SourceGenerator` 가 컴파일 시점에 생성합니다. Record, Table, Manager 를 선언하는 프로젝트는 `Sdp` 와 함께 `Sdp.SourceGenerator` 를 **분석기로** 참조해야 합니다.

소스 빌드로 가져온 경우:

```xml
<ItemGroup>
  <ProjectReference Include="..\StaticDataPipeline\Sdp\Sdp.csproj" />
  <ProjectReference Include="..\StaticDataPipeline\Sdp.SourceGenerator\Sdp.SourceGenerator.csproj"
                    OutputItemType="Analyzer"
                    ReferenceOutputAssembly="false" />
</ItemGroup>
```

dll 로 받은 경우 (`Sdp.dll` 은 Releases 에서, `Sdp.SourceGenerator.dll` 은 소스 빌드의 `Sdp.SourceGenerator/bin/Release/netstandard2.0/` 에서 `libs` 로 복사했다고 가정):

```xml
<ItemGroup>
  <Reference Include="Sdp">
    <HintPath>libs\Sdp.dll</HintPath>
  </Reference>
  <Analyzer Include="libs\Sdp.SourceGenerator.dll" />
  <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="9.0.7" />
</ItemGroup>
```

`<Reference HintPath>` 는 `Sdp.dll` 이 의존하는 패키지를 전이시키지 않으므로 `Microsoft.Extensions.Logging.Abstractions` (9.0.7) 를 소비 프로젝트에 직접 추가합니다. netstandard2.1 빌드의 `Sdp.dll` 을 쓰는 경우에는 `System.Collections.Immutable` (9.0.7) 도 함께 추가합니다.

무엇이 생성되는지, 생성 코드를 확인하는 법, Unity 2021 LTS 처럼 생성기를 실행할 수 없는 환경에서 쓰는 법은 [5.5 소스 제너레이터](./05-advanced/05-source-generator.md) 에 있습니다.

---

[← 이전: 1. 소개](./01-introduction.md) | [목차](./README.md) | [다음: 3.1 Excel 작업하기 →](./03-usage/01-record-to-excel.md)
