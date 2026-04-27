using System.Collections;
using System.Collections.Immutable;

namespace Sdp.Table;

public abstract class StaticDataTable<TSelf, TRecord>(ImmutableList<TRecord> records)
    : IStaticDataTable
    where TSelf : StaticDataTable<TSelf, TRecord>
    where TRecord : notnull
{
    public ImmutableList<TRecord> Records => records;

    protected virtual void Validate()
    {
    }

    Type IStaticDataTable.RecordType => typeof(TRecord);

    IEnumerable IStaticDataTable.GetAllRecords() => records;

    void IStaticDataTable.Validate() => Validate();
}
