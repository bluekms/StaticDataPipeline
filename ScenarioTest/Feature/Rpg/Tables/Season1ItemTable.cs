using System.Collections.Immutable;
using ScenarioTest.Records.Rpg;
using Sdp.Table;

namespace ScenarioTest.Tables;

public sealed partial class Season1ItemTable(ImmutableArray<Season1ItemRecord> records)
    : StaticDataTable<Season1ItemRecord>(records);
