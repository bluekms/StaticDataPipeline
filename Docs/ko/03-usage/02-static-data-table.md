# 3.2 StaticDataTable 구현

Record는 "한 행의 모양" 이고, `StaticDataTable` 은 "그 Record 들을 담는 테이블" 입니다. 이번 장에서는 세트 A의 `Item` 에 대응하는 `ItemTable` 을 만들고, CSV에서 로드하고 조회하는 방법까지 다룹니다.

## 테이블 정의

```csharp
using System.Collections.Immutable;
using Sdp.Table;

public sealed class ItemTable : StaticDataTable<ItemTable, Item, int>
{
    public ItemTable(ImmutableList<Item> records)
        : base(records, x => x.Id)
    {
    }
}
```

네 가지만 기억하면 됩니다.

1. **CRTP** 로 자기 자신을 타입 인자에 넣는다: `StaticDataTable<ItemTable, Item, int>`.
2. 타입 인자 세 개는 순서대로 `TSelf`, `TRecord`, `TKey`.
3. 반드시 `ImmutableList<TRecord>` 하나만 받는 생성자를 제공한다. Sdp가 리플렉션으로 이 생성자를 호출한다.
4. `base(records, x => x.Id)` — 두 번째 인자는 **키 선택 람다**. Record의 `[Key]` 로 지정한 속성과 같은 것을 반환해야 한다.

## CSV에서 로드

`CreateAsync(csvDir)` 정적 메서드로 CSV 디렉터리에서 자동으로 테이블을 만듭니다.

```csharp
var table = await ItemTable.CreateAsync("./csv");
```

Sdp는 내부에서 다음을 수행합니다.

1. `[StaticDataRecord("Items", "Items")]` 로부터 `Items.Items.csv` 경로를 조립 (`./csv/Items.Items.csv`).
2. CSV를 파싱해 각 행을 `Item` Record 인스턴스로 매핑.
3. `ImmutableList<Item>` 으로 모아서 `ItemTable` 생성자에 전달.
4. 내부에 `FrozenDictionary<int, Item>` 기반 유니크 인덱스를 구축.

`csvDir` 에 파일 경로를 직접 줄 수도 있습니다. `CreateAsync("./csv/Items.Items.csv")` 처럼 파일을 가리키면 그 파일을 바로 읽습니다.

## 조회

```csharp
// 기본 키로 단건 조회 — 없으면 KeyNotFoundException
var potion = table.Get(1);

// Try 패턴
if (table.TryGet(2, out var sword))
{
    Console.WriteLine($"{sword.Name}: {sword.Price}");
}

// 전체 순회
foreach (var item in table.Records)
{
    // ...
}
```

`Records` 는 `IReadOnlyList<Item>` 입니다. 원본이 `ImmutableList` 이므로 수정 시도는 컴파일 또는 런타임에 차단됩니다.

## 사용자 정의 검증 훅 — `Validate()`

테이블 단일 관점에서 "로드가 끝난 직후 한 번 점검할 규칙" 을 `Validate()` 메서드 override 로 표현할 수 있습니다. `CreateAsync` 가 Record 매핑과 인덱스 구축을 마친 뒤 마지막으로 호출합니다.

```csharp
public sealed class ItemTable : StaticDataTable<ItemTable, Item, int>
{
    public ItemTable(ImmutableList<Item> records)
        : base(records, x => x.Id)
    {
    }

    protected override void Validate()
    {
        foreach (var item in Records)
        {
            if (item.Price < 0)
            {
                throw new InvalidOperationException(
                    $"Item {item.Id} 의 Price 가 음수입니다.");
            }
        }
    }
}
```

여기서 던지는 예외는 `CreateAsync` 호출자까지 그대로 전달됩니다. Attribute 로 표현하기 어려운 도메인 제약 (예: "같은 카테고리 내에서 이름 중복 금지") 은 여기에 두는 것이 자연스럽습니다.

