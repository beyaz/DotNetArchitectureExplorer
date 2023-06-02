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


    public Node GetMethodNode(MethodReference methodReference, TypeDefinition callerMethodDeclaringTypeDefinition)
    {
        if (nodeCache.TryGetValue(methodReference.FullName, out var cache))
        {
            return cache;
        }

        return nodeCache[methodReference.FullName] = CreateMethodNode(methodReference, callerMethodDeclaringTypeDefinition);
    }

    public Node GetFieldNode(FieldReference fieldReference, TypeDefinition callerMethodDeclaringTypeDefinition)
    {
        if (nodeCache.TryGetValue(fieldReference.FullName, out var cache))
        {
            return cache;
        }

        return nodeCache[fieldReference.FullName] = CreateFieldNode(fieldReference, callerMethodDeclaringTypeDefinition);
    }

    public void Add(Node node)
    {
        nodeCache[node.Id] = node;
    }

}