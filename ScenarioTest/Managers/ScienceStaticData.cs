using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using ScenarioTest.Records.Science;
using Sdp.Manager;
using Sdp.Table;

namespace ScenarioTest.Managers;

public sealed partial class ElementTable : StaticDataTable<ElementRecord>
{
    private readonly UniqueIndex<ElementRecord, string> bySymbol;

    public ElementTable(ImmutableArray<ElementRecord> records)
        : base(records)
    {
        bySymbol = new UniqueIndex<ElementRecord, string>(records, x => x.Symbol);
    }

    public ElementRecord GetBySymbol(string symbol)
        => bySymbol.Get(symbol);
}

public sealed partial class ScienceStaticData(ILogger logger)
    : StaticDataManager<ScienceStaticData.TableSet>(logger)
{
    public sealed partial record TableSet(ElementTable? ElementTable);
}
