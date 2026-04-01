using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Sdp.Table;

public abstract class StaticDataTable<TRecord, TKey>
    where TRecord : notnull
    where TKey : notnull
{
    private readonly UniqueIndex<TRecord, TKey> index;
    private readonly ImmutableList<TRecord> records;

    protected StaticDataTable(ImmutableList<TRecord> records, Func<TRecord, TKey> keySelector)
    {
        this.records = records;
        index = new UniqueIndex<TRecord, TKey>(records, keySelector);
    }

    public IReadOnlyList<TRecord> Records => records;

    public TRecord Get(TKey key)
        => index.Get(key);

    public bool TryGet(TKey key, [NotNullWhen(true)] out TRecord? record)
        => index.TryGet(key, out record);
}
