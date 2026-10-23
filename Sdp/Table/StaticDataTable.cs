using System.Collections.Immutable;

namespace Sdp.Table;

public abstract class StaticDataTable<TRecord>(ImmutableArray<TRecord> records)
    where TRecord : notnull
{
    public ImmutableArray<TRecord> Records => records;

    protected virtual void Validate()
    {
    }
}
