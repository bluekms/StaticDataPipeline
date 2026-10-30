using Microsoft.CodeAnalysis;
using SchemaInfoScanner.Resources;

namespace SchemaInfoScanner.NameObjects;

public class RecordName : IEquatable<RecordName>
{
    public string Namespace { get; }
    public string Name { get; }

    public string FullName =>
        string.IsNullOrEmpty(Namespace)
            ? Name
            : $"{Namespace}.{Name}";

    public RecordName(string fullName)
    {
        if (string.IsNullOrEmpty(fullName))
        {
            throw new ArgumentException(Messages.InvalidFullName);
        }

        if (fullName[^1] == '.')
        {
            throw new ArgumentException(Messages.InvalidFullName);
        }

        var parts = fullName.Split('.');
        if (parts.Length == 1)
        {
            Namespace = string.Empty;
            Name = parts[0];
        }
        else
        {
            Namespace = string.Join(".", parts[..^1]);
            Name = parts[^1];
        }
    }

    public RecordName(INamedTypeSymbol namedTypeSymbol)
    {
        Namespace = GetContainingPath(namedTypeSymbol);
        Name = namedTypeSymbol.Name;
    }

    private static string GetContainingPath(INamedTypeSymbol namedTypeSymbol)
    {
        var parts = new List<string>();
        for (var type = namedTypeSymbol.ContainingType; type is not null; type = type.ContainingType)
        {
            parts.Add(type.Name);
        }

        var current = namedTypeSymbol.ContainingNamespace;
        while (current is not null && !current.IsGlobalNamespace)
        {
            parts.Add(current.Name);
            current = current.ContainingNamespace;
        }

        parts.Reverse();

        return string.Join(".", parts);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != this.GetType())
        {
            return false;
        }

        return Equals((RecordName)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(this.Name, this.FullName);
    }

    public bool Equals(RecordName? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return this.Name == other.Name &&
               this.FullName == other.FullName;
    }

    public override string ToString()
    {
        return FullName;
    }
}
