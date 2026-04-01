using System.Collections.Frozen;
using System.Collections.Immutable;

namespace Sdp.Table;

public sealed class MultiIndex<TRecord, TKey>
    where TRecord : notnull
    where TKey : notnull
{
    private readonly FrozenDictionary<TKey, ImmutableList<TRecord>> index;

    public MultiIndex(IReadOnlyList<TRecord> records, Func<TRecord, TKey> keySelector)
    {
        var groups = new Dictionary<TKey, ImmutableList<TRecord>.Builder>();
        foreach (var record in records)
        {
            var key = keySelector(record);
            if (!groups.TryGetValue(key, out var builder))
            {
                builder = ImmutableList.CreateBuilder<TRecord>();
                groups[key] = builder;
            }

            builder.Add(record);
        }

        index = groups.ToFrozenDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.ToImmutable());
    }

    public IReadOnlyList<TRecord> Get(TKey key)
        => index.TryGetValue(key, out var records) ? records : [];
}
