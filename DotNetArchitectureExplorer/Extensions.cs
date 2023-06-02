using System.Text;
using System.Xml.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace DotNetArchitectureExplorer;

static class Extensions
{
    public static string CreateGraph(TypeDefinition currentTypeDefinition)
    {
        var dgml = new DirectedGraph();

        var currentClassNode = CreateClassNode(currentTypeDefinition);

        dgml.Add(currentClassNode);
        
        foreach (var methodDefinition in currentTypeDefinition.Methods.Where(m => m.HasBody))
        {
            var methodDefinitionNode = CreateMethodNode(methodDefinition, currentTypeDefinition);
            
            dgml.Add(methodDefinitionNode);

            dgml.Add(new Link { Source = currentClassNode, Target = methodDefinitionNode, Category = "Contains" });
        }

        foreach (var fieldDefinition in currentTypeDefinition.Fields)
        {
            var fieldDefinitionNode = CreateFieldNode(fieldDefinition, currentTypeDefinition);

            dgml.Add(fieldDefinitionNode);

            dgml.Add(new Link { Source = currentClassNode, Target = fieldDefinitionNode, Category = "Contains" });
        }





        Node getMethodNode(MethodReference mrr) => dgml.GetMethodNode(mrr, currentTypeDefinition);
        Node getFieldNode(FieldReference fr) => dgml.GetFieldNode(fr, currentTypeDefinition);

        foreach (var currentMethodDefinition in currentTypeDefinition.Methods.Where(m => m.HasBody))
        {
            foreach (var instruction in currentMethodDefinition.Body.Instructions)
            {
                if (instruction.Operand is MethodReference mr)
                {
                    var md = instruction.Operand as MethodDefinition;
                    if (mr.IsGenericInstance)
                    {
                        mr = ((GenericInstanceMethod)mr).ElementMethod;
                    }

                    if (mr.DeclaringType.FullName == "System.Object")
                    {
                        continue;
                    }

                    if (mr.DeclaringType == currentTypeDefinition || IsInheritedFrom(currentTypeDefinition, mr.DeclaringType))
                    {
                        var currentMethodDefinitionNode = getMethodNode(currentMethodDefinition);
                        var targetMethodNode = getMethodNode(mr);

                        if (md is { IsGetter: true })
                        {
                            dgml.Add(new Link
                            {
                                Source          = currentMethodDefinitionNode, 
                                Target          = targetMethodNode,
                                StrokeDashArray = "5,5"
                            });
                            continue;
                        }

                        dgml.Add(new Link { Source = currentMethodDefinitionNode, Target = targetMethodNode });

                        
                    }
                }

                if (instruction.Operand is FieldDefinition fr)
                {
                    if (fr.Name?.EndsWith(">k__BackingField") == true)
                    {
                        continue;
                    }

                    if (fr.DeclaringType == currentTypeDefinition || IsInheritedFrom(currentTypeDefinition, fr.DeclaringType))
                    {
                        var currentMethodDefinitionNode = getMethodNode(currentMethodDefinition);
                        var targetFieldNode = getFieldNode(fr);

                        if (instruction.OpCode.Code == Code.Ldfld)
                        {
                            dgml.Add(new Link { Source = currentMethodDefinitionNode, Target = targetFieldNode, StrokeDashArray = "5,5" });
                            continue;
                        }

                        dgml.Add(new Link { Source = currentMethodDefinitionNode, Target = targetFieldNode });
                    }
                }
            }
        }

        return dgml.ToDirectedGraphElement().ToString();
    }
    
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

        return createDirectedGraphElement(nodeElements, linkElements);

        static XElement createDirectedGraphElement(IEnumerable<XElement> nodes, IEnumerable<XElement> links)
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

       

        if (link.StrokeDashArray is not null)
        {
            element.Add(new XAttribute(nameof(link.StrokeDashArray), link.StrokeDashArray));
        }
        
        if (link.Category is not null)
        {
            element.Add(new XAttribute(nameof(link.Category), link.Category));
        }

        return element;
    }

    static string IconField => Path.Combine("img", "field.png");
    static string IconMethod => Path.Combine("img", "method.png");
    static string IconClass => Path.Combine("img", "class.png");
    
    public static Node CreateFieldNode(FieldReference fieldReference, TypeDefinition typeDefinition)
    {
        return new Node
        {
            Id              = fieldReference.FullName,
            Label           = fieldReference.Name,
            StrokeDashArray = "5,5",
            Background      = "#c9cbce",
            Icon = IconField
        };
    }

    public static Node CreateClassNode(TypeDefinition typeDefinition)
    {
        return new Node
        {
            Id    = typeDefinition.FullName,
            Label = typeDefinition.Name,
            Icon  = IconClass,
            Group = "Expanded"
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

    static string GetMethodNameWithParameterSignature(this MethodReference methodReference)
    {
        StringBuilder builder = new StringBuilder();

        builder.Append(methodReference.Name);
        
        builder.Append("(");

        if (methodReference.HasParameters)
        {
            var parameters = methodReference.Parameters;
            for (int i = 0; i < parameters.Count; i++)
            {
                var parameter = parameters[i];
                if (i > 0)
                    builder.Append(",");

                if (parameter.ParameterType.IsSentinel)
                    builder.Append("...,");

                builder.Append(parameter.ParameterType.Name);
            }
        }

        builder.Append(")");

        return builder.ToString();
    }

    public static Node CreateMethodNode(MethodReference methodReference, TypeDefinition callerMethodDeclaringTypeDefinition)
    {
        var id = methodReference.FullName;

        var label = methodReference.Name;

        if (methodReference.IsDefinition)
        {
            var sameNamedMethodCount = methodReference.DeclaringType.Resolve().Methods.Count(x => x.Name == methodReference.Name);
            if (sameNamedMethodCount > 1)
            {
                label = methodReference.GetMethodNameWithParameterSignature();
            }
        }
        
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
                Background      = "#f2f4f7",
                Icon = IconField
            };
        }

        return new Node
        {
            Id    = id,
            Label = label,
            Icon  = IconMethod
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

        if (node.Icon is not null)
        {
            element.Add(new XAttribute(nameof(node.Icon), node.Icon));
        }

        if (node.Group is not null)
        {
            element.Add(new XAttribute(nameof(node.Group), node.Group));
        }

        return element;
    }

    static bool IsInheritedFrom(TypeReference derived, TypeReference baseTypeReference)
    {
        if (derived == null)
        {
            return false;
        }

        if (derived == baseTypeReference)
        {
            return true;
        }

        if (baseTypeReference is GenericInstanceType)
        {
            baseTypeReference = baseTypeReference.GetElementType();
        }

        if (derived == baseTypeReference)
        {
            return true;
        }

        if (derived is GenericInstanceType)
        {
            derived = derived.GetElementType();
        }

        if (derived == baseTypeReference)
        {
            return true;
        }

        var definition = derived.Resolve();
        if (definition == null)
        {
            return false;
        }

        return IsInheritedFrom(definition.BaseType, baseTypeReference);
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

        var dgml = CreateGraph(typeDefinition);

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