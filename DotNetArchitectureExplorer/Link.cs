    using System.Diagnostics;

namespace DotNetArchitectureExplorer;

[DebuggerDisplay("{Source.Label} -> {Target.Label}")]
public sealed record Link
{
    public Node Source { get; init; }

    public Node Target { get; init; }

    public string StrokeDashArray { get; init; }

    public string Category { get; init; }
}

