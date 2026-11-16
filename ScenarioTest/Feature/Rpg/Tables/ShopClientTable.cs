using System.Collections.Immutable;
using ScenarioTest.Records.Rpg;
using Sdp.Table;

namespace ScenarioTest.Tables;

public sealed partial class ShopClientTable(ImmutableArray<ShopClientRecord> records)
    : StaticDataTable<ShopClientRecord>(records);
