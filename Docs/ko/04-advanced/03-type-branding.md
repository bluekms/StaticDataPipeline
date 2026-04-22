# 4.3 타입 브랜딩 패턴

타입 브랜딩 (type branding) 은 의미가 서로 다른 ID 들을 컴파일러가 구분하도록 만드는 기법입니다. 둘 다 `int` 인 `CharId` 와 `ItemId` 를 그대로 두면 컴파일러는 같은 타입으로 보고 잘못된 대입을 잡아 주지 못합니다. 별도 타입으로 감싸면 잘못된 대입이 빌드 시점에 차단됩니다.

Sdp 에서 타입 브랜딩을 표현하는 방법은 두 가지입니다. **두 방식은 대안 관계** 이며, 함께 쓰는 패턴이 아니라 상황에 따라 한쪽을 고르는 방식입니다.

---

## 방법 1 — 단일 원시 파라미터 record

원시 타입을 record 한 겹으로 감싼 형태입니다.

```csharp
public sealed record CharId(int Value);
public sealed record ItemId(int Value);
```

같은 `int` 를 감싸도 record 타입이 다르므로 잘못된 대입이 컴파일 에러로 잡힙니다.

```csharp
void GetItem(ItemId id) { ... }

var charId = new CharId(100);
GetItem(charId);   // 컴파일 에러 — CharId 를 ItemId 자리에 넣을 수 없음
```

### CSV 헤더 측면

`record CharId(int Value)` 처럼 파라미터가 단 하나이고 그 타입이 원시이면, 헤더는 부모 컬럼 한 칸으로 합쳐집니다. Excel 작업자 입장에서는 평범한 정수 컬럼이고, C# 측에서만 강타입 감싸기 객체로 받습니다.

```csharp
[StaticDataRecord("Game", "Heroes")]
public sealed record Hero(
    [Key] CharId Id,
    string Name);
```

생성 헤더:

```
Id    Name
```

CSV 셀에 `100` 만 적으면 `new CharId(100)` 으로 매핑됩니다.

---

## 방법 2 — enum

enum 은 그 자체로 별개 타입이므로 다른 enum 이나 정수와 섞이지 않습니다.

```csharp
public enum SkillId
{
    Fireball = 1001,
    Heal = 1002,
    Lightning = 1003,
}
```

### `[Key]` enum 의 멤버 검사 생략

`[Key]` 로 사용된 enum 은 매핑 시 `Enum.IsDefined` 검사가 생략됩니다. 즉 enum 멤버로 정의되지 않은 정수 값 (`1004`, `9999` 등) 도 받아들여 `(SkillId)1004` 로 매핑됩니다.

이 동작 덕분에 enum 을 **닫힌 집합** 이 아닌 **ID 코드 공간** 으로 활용할 수 있습니다. 기획자가 새 스킬 ID 를 추가할 때마다 enum 멤버를 갱신할 필요 없이, 이미 알려진 일부 값만 명명해 두고 나머지는 데이터로 추가하면 됩니다.

```csharp
[StaticDataRecord("Game", "Skills")]
public sealed record Skill(
    [Key] SkillId Id,   // (SkillId)9999 같은 미정의 값도 그대로 매핑
    string Name);
```

> `[Key]` 가 아닌 일반 enum 파라미터는 `Enum.IsDefined` 가 적용되어 미정의 값을 거부합니다. 타입 브랜딩으로 enum 을 쓰려면 `[Key]` 위치에서 사용해야 합니다.

---

## 두 방식 비교

|관점|단일 파라미터 record|enum|
|-|-|-|
|선언 형태|`record CharId(int Value);`|`enum SkillId { ... }`|
|코드에서의 값 추출|`id.Value`|`(int)id`|
|알려진 값에 이름 부여|불가능 (값은 데이터에서만 옴)|enum 멤버로 자연스럽게 표현 (`SkillId.Fireball`)|
|새 ID 자유 추가|문제없음 (값은 임의 정수)|`[Key]` 일 때만 문제없음 (검사 생략)|
|적합한 상황|순수 ID, 코드에서 자주 생성·전달|일부 ID 에 명명된 상수가 있고 나머지는 데이터로 추가|

---

## 외래 키와의 결합

브랜딩 타입은 `[ForeignKey]` 의 대상으로 그대로 사용할 수 있습니다.

```csharp
public sealed record CharId(int Value);

[StaticDataRecord("Game", "Heroes")]
public sealed record Hero(
    [Key] CharId Id,
    string Name);

[StaticDataRecord("Game", "Quests")]
public sealed record Quest(
    [Key] int Id,
    [ForeignKey("Heroes", "Id")] CharId AssignedTo);
```

`AssignedTo` 가 `CharId` 타입이므로 다른 ID 와 섞일 일이 없고, 동시에 `Heroes.Id` 컬럼에 실제 값이 존재하는지 로드 시점에 검증됩니다.

---

[← 이전: 4.2 Attribute 카탈로그](./02-attributes.md) | [목차](../README.md)
