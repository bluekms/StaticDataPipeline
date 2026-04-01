using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;

namespace Sdp.Table;

public sealed class UniqueIndex<TRecord, TKey>
    where TRecord : notnull
    where TKey : notnull
{
    private readonly FrozenDictionary<TKey, TRecord> index;

    public UniqueIndex(IReadOnlyList<TRecord> records, Func<TRecord, TKey> keySelector)
    {
        var dictionary = new Dictionary<TKey, TRecord>(records.Count);
        foreach (var record in records)
        {
            var key = keySelector(record);
            if (!dictionary.TryAdd(key, record))
            {
                throw new InvalidOperationException(
                    FormattableString.Invariant($"Duplicate key '{key}' in {typeof(TRecord).Name}."));
            }
        }

        index = dictionary.ToFrozenDictionary();
    }

    public TRecord Get(TKey key)
    {
        if (index.TryGetValue(key, out var record))
        {
            return record;
        }

        throw new KeyNotFoundException(
            FormattableString.Invariant($"Key '{key}' not found in {typeof(TRecord).Name}."));
    }

    public bool TryGet(TKey key, [NotNullWhen(true)] out TRecord? record)
    {
        if (index.TryGetValue(key, out record))
        {
            return true;
        }

        record = default;
        return false;
    }
}
