using System.Xml.Linq;
using Mono.Cecil;

namespace DotNetArchitectureExplorer;

static class Extensions
{
    public static readonly string ns = "http://schemas.microsoft.com/vs/2009/dgml";

    public static XElement ToDirectedGraphElement(this DirectedGraph directedGraph)
    {
        var links = directedGraph.Links;

        var nodeElements =
            from n in ConnectedNodes(links)
            select n.ToDgml();

        var linkElements =
            from v in links
            select v.ToDgml();

        return CreateGraph(nodeElements, linkElements);

        static XElement CreateGraph(IEnumerable<XElement> nodes, IEnumerable<XElement> links)
        {
            var root = new XElement(XName.Get("DirectedGraph", ns));
            var rootForNodes = new XElement(XName.Get("Nodes", ns));
            var rootForLinks = new XElement(XName.Get("Links", ns));

            rootForNodes.Add(nodes.Cast<object>().ToArray());
            rootForLinks.Add(links.Cast<object>().ToArray());

            root.Add(rootForNodes);
            root.Add(rootForLinks);

            return root;
        }

        static IEnumerable<Node> ConnectedNodes(IReadOnlyList<Link> links)
        {
            return links.SelectMany(v => new[]
                {
                    v.Source,
                    v.Target
                })
                .Distinct()
                .ToList();
        }
    }

    public static XElement ToDgml(this Link link)
    {
        var element = new XElement(XName.Get("Link", ns), new XAttribute("Source", link.Source.Id), new XAttribute("Target", link.Target.Id));

        if (link.LinkType == LinkType.ReadProperty)
        {
            element.Add(new XAttribute("StrokeDashArray", "5,5"));
        }

        return element;
    }

    public static Node CreateFieldNode(FieldReference fieldReference, TypeDefinition typeDefinition)
    {
        return new Node
        {
            Id              = fieldReference.FullName,
            Label           = fieldReference.Name,
            StrokeDashArray = "5,5",
            Background      = "#c9cbce"
        };
    }

    static (bool isLocalFunction, string parentMethodName, string localFunctionName) TryGetLocalFunctionName(this MethodReference methodReference)
    {
        // sample name: <ToDirectedGraphElement>g__CreateGraph|1_2
        var methodName = methodReference.Name;

        if (methodName[0] == '<')
        {
            var i = methodName.IndexOf(">g__", StringComparison.OrdinalIgnoreCase);
            if (i > 0)
            {
                var j = methodName.IndexOf('|', i);
                if (j > 0)
                {
                    var localFunctionName = methodName.Substring(i + ">g__".Length, j - (i + ">g__".Length));
                    var parentMethodName = methodName.Substring(1, i - 1);

                    return (true, parentMethodName, localFunctionName);
                }
            }
        }

        return default;
    }

    public static Node CreateMethodNode(MethodReference methodReference, TypeDefinition typeDefinition)
    {
        var id = methodReference.FullName;

        var label = methodReference.Name;

        var (isLocalFunction, parentMethodName, localFunctionName) = methodReference.TryGetLocalFunctionName();
        if (isLocalFunction)
        {
            label = $"{parentMethodName}.{localFunctionName}";
        }

        var methodDefinition = methodReference.Resolve();

        if (methodDefinition?.IsSetter == true || methodDefinition?.IsGetter == true)
        {
            label = methodDefinition.Name.RemoveFromStart("set_").RemoveFromStart("get_");

            return new Node
            {
                Id              = id,
                Label           = label,
                StrokeDashArray = "5,5",
                Background      = "#f2f4f7"
            };
        }

        return new Node
        {
            Id    = id,
            Label = label
        };
    }

    public static XElement ToDgml(this Node node)
    {
        var element = new XElement(XName.Get("Node", ns), new XAttribute("Label", node.Label), new XAttribute("Id", node.Id));

        if (node.StrokeDashArray is not null)
        {
            element.Add(new XAttribute(nameof(node.StrokeDashArray), node.StrokeDashArray));
        }

        if (node.Background is not null)
        {
            element.Add(new XAttribute(nameof(node.Background), node.Background));
        }

        return element;
    }

    public static (string exception, string dgmlContent) CreateMethodCallGraph(string assemblyFilePath, string fullTypeName)
    {
        var (exception, assemblyDefinition) = ReadAssemblyDefinition(assemblyFilePath);
        if (exception is not null)
        {
            return (exception.ToString(), default);
        }

        var (isFound, typeDefinition) = FindType(assemblyDefinition, fullTypeName);
        if (!isFound)
        {
            return ($"Type not found. type: {fullTypeName}", default);
        }

        var dgml = new GraphCreator().CreateGraph(typeDefinition);

        return (default, dgml);
    }

    public static (bool isFound, TypeDefinition typeDefinition) FindType(AssemblyDefinition assemblyDefinition, string fullTypeName)
    {
        foreach (var moduleDefinition in assemblyDefinition.Modules)
        {
            foreach (var typeDefinition in moduleDefinition.Types)
            {
                if (typeDefinition.FullName == fullTypeName)
                {
                    return (isFound: true, typeDefinition);
                }
            }
        }

        return default;
    }

    public static (BadImageFormatException exception, AssemblyDefinition assemblyDefinition) ReadAssemblyDefinition(string filePath)
    {
        return Try<BadImageFormatException, AssemblyDefinition>(() =>
        {
            var resolver = new DefaultAssemblyResolver();

            resolver.AddSearchDirectory(Path.GetDirectoryName(filePath));

            return AssemblyDefinition.ReadAssembly(filePath, new ReaderParameters { AssemblyResolver = resolver });
        });
    }

    public static (Exception exception, T value) Try<T>(Func<T> func)
    {
        try
        {
            return (default, func());
        }
        catch (Exception exception)
        {
            return (exception, default);
        }
    }

    public static (TException exception, T value) Try<TException, T>(Func<T> func) where TException : Exception
    {
        try
        {
            return (default, func());
        }
        catch (TException exception)
        {
            return (exception, default);
        }
    }

    /// <summary>
    ///     Removes value from start of str
    /// </summary>
    public static string RemoveFromStart(this string data, string value)
    {
        return RemoveFromStart(data, value, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Removes value from start of str
    /// </summary>
    public static string RemoveFromStart(this string data, string value, StringComparison comparison)
    {
        if (data == null)
        {
            return null;
        }

        if (data.StartsWith(value, comparison))
        {
            return data.Substring(value.Length, data.Length - value.Length);
        }

        return data;
    }

    public static void ForEachType(this AssemblyDefinition assemblyDefinition, Action<TypeDefinition> action)
    {
        foreach (var moduleDefinition in assemblyDefinition.Modules)
        {
            foreach (var type in moduleDefinition.Types)
            {
                action(type);
            }
        }
    }

    public static TypeDefinition FindType(this AssemblyDefinition assemblyDefinition, Func<TypeDefinition, bool> action)
    {
        foreach (var moduleDefinition in assemblyDefinition.Modules)
        {
            foreach (var type in moduleDefinition.Types)
            {
                if (action(type))
                {
                    return type;
                }
            }
        }

        return null;
    }
}