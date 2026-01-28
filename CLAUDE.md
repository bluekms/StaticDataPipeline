# CLAUDE.md

## 프로젝트 개요

Excel Data Solution

## 코딩 컨벤션

### 기본 포맷팅

- 인코딩: UTF-8
- 줄 끝: LF
- 들여쓰기: 스페이스 4칸 (C# 파일)
- 최대 줄 길이: 120자
- 파일 끝에 빈 줄 추가
- 후행 공백 제거

### C# 스타일 규칙

#### var 사용
- **항상 var를 우선 사용**
- 타입이 명확하거나, 내장 타입이거나, 그 외 모든 경우에 var 사용

```csharp
// Good
var user = new User();
var count = 10;
var result = GetResult();

// Avoid
User user = new User();
int count = 10;
```

#### 중괄호 규칙
- **한 줄 코드여도 반드시 중괄호 사용**
- Allman 스타일 (새 줄에 중괄호)

```csharp
// Good
if (condition)
{
    DoSomething();
}

// Bad
if (condition)
    DoSomething();

if (condition) DoSomething();
```

#### Namespace
- **File-scoped namespace 사용**

```csharp
// Good
namespace NK.LobbyWebAPI.Feature.Arena;

public class MyClass { }

// Avoid
namespace NK.LobbyWebAPI.Feature.Arena
{
    public class MyClass { }
}
```

#### Using 문
- System namespace를 먼저 정렬
- namespace 바깥에 위치

```csharp
using System;
using System.Threading;
using Microsoft.AspNetCore.Mvc;
using NK.LobbyWebAPI.Authentication.UserSessions;
```

#### Switch 문
- case 내용 들여쓰기
- case가 블록일 때 추가 들여쓰기 없음

```csharp
switch (value)
{
    case 1:
        DoSomething();
        break;
    case 2:
    {
        var x = 1;
        DoOther(x);
        break;
    }
}
```

#### Modifier 순서
```
public, private, protected, internal, required, file, static, extern, new, virtual, abstract, sealed, override, readonly, unsafe, volatile, async
```

#### empty string 사용

```aiignore
// Good
string s = string.Empty;

// bad
string s = "";
```

### 로깅

- **`logger.LogXxx()` 스타일 유지**
- CA1873 경고(LoggerMessage 패턴 권장)가 발생해도 기존 스타일 유지
- 특별히 지시하지 않는 한 `LoggerMessage` attribute 패턴으로 변환하지 않음

```csharp
// 이 스타일 유지
logger.LogError("There are {Count} exceptions.", count);

// 이 스타일로 변환하지 않음
[LoggerMessage(Level = LogLevel.Error, Message = "...")]
private static partial void LogExceptionCount(ILogger logger, int count);
```

### Nullable

- **Nullable reference types 활성화됨**
- null 가능성을 명시적으로 표기

```csharp
// nullable
string? nullableString;

// non-nullable
string nonNullableString;
```

### Record 타입

- 간단한 데이터 클래스는 record 사용
- Primary constructor 문법 활용

```csharp
public sealed record ArenaRanker(long Rank, int Rating, WholeUserData User);
```

### 문서화

- 공개 API에 대한 XML 주석은 필수가 아님 (CS1591 silent)
- 필요한 경우에만 주석 추가

### 테스트 (xUnit)

- 컬렉션 크기가 1개인지 검증할 때 `Assert.Single()` 사용

```csharp
// Good
var record = Assert.Single(result);
Assert.Equal("expected", record.Name);

// Bad (xUnit2013)
Assert.Equal(1, result.Count);
```


## 응답 규칙

- 작업 완료 후 제목과 수정 내용을 간결하게 작성
- 제목에 명확히 "제목: " 해서 작성
- 과장되지 않은 표현 사용


## 빌드

* 빌드하지 않는다
* 커밋하지 않는다
