using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SchemaInfoScanner.NameObjects;

public class PropertyName : IEquatable<PropertyName>
{
    public RecordName RecordName { get; }
    public string Name { get; }
    public bool IsNullableTypeSyntax { get; }
    public string FullName => $"{RecordName.FullName}.{Name}";

    public PropertyName(RecordName recordName, ParameterSyntax parameterSyntax)
        : this(recordName, parameterSyntax.Identifier.ValueText, parameterSyntax.Type is NullableTypeSyntax)
    {
    }

    public PropertyName(RecordName recordName, string name, bool isNullableTypeSyntax)
    {
        RecordName = recordName;
        Name = name;
        IsNullableTypeSyntax = isNullableTypeSyntax;
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

        return Equals((PropertyName)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(this.Name, this.RecordName);
    }

    public bool Equals(PropertyName? other)
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
               this.RecordName.Equals(other.RecordName);
    }

    public override string ToString()
    {
        return FullName;
    }
}
