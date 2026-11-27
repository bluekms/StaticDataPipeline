using System.Collections.Immutable;
using ScenarioTest.Records.Science;
using Sdp.Attributes;
using Sdp.Table;

namespace ScenarioTest.Tables;

// 키는 원자 번호지만 기호로만 찾는다. 쓰지 않는 기본 키 인덱스를 끈다.
[NoKeyIndex]
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
