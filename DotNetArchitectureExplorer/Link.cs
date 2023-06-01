    using System.Diagnostics;

namespace DotNetArchitectureExplorer;

[DebuggerDisplay("{Source.Label} -{LinkType}-> {Target.Label}")]
public sealed record Link
{
    public Node Source { get; init; }

    public Node Target { get; init; }

    public LinkType LinkType { get; init; }
}

public enum LinkType
{
    None,
    ReadProperty,
    True,
    False
}