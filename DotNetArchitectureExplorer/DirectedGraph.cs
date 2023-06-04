namespace DotNetArchitectureExplorer;

public sealed class DirectedGraph
{
    readonly List<Link> links = new();

    public IReadOnlyList<Link> Links => links;

    public void Add(Link link)
    {
        links.Add(link);
    }
}