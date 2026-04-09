using System.Globalization;
using SchemaInfoScanner.Catalogs;
using SchemaInfoScanner.Resources;

namespace SchemaInfoScanner.Schemata.TypedPropertySchemata;

public sealed class CompatibilityContext
{
    private enum CollectMode
    {
        None,
        KeyOnly,
    }

    private CompatibilityContext(
        MetadataCatalogs metadataCatalogs,
        IReadOnlyList<CellData> cells,
        CollectMode collectMode,
        int startPosition = 0)
    {
        MetadataCatalogs = metadataCatalogs;
        Cells = cells;
        this.collectMode = collectMode;
        Position = startPosition;
    }

    public static CompatibilityContext CreateNoCollect(
        MetadataCatalogs metadataCatalogs,
        IReadOnlyList<CellData> cells,
        int startPosition = 0)
        => new(metadataCatalogs, cells, CollectMode.None, startPosition);

    public static CompatibilityContext CreateCollectKey(
        MetadataCatalogs metadataCatalogs,
        IReadOnlyList<CellData> cells,
        int startPosition = 0)
        => new(metadataCatalogs, cells, CollectMode.KeyOnly, startPosition);

    public MetadataCatalogs MetadataCatalogs { get; }
    public EnumMemberCatalog EnumMemberCatalog => MetadataCatalogs.EnumMemberCatalog;
    public RecordSchemaCatalog RecordSchemaCatalog => MetadataCatalogs.RecordSchemaCatalog;

    public IReadOnlyList<CellData> Cells { get; }

    public int Position { get; private set; }

    private readonly CollectMode collectMode;
    private readonly List<object?> duplicateCandidates = new();
    private readonly List<object?> keyScopeComponents = new();
    private bool isKeyScope;

    public CellData Current => Cells[Position];

    public CellData Consume()
    {
        if (Position >= Cells.Count)
        {
            throw new InvalidOperationException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.StartIndexOutOfRange,
                Position));
        }

        return Cells[Position++];
    }

    public void Collect(object? key)
    {
        if (collectMode is CollectMode.None)
        {
            return;
        }

        if (collectMode is CollectMode.KeyOnly)
        {
            if (isKeyScope)
            {
                keyScopeComponents.Add(key);
            }

            return;
        }

        throw new InvalidOperationException(Messages.UnreachableCodeInCollect);
    }

    public void ValidateNoDuplicates()
    {
        if (collectMode is CollectMode.None)
        {
            throw new InvalidOperationException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.CannotValidateDuplicatesInNoneMode,
                this));
        }

        if (duplicateCandidates.Count != duplicateCandidates.Distinct().Count())
        {
            throw new InvalidOperationException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.DuplicateValuesInArgument,
                this));
        }
    }

    public void ConsumeNull()
    {
        Position++;
        duplicateCandidates.Add(null);
    }

    public void Skip(int count = 1)
    {
        Position += count;
    }

    public void BeginKeyScope()
    {
        if (collectMode is not CollectMode.KeyOnly)
        {
            throw new InvalidOperationException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.BeginKeyScopeOnlyInKeyOnlyMode,
                this));
        }

        if (isKeyScope)
        {
            throw new InvalidOperationException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.NestedKeyScopesNotAllowed,
                this));
        }

        isKeyScope = true;
        keyScopeComponents.Clear();
    }

    public void EndKeyScope()
    {
        if (collectMode is not CollectMode.KeyOnly)
        {
            throw new InvalidOperationException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.EndKeyScopeOnlyInKeyOnlyMode,
                this));
        }

        if (!isKeyScope)
        {
            throw new InvalidOperationException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.EndKeyScopeWithoutBeginKeyScope,
                this));
        }

        if (keyScopeComponents.Count == 0)
        {
            throw new InvalidOperationException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.NoComponentsInKeyScope,
                this));
        }

        object? keyObject;

        if (keyScopeComponents.Count == 1)
        {
            keyObject = keyScopeComponents[0];
        }
        else
        {
            keyObject = new RecordKey(keyScopeComponents.ToArray());
        }

        duplicateCandidates.Add(keyObject);
        keyScopeComponents.Clear();
        isKeyScope = false;
    }

    public override string ToString()
    {
        return $"CompatibilityContext[{string.Join(", ", Cells)}]";
    }

    private sealed class RecordKey(object?[] values)
    {
        private readonly object?[] values = values;

        public override bool Equals(object? obj)
        {
            if (obj is not RecordKey other)
            {
                return false;
            }

            if (values.Length != other.values.Length)
            {
                return false;
            }

            for (var i = 0; i < values.Length; i++)
            {
                if (!Equals(values[i], other.values[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public override int GetHashCode()
        {
            var hash = default(HashCode);

            for (var i = 0; i < values.Length; i++)
            {
                hash.Add(values[i]);
            }

            return hash.ToHashCode();
        }

        public override string ToString()
        {
            return $"({string.Join(", ", values.Select(v => v?.ToString() ?? "null"))})";
        }
    }
}
