# 5.3 타입 브랜딩 패턴

타입 브랜딩 (type branding) 은 의미가 서로 다른 ID 들을 컴파일러가 구분하도록 만드는 기법입니다. 둘 다 `int` 인 `CharId` 와 `ItemId` 를 그대로 두면 컴파일러는 같은 타입으로 보고 잘못된 대입을 잡아 주지 못합니다. 별도 타입으로 감싸면 잘못된 대입이 빌드 시점에 차단됩니다.

Sdp 에서 타입 브랜딩을 표현하는 방법은 두 가지입니다. **두 방식은 대안 관계** 이며, 함께 쓰는 패턴이 아니라 상황에 따라 한쪽을 고르는 방식입니다.

---

## 방법 1 — 단일 원시 파라미터 record struct

원시 타입을 record struct 한 겹으로 감싼 형태입니다.

```csharp
public record struct CharId(int Value);
public record struct ItemId(int Value);
```

같은 `int` 를 감싸도 record struct 타입이 다르므로 잘못된 대입이 컴파일 에러로 잡힙니다. `record struct` 대신 `record` (참조 타입) 로 선언해도 동일하게 동작합니다.

브랜딩 타입 자체를 nullable (`CharId?`) 로 선언할 수는 없습니다 (스캐너 `NullableRecordNotSupported`, 빌드 `SDP0004`). "값 없음" 이 필요하면 내부 파라미터를 `int?` 로 두고 [`[NullString]`](./02-attributes.md#attr-nullstring) 을 내부 파라미터에 붙입니다.

```csharp
public record struct OptionalCharId([NullString("")] int? Value);
```

```csharp
void GetItem(ItemId id) { ... }

var charId = new CharId(100);
GetItem(charId);   // 컴파일 에러 — CharId 를 ItemId 자리에 넣을 수 없음
```

### CSV 헤더 측면

`record struct CharId(int Value)` 처럼 파라미터가 단 하나이고 그 타입이 원시이면, 헤더는 부모 컬럼 한 칸으로 합쳐집니다. 데이터 작업자 입장에서는 평범한 정수 컬럼이고, C# 측에서만 강타입 감싸기 객체로 받습니다.

```csharp
[StaticDataRecord("Game", "Heroes")]
public sealed partial record HeroRecord(
    [Key] CharId Id,
    string Name);
```

생성 헤더:

```
Id    Name
```

CSV 셀에 `100` 만 적으면 `new CharId(100)` 으로 매핑됩니다. 또한 [`[Key]`](./02-attributes.md#attr-key) 가 붙어 있으므로 `ExcelColumnExtractor` 가 추출 단계에서 이 컬럼의 값 중복을 함께 검사합니다. 이 중복 검사는 셀 문자열을 ordinal 비교하므로 `01` 과 `1` 은 서로 다른 키로 취급되어 통과합니다 (로드 시에는 둘 다 `1` 로 파싱됩니다).

헤더는 축약형 `Id` 와 전체 확장형 `Id.Value` 를 추출기와 런타임 매퍼 모두 허용합니다. 표준 헤더 생성기는 축약형 `Id` 를 출력하고, 시트에 `Id.Value` 가 있어도 그대로 받습니다.

브랜딩 타입이 중첩 Record 안에 있을 때는 **마지막 스칼라 세그먼트만** 축약할 수 있습니다. 예를 들어 `record Middle(Inner Inner)`, `record Inner(long Value)` 를 `Middle Middle` 파라미터로 쓰면 헤더는 `Middle.Inner` 또는 `Middle.Inner.Value` 두 가지만 유효하고, `Middle` 하나로 줄이면 로드에 실패합니다.

---

</br></br></br>

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

enum 멤버에 정수 값을 명시해 두면, 그 값이 곧 CSV 셀에 적히는 값이 됩니다. CSV 셀에 `1001` 이라고 적혀 있고 enum 에 `Fireball = 1001` 로 정의돼 있으면, 매핑 결과는 `SkillId.Fireball` 이 됩니다. 데이터 작업자는 시트에 평범한 정수 `1001` 을 적고, C# 코드는 같은 행을 `SkillId.Fireball` 이라는 이름으로 다룰 수 있습니다 — 매직 넘버 `1001` 을 코드 곳곳에 흩뿌리지 않아도 됩니다.

반대로 enum 에 이름이 없는 값, 예를 들어 셀에 `1004` 가 적혀 있는데 그 값에 대응하는 멤버가 없다면 `(SkillId)1004` 로 매핑됩니다. 자주 참조하는 ID 만 골라 enum 멤버로 이름을 붙이고, 나머지는 정수 값 그대로 두는 식으로 쓸 수 있습니다 (단, 이름 없는 값을 허용하려면 아래의 `[Key]` 위치에서 써야 합니다).

### `[Key]` enum 의 멤버 검사 생략

`[Key]` 로 사용된 enum 은 매핑 시 정의된 멤버인지 검사하지 않습니다. 즉 enum 멤버로 정의되지 않은 정수 값 (`1004`, `9999` 등) 도 받아들여 `(SkillId)1004` 로 매핑됩니다.

이 동작 덕분에 enum 을 **닫힌 집합** 이 아닌 **ID 코드 공간** 으로 활용할 수 있습니다. 데이터 작업자가 새 스킬 ID 를 추가할 때마다 enum 멤버를 갱신할 필요 없이, 이미 알려진 일부 값만 명명해 두고 나머지는 데이터로 추가하면 됩니다.

```csharp
[StaticDataRecord("Game", "Skills")]
public sealed partial record SkillRecord(
    [Key] SkillId Id,   // (SkillId)9999 같은 미정의 값도 그대로 매핑
    string Name);
```

> `[Key]` 가 아닌 일반 enum 파라미터는 미정의 값을 거부합니다. 추출기는 enum 멤버 이름만 허용합니다 (정수 `1`, 대소문자가 다른 `fireball` 모두 추출 실패). 로드 시 생성된 매퍼는 멤버 이름과 멤버의 정수 값 문자열 (예: `1001`) 을 모두 받습니다. 대소문자는 구분합니다. 타입 브랜딩으로 enum 을 쓰려면 `[Key]` 위치에서 사용해야 합니다.

---

</br></br></br>

## 두 방식 비교

|관점|단일 파라미터 record|enum|
|-|-|-|
|선언 형태|`record struct CharId(int Value);`|`enum SkillId { ... }`|
|코드에서의 값 추출|`id.Value`|`(int)id`|
|알려진 값에 이름 부여|불가능 (값은 데이터에서만 옴)|enum 멤버로 자연스럽게 표현 (`SkillId.Fireball`)|
|새 ID 자유 추가|문제없음 (값은 임의 정수)|`[Key]` 일 때만 문제없음 (검사 생략)|
|적합한 상황|순수 ID, 코드에서 자주 생성, 전달|일부 ID 에 명명된 상수가 있고 나머지는 데이터로 추가|

---

</br></br></br>

## 외래 키와의 결합

브랜딩 타입은 `[ForeignKey]` 의 대상으로 그대로 사용할 수 있습니다.

```csharp
public record struct CharId(int Value);

[StaticDataRecord("Game", "Heroes")]
public sealed partial record HeroRecord(
    [Key] CharId Id,
    string Name);

[StaticDataRecord("Game", "Quests")]
public sealed partial record QuestRecord(
    [Key] int Id,
    [ForeignKey("HeroTable", "Id")] CharId AssignedTo);
```

`AssignedTo` 가 `CharId` 타입이므로 다른 ID 와 섞일 일이 없고, 동시에 `HeroTable.Id` 컬럼에 실제 값이 존재하는지 로드 시점에 검증됩니다.

---

</br></br></br>

## Attribute 부착 위치

브랜딩 record 는 바깥 파라미터 (`CharId Id`) 와 내부 파라미터 (`int Value`) 두 자리가 있으므로, Attribute 가 어느 자리에 붙어야 하는지 구분해야 합니다.

|Attribute|부착 위치|잘못 붙였을 때|
|-|-|-|
|[`[Range]`](./02-attributes.md#attr-range) (숫자 형태), [`[NullString]`](./02-attributes.md#attr-nullstring), [`[RegularExpression]`](./02-attributes.md#attr-regularexpression), [`[DateTimeFormat]`](./02-attributes.md#attr-datetimeformat), [`[TimeSpanFormat]`](./02-attributes.md#attr-timespanformat)|내부 파라미터 (`Value`)|바깥 파라미터에 붙이면 빌드 시 `SDP0016` (예: `파라미터 'Id'의 타입에는 [Range]를 적용할 수 없습니다`)|
|[`[Range]`](./02-attributes.md#attr-range) (`typeof` 형태)|내부 파라미터 (`Value`)|바깥 파라미터에 붙이면 빌드 시 `SDP0004`|
|`[Key]` 의 enum 멤버 검사 생략|내부 파라미터 (`Value`)|바깥 파라미터의 `[Key]` 만으로는 내부 enum 의 멤버 검사가 생략되지 않음|
|[`[Key]`](./02-attributes.md#attr-key) (추출기 중복 검사, Map 의 키), [`[ForeignKey]`](./02-attributes.md#attr-foreignkey), [`[SwitchForeignKey]`](./02-attributes.md#attr-switchforeignkey), [`[ColumnName]`](./02-attributes.md#attr-columnname)|바깥 파라미터 (`Id`)|—|

값 검증·변환 Attribute 는 실제로 셀 값을 읽는 내부 파라미터에, 컬럼 단위 의미 (헤더 이름, 키, 외래 키) 를 갖는 Attribute 는 바깥 파라미터에 붙입니다.

```csharp
public record struct CharId([Range(1, 100_000)] int Value);

public record struct SkillKey([Key] SkillId Value);   // enum 멤버 검사 생략은 내부 [Key] 기준

[StaticDataRecord("Game", "Heroes")]
public sealed partial record HeroRecord(
    [Key] [ColumnName("HeroId")] CharId Id,
    [ForeignKey("SkillTable", "Id")] SkillKey MainSkill);
```

---

[← 이전: 5.2 Attribute 카탈로그](./02-attributes.md) | [목차](../README.md) | [다음: 5.4 유효성 검사 개요 →](./04-validation.md)
