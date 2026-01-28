using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SchemaInfoScanner.Extensions;

namespace SchemaInfoScanner.NameObjects;

public class RecordName : IEquatable<RecordName>
{
    public string Namespace { get; }
    public string Name { get; }

    public string FullName =>
        string.IsNullOrEmpty(Namespace)
            ? Name
            : $"{Namespace}.{Name}";

    public RecordName(RecordDeclarationSyntax recordDeclarationSyntax)
    {
        Namespace = recordDeclarationSyntax.GetNamespace();
        Name = recordDeclarationSyntax.Identifier.ValueText;
    }

    public RecordName(string fullName)
    {
        if (string.IsNullOrEmpty(fullName))
        {
            throw new ArgumentException("fullName should not be null, empty, or end with '.'");
        }

        if (fullName[^1] == '.')
        {
            throw new ArgumentException("fullName should not be null, empty, or end with '.'");
        }

        var parts = fullName.Split('.');
        if (parts.Length == 1)
        {
            Namespace = string.Empty;
            Name = parts[0];
        }
        else
        {
            Namespace = parts[0];
            Name = parts[^1];
        }
    }

    public RecordName(INamedTypeSymbol namedTypeSymbol)
    {
        Namespace = GetFullNamespace(namedTypeSymbol.ContainingNamespace);
        Name = namedTypeSymbol.Name;
    }

    private static string GetFullNamespace(INamespaceSymbol namespaceSymbol)
    {
        var parts = new List<string>();
        var current = namespaceSymbol;
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
