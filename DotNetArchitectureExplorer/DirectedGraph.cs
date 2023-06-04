using Mono.Cecil;

namespace DotNetArchitectureExplorer;

public sealed class DirectedGraph
{
    readonly List<Link> links = new();

    public IReadOnlyList<Link> Links => links;

    public void Add(Link link)
    {
        links.Add(link);
    }

    readonly Dictionary<string, Node> nodeCache = new();


    public Node GetMethodNode(MethodReference methodReference)
    {
        if (nodeCache.TryGetValue(methodReference.FullName, out var cache))
        {
            return cache;
        }

        return nodeCache[methodReference.FullName] = CreateMethodNode(methodReference);
    }

    public Node GetFieldNode(FieldReference fieldReference)
    {
        if (nodeCache.TryGetValue(fieldReference.FullName, out var cache))
        {
            return cache;
        }

        return nodeCache[fieldReference.FullName] = CreateFieldNode(fieldReference);
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
        nodeCache[node.Id] = node;
    }

}