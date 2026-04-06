using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Sdp.Attributes;
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

    protected StaticDataTable(string path, Func<TRecord, TKey> keySelector)
        : this(LoadRecords(path), keySelector)
    {
    }

    private static ImmutableList<TRecord> LoadRecords(string path)
    {
        if (Directory.Exists(path))
        {
            var attr = typeof(TRecord).GetCustomAttribute<StaticDataRecordAttribute>()
                ?? throw new InvalidOperationException(FormattableString.Invariant(
                    $"{typeof(TRecord).Name} requires [StaticDataRecord] attribute when loading from a directory."));

            path = Path.Combine(path, FormattableString.Invariant($"{attr.ExcelFileName}.{attr.SheetName}.csv"));
        }

        return CsvLoader.Load<TRecord>(path);
    }

    public IReadOnlyList<TRecord> Records => records;

    public TRecord Get(TKey key)
        => index.Get(key);

    public bool TryGet(TKey key, [NotNullWhen(true)] out TRecord? record)
        => index.TryGet(key, out record);
}
