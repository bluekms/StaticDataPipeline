using System.Collections.Immutable;
using ScenarioTest.Records.Rpg;
using Sdp.Table;

namespace ScenarioTest.Tables;

public sealed partial class ItemTable(ImmutableArray<ItemRecord> records)
    : StaticDataTable<ItemRecord>(records);
