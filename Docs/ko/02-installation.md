# 2. 설치

## 요구 환경

- **.NET 10.0** 이상
- OS: Windows / Linux / macOS (CLI 도구 `ExcelColumnExtractor`, `StaticDataHeaderGenerator` 는 현재 Windows 바이너리 위주로 배포 예정)

## 설치 방법

### GitHub Releases

> 정식 릴리즈는 아직 게시되지 않았습니다. `v0.9.0` 이후 GitHub Releases를 통해 다음 아티팩트를 배포할 예정입니다.
>
> - `Sdp.<version>.nupkg` — 런타임 라이브러리
> - `ExcelColumnExtractor-<version>-win-x64.zip` — Excel → CSV 추출 CLI
> - `StaticDataHeaderGenerator-<version>-win-x64.zip` — 표준 헤더 생성 CLI
>
> 릴리즈가 게시되면 이 문단에 다운로드 링크가 추가됩니다.

### 소스 빌드 (현재 유일한 방법)

리포지토리를 클론한 뒤 .NET SDK로 빌드합니다.

```bash
git clone https://github.com/bluekms/StaticDataPipeline.git
cd StaticDataPipeline
dotnet build -c Release
```

필요한 산출물:

- `Sdp/bin/Release/net10.0/Sdp.dll` — 애플리케이션에서 참조할 라이브러리
- `ExcelColumnExtractor/bin/Release/net10.0/ExcelColumnExtractor.exe` — CSV 추출 CLI
- `StaticDataHeaderGenerator/bin/Release/net10.0/StaticDataHeaderGenerator.exe` — 헤더 생성 CLI

### NuGet

NuGet 배포는 `v0.9.0` 이후 계획되어 있습니다. 계획된 패키지 ID는 `Sdp` 입니다.

---

[← 이전: 1. 소개](./01-introduction.md) | [목차](./README.md) | [다음: 3.1 첫 Record 정의하기 →](./03-usage/01-first-record.md)
