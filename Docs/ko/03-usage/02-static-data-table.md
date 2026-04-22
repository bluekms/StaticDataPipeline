# 3.2 StaticDataTable 구현

Record 는 "한 행의 모양" 이고, `StaticDataTable` 은 "그 Record 들을 담는 테이블" 입니다. 이번 장에서는 [3.1](./01-first-record.md) 의 `Item` 에 대응하는 `ItemTable` 을 만들고, `UniqueIndex` 로 단건 조회까지 붙여 봅니다.

## 가장 단순한 테이블 정의

```csharp
using System.Collections.Immutable;
using Sdp.Table;

public sealed class ItemTable(ImmutableList<Item> records)
    : StaticDataTable<ItemTable, Item>(records);
```

세 가지만 기억하면 됩니다.

1. **CRTP** 로 자기 자신을 첫 번째 타입 인자에 넣는다: `StaticDataTable<ItemTable, Item>`.
2. 타입 인자 두 개는 순서대로 `TSelf`, `TRecord`.
3. 반드시 `ImmutableList<TRecord>` 하나만 받는 생성자를 제공한다. Sdp 가 리플렉션으로 이 생성자를 호출한다.

이 상태에서는 `Records` 속성으로 전체 목록만 노출됩니다.

```csharp
foreach (var item in table.Records)
{
    // ...
}
```

`Records` 는 `ImmutableList<Item>` 이며, **CSV 의 행 순서를 그대로 유지** 합니다. 정렬이 필요한 데이터라면 Excel/CSV 측에서 의도한 순서로 정렬해 두면 됩니다.

## UniqueIndex 로 단건 조회 추가

기본 키로 단건 조회가 필요하다면 `UniqueIndex` 를 만들고 Getter 를 노출합니다.

```csharp
using System.Collections.Immutable;
using Sdp.Table;

public sealed class ItemTable : StaticDataTable<ItemTable, Item>
{
    private readonly UniqueIndex<Item, int> byId;

    public ItemTable(ImmutableList<Item> records)
        : base(records)
    {
        byId = new UniqueIndex<Item, int>(records, x => x.Id);
    }

    public Item Get(int id)
        => byId.Get(id);

    public bool TryGet(int id, out Item? record)
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

다른 컬럼으로도 단건 조회가 필요하면 `UniqueIndex<Item, string> byName` 처럼 인덱스를 추가하면 됩니다. 키가 유일하지 않은 그룹 조회는 `MultiIndex` 를 씁니다 — 자세한 사용은 [4.2](../04-advanced/02-attributes.md) 와 함께 활용하면 자연스럽습니다.

## CSV 로드는 어떻게?

`ItemTable` 자체에는 CSV 로더가 들어 있지 않습니다. 단일 테이블도, 여러 테이블 묶음도, 모두 `StaticDataManager` 를 통해 로드합니다 ([3.6](./06-static-data-manager.md)). 이 장에서는 "테이블 클래스의 모양" 까지만 다룹니다.

## 요약

- `StaticDataTable<TSelf, TRecord>` 를 상속하고 `ImmutableList<TRecord>` 생성자를 제공한다.
- `Records` 는 CSV 의 행 순서를 유지한다.
- 단건/그룹 조회는 `UniqueIndex` / `MultiIndex` 를 멤버로 두고 Getter 로 노출한다.
- 실제 CSV 로드는 `StaticDataManager` 가 담당한다.

---

[← 이전: 3.1 첫 Record 정의하기](./01-first-record.md) | [목차](../README.md) | [다음: 3.3 Record를 먼저 쓰고 Excel 작업하기 →](./03-record-to-excel.md)
