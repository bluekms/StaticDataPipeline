using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Sdp.Resources;

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
                throw new InvalidOperationException(string.Format(
                    CultureInfo.CurrentCulture,
                    Messages.Composite.DuplicateKey,
                    key,
                    typeof(TRecord).Name));
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

        throw new KeyNotFoundException(string.Format(
            CultureInfo.CurrentCulture,
            Messages.Composite.KeyNotFound,
            key,
            typeof(TRecord).Name));
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
