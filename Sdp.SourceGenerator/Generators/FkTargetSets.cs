using System.Globalization;

namespace Sdp.SourceGenerator.Generators;

internal sealed class FkTargetSets
{
    private readonly Dictionary<Key, string> variables = new();

    public bool Contains(FkBranch branch, string valueTypeName)
    {
        return variables.ContainsKey(CreateKey(branch, valueTypeName));
    }

    public string Add(FkBranch branch, string valueTypeName)
    {
        var variable = "__fkSet" + variables.Count.ToString(CultureInfo.InvariantCulture);
        variables[CreateKey(branch, valueTypeName)] = variable;

        return variable;
    }

    public string Find(FkBranch branch, string valueTypeName)
    {
        return variables[CreateKey(branch, valueTypeName)];
    }

    private static Key CreateKey(FkBranch branch, string valueTypeName)
    {
        return new Key(branch.TableSetMember, branch.TargetColumn, valueTypeName);
    }

    private sealed record Key(string TableSetMember, string TargetColumn, string ValueTypeName);
}
