using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SchemaInfoScanner.Extensions;
using SchemaInfoScanner.Resources;

namespace SchemaInfoScanner.NameObjects;

public class EnumName : IEquatable<EnumName>
{
    public string Name { get; }
    public string FullName { get; }

    public EnumName(EnumDeclarationSyntax enumDeclaration)
    {
        Name = enumDeclaration.Identifier.ValueText;

        var namespaceName = enumDeclaration.GetNamespace();
        FullName = string.IsNullOrEmpty(namespaceName)
            ? Name
            : $"{namespaceName}.{Name}";
    }

    public EnumName(INamedTypeSymbol namedTypeSymbol)
    {
        var fullName = namedTypeSymbol.ToString();

        if (string.IsNullOrEmpty(fullName) || fullName[^1] == '.')
        {
            throw new ArgumentException(Messages.InvalidFullName);
        }

        var parts = fullName.Split('.');
        Name = parts[^1];
        FullName = fullName;
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

        return Equals((EnumName)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(this.Name, this.FullName);
    }

    public bool Equals(EnumName? other)
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
