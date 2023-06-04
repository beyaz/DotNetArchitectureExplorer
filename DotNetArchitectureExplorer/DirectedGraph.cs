namespace DotNetArchitectureExplorer;

public sealed class DirectedGraph
{
    readonly List<Link> links = new();

    readonly Dictionary<string, Node> nodeCache = new();

    public IReadOnlyList<Link> Links => links;

    public void Add(Link link)
    {
        links.Add(link);
    }

    public Node GetNode(string id, Func<Node> createNodeIfNewFunc)
    {
        if (nodeCache.TryGetValue(id, out var cache))
        {
            return cache;
        }

        return nodeCache[id] = createNodeIfNewFunc();
    }

    public void Add(Node node)
    {
        //nodeCache[node.Id] = node;
    }
}