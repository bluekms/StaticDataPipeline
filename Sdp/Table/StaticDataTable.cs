using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using Sdp.Attributes;
using Sdp.Csv;
using Sdp.Resources;

namespace Sdp.Table;

public abstract class StaticDataTable<TRecord, TKey> : IStaticDataTable
    where TRecord : notnull
    where TKey : notnull
{
    private readonly UniqueIndex<TRecord, TKey> index;
    private readonly ImmutableList<TRecord> records;
    private readonly string? primaryKeyPropertyName;

    internal StaticDataTable(
        ImmutableList<TRecord> records,
        Func<TRecord, TKey> keySelector,
        string? primaryKeyPropertyName = null)
    {
        this.records = records;
        this.primaryKeyPropertyName = primaryKeyPropertyName;
        index = new UniqueIndex<TRecord, TKey>(records, keySelector);
    }

    protected StaticDataTable(
        string path,
        Expression<Func<TRecord, TKey>> keySelector)
        : this(
            LoadRecords(path),
            keySelector.Compile(),
            ExtractPropertyName(keySelector))
    {
    }

    private static string ExtractPropertyName(Expression<Func<TRecord, TKey>> expr)
    {
        if (expr.Body is MemberExpression memberExpr)
        {
            return memberExpr.Member.Name;
        }

        throw new ArgumentException(Messages.KeySelectorInvalid, nameof(expr));
    }

    private static ImmutableList<TRecord> LoadRecords(string path)
    {
        if (Directory.Exists(path))
        {
            var attr = typeof(TRecord).GetCustomAttribute<StaticDataRecordAttribute>();
            if (attr is null)
            {
                throw new InvalidOperationException(string.Format(
                    CultureInfo.CurrentCulture,
                    Messages.Composite.StaticDataRecordAttributeRequired,
                    typeof(TRecord).Name));
            }

            path = Path.Combine(path, FormattableString.Invariant($"{attr.ExcelFileName}.{attr.SheetName}.csv"));
        }

        return CsvLoader.Load<TRecord>(path);
    }

    public IReadOnlyList<TRecord> Records => records;

    public TRecord Get(TKey key)
        => index.Get(key);

    public bool TryGet(TKey key, [NotNullWhen(true)] out TRecord? record)
        => index.TryGet(key, out record);

    Type IStaticDataTable.RecordType => typeof(TRecord);

    string? IStaticDataTable.PrimaryKeyPropertyName => primaryKeyPropertyName;

    IEnumerable IStaticDataTable.GetAllRecords() => records;

    bool IStaticDataTable.ContainsPrimaryKey(object? value)
        => value is TKey typedKey && index.TryGet(typedKey, out _);
}
