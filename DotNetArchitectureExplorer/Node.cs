using System.Diagnostics;

namespace DotNetArchitectureExplorer;

[DebuggerDisplay("{" + nameof(Label) + "}")]
public sealed class Node
{
    public string Id { get; init; }
    public string Label { get; init; }

    public string Background { get; init; }
    public string StrokeDashArray { get; init; }

    public string Icon { get; init; }

    public string Group { get; init; }

    public string Description { get; init; }

    public override bool Equals(object obj)
    {
        return obj is Node node && node.GetHashCode() == GetHashCode();
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}