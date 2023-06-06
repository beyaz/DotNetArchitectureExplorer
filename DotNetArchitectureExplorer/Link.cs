using System.Diagnostics;

namespace DotNetArchitectureExplorer;

[DebuggerDisplay("{Source.Label} -> {Target.Label}")]
public sealed class Link
{
    public string Category { get; init; }
    public string Description { get; init; }
    public Node Source { get; init; }
    public string StrokeDashArray { get; init; }
    public Node Target { get; init; }
}