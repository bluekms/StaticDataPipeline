# 3.4 StaticDataTable 구현

Record 는 "한 행의 모양" 이고, `StaticDataTable` 은 "그 Record 들을 담는 테이블" 입니다. 이번 장에서는 [3.3](./03-first-record.md) 의 `ItemRecord` 에 대응하는 `ItemTable` 을 만들고, `UniqueIndex` 로 키 조회까지 붙여 봅니다.

## 가장 단순한 테이블 정의

```csharp
using System.Collections.Immutable;
using Sdp.Table;

public sealed class ItemTable(ImmutableArray<ItemRecord> records)
    : StaticDataTable<ItemRecord>(records);
```

두 가지만 기억하면 됩니다.

1. 타입 인자는 레코드 타입 `TRecord` 하나다: `StaticDataTable<ItemRecord>`.
2. 반드시 `ImmutableArray<TRecord>` 하나만 받는 생성자를 제공한다. Sdp 가 리플렉션으로 이 생성자를 호출한다.

이 상태에서는 `Records` 속성으로 전체 목록만 노출됩니다.

```csharp
foreach (var item in table.Records)
{
    // ...
}
```

`Records` 는 `ImmutableArray<ItemRecord>` 이며, CSV 의 행 순서를 그대로 유지합니다.

</br></br></br>

## UniqueIndex 로 키 조회 추가

기본 키로 한 행을 직접 꺼내는 조회가 필요하다면 `UniqueIndex` 를 만들고 Getter 를 노출합니다.

```csharp
using System.Collections.Immutable;
using Sdp.Table;

public sealed class ItemTable : StaticDataTable<ItemRecord>
{
    private readonly UniqueIndex<ItemRecord, int> byId;

    public ItemTable(ImmutableArray<ItemRecord> records)
        : base(records)
    {
        byId = new UniqueIndex<ItemRecord, int>(records, x => x.Id);
    }

    public ItemRecord Get(int id)
        => byId.Get(id);

    public bool TryGet(int id, out ItemRecord? record)
        => byId.TryGet(id, out record);
}
```

`UniqueIndex` 는 생성 시점에 키 중복을 검사합니다. 같은 `Id` 가 둘 이상이면 `InvalidOperationException` 을 던집니다.

```csharp
var potion = table.Get(1);

if (table.TryGet(2, out var sword))
{
    Console.WriteLine($"{sword.Name}: {sword.Price}");
}
```

`Get` 은 키가 없으면 `KeyNotFoundException` 을 던지고, `TryGet` 은 `false` 를 반환합니다. 키 존재가 보장되지 않는 조회에는 `TryGet` 을 사용합니다.

다른 컬럼으로도 같은 방식의 조회가 필요하면 `UniqueIndex<ItemRecord, string> byName` 처럼 인덱스를 추가하면 됩니다. 키가 유일하지 않은 그룹 조회는 `MultiIndex` 를 씁니다.

</br></br></br>

## 테이블 내부 추가 검증

대부분의 검증은 `[Range]`, `[ForeignKey]`, `[RegularExpression]` 같은 Attribute 로 선언적으로 동작합니다. Attribute 만으로 표현하기 어려운 특수한 규칙이 있다면 `Validate` 를 override 해서 사용자 검증 로직을 추가할 수 있습니다. 이 메서드는 테이블이 만들어진 직후 호출되며, StaticDataManager 단계의 FK 검증보다 앞서 실행됩니다.

```csharp
public sealed class ItemTable : StaticDataTable<ItemRecord>
{
    public ItemTable(ImmutableArray<ItemRecord> records)
        : base(records)
    {
    }

    protected override void Validate()
    {
        // 예: 모든 카테고리에 아이템이 최소 하나씩은 있는지 확인
        foreach (var category in Enum.GetValues<ItemCategory>())
        {
            if (!Records.Any(x => x.Category == category))
            {
                throw new InvalidOperationException($"카테고리 {category} 에 해당하는 아이템이 없습니다.");
            }
        }
    }
}
```

위 예처럼 **여러 행에 걸친 조건** 은 한 셀 값만 보는 Attribute 로는 표현할 수 없어 `Validate` 가 적합한 자리입니다. 반대로 "가격이 음수가 아닌지" 같은 단일 값 검사는 `Validate` 가 아니라 `[Range]` 로 선언하는 편이 낫습니다 — 추출 단계에서도 함께 걸러지기 때문입니다.

테이블 간 참조의 유효성은 보통 `[ForeignKey]` / `[SwitchForeignKey]` Attribute 로 표현하는 편이 깔끔합니다 ([3.6](./06-foreign-keys.md)). `Validate` 는 그것으로 표현하기 어려운 자기 테이블 내부 규칙용으로 남겨 둡니다.

</br></br></br>

## CSV 로드는 어떻게?

`ItemTable` 자체에는 CSV 로더가 들어 있지 않습니다. 단일 테이블도, 여러 테이블 묶음도, 모두 `StaticDataManager` 를 통해 로드합니다 ([3.5](./05-static-data-manager.md)). 이 장에서는 "테이블 클래스의 모양" 까지만 다룹니다.

</br></br></br>

## 요약

- `StaticDataTable<TRecord>` 를 상속하고 `ImmutableArray<TRecord>` 생성자를 제공한다.
- `Records` 는 CSV 의 행 순서를 유지한다.
- 키 조회나 그룹 조회는 `UniqueIndex` / `MultiIndex` 를 멤버로 두고 Getter 로 노출한다.
- 실제 CSV 로드는 `StaticDataManager` 가 담당한다.

---

[← 이전: 3.3 첫 Record 정의하기](./03-first-record.md) | [목차](../README.md) | [다음: 3.5 StaticDataManager 로 여러 테이블 관리 →](./05-static-data-manager.md)
