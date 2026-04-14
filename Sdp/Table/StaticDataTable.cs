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

public abstract class StaticDataTable<TSelf, TRecord, TKey> : IStaticDataTable
    where TSelf : StaticDataTable<TSelf, TRecord, TKey>
    where TRecord : notnull
    where TKey : notnull
{
    private readonly UniqueIndex<TRecord, TKey> index;
    private readonly ImmutableList<TRecord> records;
    private readonly string primaryKeyPropertyName;

    internal StaticDataTable(
        ImmutableList<TRecord> records,
        Func<TRecord, TKey> keySelector,
        string primaryKeyPropertyName)
    {
        this.records = records;
        this.primaryKeyPropertyName = primaryKeyPropertyName;
        index = new UniqueIndex<TRecord, TKey>(records, keySelector);
    }

    protected StaticDataTable(
        ImmutableList<TRecord> records,
        Expression<Func<TRecord, TKey>> keySelector)
        : this(records, keySelector.Compile(), ExtractPropertyName(keySelector))
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

    [SuppressMessage(
        "Design",
        "CA1000:Do not declare static members on generic types",
        Justification = "CRTP 패턴: 서브클래스 이름으로 호출하므로 타입 인자 지정 불필요 (e.g. MyTable.CreateAsync)")]
    public static async Task<TSelf> CreateAsync(string csvDir)
    {
        var records = await LoadRecordsAsync(csvDir);

        var ctor = typeof(TSelf).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
            [typeof(ImmutableList<TRecord>)]);

        if (ctor is null)
        {
            throw new InvalidOperationException(FormattableString.Invariant(
                $"{typeof(TSelf).Name} must have a constructor accepting ImmutableList<{typeof(TRecord).Name}>."));
        }

        var instance = (TSelf)ctor.Invoke([records]);
        instance.Validate();
        return instance;
    }

    protected virtual void Validate()
    {
    }

    private static async Task<ImmutableList<TRecord>> LoadRecordsAsync(string path)
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

        return await CsvLoader.LoadAsync<TRecord>(path);
    }

    public IReadOnlyList<TRecord> Records => records;

    public TRecord Get(TKey key)
        => index.Get(key);

    public bool TryGet(TKey key, [NotNullWhen(true)] out TRecord? record)
        => index.TryGet(key, out record);

    Type IStaticDataTable.RecordType => typeof(TRecord);

    string IStaticDataTable.PrimaryKeyPropertyName => primaryKeyPropertyName;

    IEnumerable IStaticDataTable.GetAllRecords() => records;

    bool IStaticDataTable.ContainsPrimaryKey(object? value)
        => value is TKey typedKey && index.TryGet(typedKey, out _);
}
