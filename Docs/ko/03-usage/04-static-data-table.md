# 3.4 StaticDataTable 구현

Record 는 "한 행의 모양" 이고, `StaticDataTable` 은 "그 Record 들을 담는 테이블" 입니다. 이번 장에서는 [3.3](./03-first-record.md) 의 `ItemRecord` 에 대응하는 `ItemTable` 을 만들고, `UniqueIndex` 로 키 조회까지 붙여 봅니다.

## 가장 단순한 테이블 정의

```csharp
using System.Collections.Immutable;
using Sdp.Table;

public sealed class ItemTable(ImmutableList<ItemRecord> records)
    : StaticDataTable<ItemTable, ItemRecord>(records);
```

세 가지만 기억하면 됩니다.

1. **CRTP** 로 자기 자신을 첫 번째 타입 인자에 넣는다: `StaticDataTable<ItemTable, ItemRecord>`.
2. 타입 인자 두 개는 순서대로 `TSelf`, `TRecord`.
3. 반드시 `ImmutableList<TRecord>` 하나만 받는 생성자를 제공한다. Sdp 가 리플렉션으로 이 생성자를 호출한다.

이 상태에서는 `Records` 속성으로 전체 목록만 노출됩니다.

```csharp
foreach (var item in table.Records)
{
    // ...
}
```

`Records` 는 `ImmutableList<ItemRecord>` 이며, CSV 의 행 순서를 그대로 유지합니다.

## UniqueIndex 로 키 조회 추가

기본 키로 한 행을 직접 꺼내는 조회가 필요하다면 `UniqueIndex` 를 만들고 Getter 를 노출합니다.

```csharp
using System.Collections.Immutable;
using Sdp.Table;

public sealed class ItemTable : StaticDataTable<ItemTable, ItemRecord>
{
    private readonly UniqueIndex<ItemRecord, int> byId;

    public ItemTable(ImmutableList<ItemRecord> records)
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

다른 컬럼으로도 같은 방식의 조회가 필요하면 `UniqueIndex<ItemRecord, string> byName` 처럼 인덱스를 추가하면 됩니다. 키가 유일하지 않은 그룹 조회는 `MultiIndex` 를 씁니다.

## 테이블 내부 추가 검증

테이블 자체에서 자기 행들에 대해 한 번 더 검증하고 싶다면 `Validate` 를 override 합니다. 이 메서드는 테이블이 만들어진 직후 호출되며, 매니저 단계의 FK 검증보다 앞서 실행됩니다.

```csharp
public sealed class ItemTable : StaticDataTable<ItemTable, ItemRecord>
{
    public ItemTable(ImmutableList<ItemRecord> records)
        : base(records)
    {
    }

    protected override void Validate()
    {
        // 예: 가격이 음수인 행이 없는지 마지막으로 확인
    }
}
```

테이블 간 참조의 유효성은 보통 `[ForeignKey]` / `[SwitchForeignKey]` Attribute 로 표현하는 편이 깔끔합니다 ([3.6](./06-foreign-keys.md)). `Validate` 는 그것으로 표현하기 어려운 자기 테이블 내부 규칙용으로 남겨 둡니다.

## CSV 로드는 어떻게?

`ItemTable` 자체에는 CSV 로더가 들어 있지 않습니다. 단일 테이블도, 여러 테이블 묶음도, 모두 `StaticDataManager` 를 통해 로드합니다 ([3.5](./05-static-data-manager.md)). 이 장에서는 "테이블 클래스의 모양" 까지만 다룹니다.

## 요약

- `StaticDataTable<TSelf, TRecord>` 를 상속하고 `ImmutableList<TRecord>` 생성자를 제공한다.
- `Records` 는 CSV 의 행 순서를 유지한다.
- 키 조회나 그룹 조회는 `UniqueIndex` / `MultiIndex` 를 멤버로 두고 Getter 로 노출한다.
- 실제 CSV 로드는 `StaticDataManager` 가 담당한다.

---

[← 이전: 3.3 첫 Record 정의하기](./03-first-record.md) | [목차](../README.md) | [다음: 3.5 StaticDataManager 로 여러 테이블 관리 →](./05-static-data-manager.md)
