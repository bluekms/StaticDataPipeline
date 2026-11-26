using System.Collections.Immutable;
using ScenarioTest.Records.Science;
using Sdp.Table;

namespace ScenarioTest.Tables;

public sealed partial class ElementTable : StaticDataTable<ElementRecord>
{
    private readonly UniqueIndex<ElementRecord, string> bySymbol;

    public ElementTable(ImmutableArray<ElementRecord> records)
        : base(records)
    {
        bySymbol = new UniqueIndex<ElementRecord, string>(records, x => x.Symbol);
    }

    public ElementRecord GetBySymbol(string symbol)
    {
        return bySymbol.Get(symbol);
    }
}