테이블 **간** 교차 제약이 필요하다면 `StaticDataTable.Validate()` 가 아니라 [StaticDataManager.Validate(tableSet)](./06-static-data-manager.md) 를 사용하세요. 이쪽은 모든 테이블이 로드되고 FK까지 검증된 이후에 호출됩니다.

## 보조 인덱스: UniqueIndex / MultiIndex

`StaticDataTable` 은 기본 키로 하나의 유니크 인덱스를 자동으로 만듭니다. 다른 속성으로도 조회가 잦다면 보조 인덱스를 직접 준비해 두는 것이 편합니다. Sdp는 두 가지를 제공합니다.

### UniqueIndex\<TRecord, TKey\> — 유일 키로 조회

Record 컬렉션에 대해 특정 속성을 **유니크 키** 로 보는 인덱스입니다. 중복이 있으면 생성자에서 `InvalidOperationException` 이 발생합니다.

```csharp
using Sdp.Table;

public sealed class ItemTable : StaticDataTable<ItemTable, Item, int>
{
    private readonly UniqueIndex<Item, string> byName;

    public ItemTable(ImmutableList<Item> records)
        : base(records, x => x.Id)
    {
        byName = new UniqueIndex<Item, string>(records, x => x.Name);
    }

    public Item GetByName(string name)
        => byName.Get(name);

    public bool TryGetByName(string name, out Item? record)
        => byName.TryGet(name, out record);
}
```

Record의 `Name` 이 테이블 내에서 유니크하다는 것을 강제하면서, `Name` 으로도 단건 조회를 지원하게 됩니다.

### MultiIndex\<TRecord, TKey\> — 유일하지 않은 키로 그룹 조회

`Get(key)` 는 해당 키에 속한 `IReadOnlyList<TRecord>` 를 반환하고, 키가 없으면 빈 리스트를 돌려줍니다.

```csharp
public sealed class ItemTable : StaticDataTable<ItemTable, Item, int>
{
    private readonly MultiIndex<Item, int> byCategory;

    public ItemTable(ImmutableList<Item> records)
        : base(records, x => x.Id)
    {
        byCategory = new MultiIndex<Item, int>(records, x => x.CategoryId);
    }

    public IReadOnlyList<Item> GetByCategory(int categoryId)
        => byCategory.Get(categoryId);
}
```

"같은 카테고리의 아이템 전부" 같은 조회를 `Records.Where(...)` 로 매번 돌리는 대신, 테이블 생성 시점에 한 번 묶어 두는 패턴입니다.

두 인덱스 모두 내부 저장소는 불변(`FrozenDictionary`, `ImmutableList`)이므로 로드 후 변경되지 않습니다.

## 생성자 접근 제한자

위 예제에서는 생성자를 `public` 으로 열었습니다. 원한다면 `internal` 로도 쓸 수 있습니다. Sdp는 public / non-public 생성자를 모두 탐색해서 호출합니다. 외부 어셈블리에서 `new ItemTable(...)` 직접 호출을 막고 싶다면 `internal` 을 선택합니다.

```csharp
public sealed class ItemTable : StaticDataTable<ItemTable, Item, int>
{
    internal ItemTable(ImmutableList<Item> records)
        : base(records, x => x.Id)
    {
    }
}
```

## 요약

- `StaticDataTable<TSelf, TRecord, TKey>` 를 상속하고 `ImmutableList` 생성자를 제공한다.
- `CreateAsync(csvDir)` 로 로드한다. CSV 파일 이름은 `{ExcelFileName}.{SheetName}.csv`.
- 조회는 `Get`, `TryGet`, `Records`.
- 로드 후 단일 테이블 제약은 `Validate()` override.
- 다른 속성으로의 조회는 `UniqueIndex` / `MultiIndex` 로 보강.

---

[← 이전: 3.1 첫 Record 정의하기](./01-first-record.md) | [목차](../README.md) | [다음: 3.3 Record를 먼저 쓰고 Excel 작업하기 →](./03-record-to-excel.md)
