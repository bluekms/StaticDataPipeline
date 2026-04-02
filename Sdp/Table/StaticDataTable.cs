using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Sdp.Csv;

namespace Sdp.Table;

public abstract class StaticDataTable<TRecord, TKey>
    where TRecord : notnull
    where TKey : notnull
{
    private readonly UniqueIndex<TRecord, TKey> index;
    private readonly ImmutableList<TRecord> records;

    internal StaticDataTable(ImmutableList<TRecord> records, Func<TRecord, TKey> keySelector)
    {
        this.records = records;
        index = new UniqueIndex<TRecord, TKey>(records, keySelector);
    }

    protected StaticDataTable(string filePath, Func<TRecord, TKey> keySelector)
        : this(CsvLoader.Load<TRecord>(filePath), keySelector)
    {
    }

    public IReadOnlyList<TRecord> Records => records;

    public TRecord Get(TKey key)
        => index.Get(key);

    public bool TryGet(TKey key, [NotNullWhen(true)] out TRecord? record)
        => index.TryGet(key, out record);
}
