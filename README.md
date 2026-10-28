# StaticDataPipeline (Sdp)

Sdp is a C# pipeline library for static (master) data. It reads the schema
from your C# record definitions, extracts only the columns those records need
from Excel sheets, and loads the data into memory as immutable collections for
fast, thread-safe lookup.

1. Read the schema from C# record definitions.
2. Extract only the required columns from Excel sheets into CSV.
3. Load the CSV as immutable collections and query it in memory.

## Example

Define a record that mirrors one Excel row, back it with a table and a manager,
then load and query:

```csharp
using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using Sdp.Attributes;
using Sdp.Manager;
using Sdp.Table;

// A record describes one Excel row. The Attribute names the file and sheet.
// [Range] is validated automatically — at extraction and at load time.
[StaticDataRecord("GameItems", "Items")]
public sealed partial record ItemRecord(
    int Id,
    string Name,
    [Range(0, 1_000_000)] int Price);

// A table holds the loaded rows. UniqueIndex enables lookup by key.
public sealed partial class ItemTable : StaticDataTable<ItemRecord>
{
    private readonly UniqueIndex<ItemRecord, int> byId;

    public ItemTable(ImmutableArray<ItemRecord> records)
        : base(records)
    {
        byId = new UniqueIndex<ItemRecord, int>(records, x => x.Id);
    }

    public ItemRecord Get(int id) => byId.Get(id);
}

// A manager owns the tables and loads them together.
public sealed partial class GameStaticData(ILogger<GameStaticData> logger)
    : StaticDataManager<GameStaticData.TableSet>(logger)
{
    public sealed partial record TableSet(ItemTable? ItemTable);
}
```

Every record, table, table set, and manager is declared `partial`: the
`Sdp.SourceGenerator` analyzer fills in the CSV mapping, table loading, and
table set assembly code at compile time, so the consuming project must
reference it as an analyzer alongside `Sdp`. See
[5.5 Source Generator](./Docs/ko/05-advanced/05-source-generator.md) for
what is generated and how to reference it.

```csharp
// Load every sheet, then query from an immutable, thread-safe snapshot.
var staticData = new GameStaticData(logger);
await staticData.LoadAsync("./csv");

var potion = staticData.Current.ItemTable!.Get(1);
Console.WriteLine($"{potion.Name}: {potion.Price}");
```

See the [Quick Start](./Docs/en/quickstart.md) for the full walkthrough.

## Documentation

- [English](./Docs/en/README.md)
- [한국어](./Docs/ko/README.md)
- [日本語](./Docs/ja/README.md)
- [简体中文](./Docs/zh/README.md)

Korean is the source of truth. The English, Japanese, and Chinese documentation
is machine-translated by AI.
