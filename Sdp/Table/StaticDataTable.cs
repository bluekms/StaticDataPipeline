using System.Collections.Immutable;
using System.ComponentModel;

namespace Sdp.Table;

public abstract class StaticDataTable<TRecord>
    where TRecord : notnull
{
    protected StaticDataTable(ImmutableArray<TRecord> records)
    {
        Records = records;
        BuildKeyIndex();
    }

    public ImmutableArray<TRecord> Records { get; }

    // 소스 제너레이터가 [Key] 가 있는 Record 의 Table 에 재정의를 만든다. 직접 재정의하지 않는다.
    [EditorBrowsable(EditorBrowsableState.Never)]
    protected virtual void BuildKeyIndex()
    {
    }

    protected virtual void Validate()
    {
    }
}
