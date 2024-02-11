using System.Collections.Immutable;
using System.Text;
using System.Xml.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace DotNetArchitectureExplorer;

static partial class Program
{
    const string ns = "http://schemas.microsoft.com/vs/2009/dgml";
    static string IconClass => Image("class.png");
    static string IconField => Image("field.png");
    static string IconInterface => Image("interface.png");
    static string IconMethod => Image("method.png");
    static string IconNamespace => Image("namespace.png");

    public static (string exception, string dgmlContent) CreateMethodCallGraphOfAssembly(string assemblyFilePath)
    {
        var (exception, assemblyDefinition) = ReadAssemblyDefinition(assemblyFilePath);
        if (exception is not null)
        {
            return (exception.ToString(), default);
        }

        var dgml = new DirectedGraph();

        var typeDefinitions = assemblyDefinition.GetTypesForAnalyze().ToImmutableList();

        foreach (var typeDefinition in typeDefinitions)
        {
            AddType(dgml, typeDefinition, t => typeDefinitions.Contains(t));
        }

        return (default, dgml.ToDirectedGraphElement().ToString());
    }

    static void AddType(DirectedGraph dgml, TypeDefinition currentTypeDefinition, Func<TypeReference, bool> isInAnalyse)
    {
        var currentClassNode = CreateTypeNode(currentTypeDefinition);

        // arrange namespace
        {
            var namespaceName = currentTypeDefinition.Namespace;

            var nameListInNamesapceName = namespaceName.Split('.').ToList();

            Node parentNamespaceNode = null, currentNamespaceNode = null;

            string namespaceId = null;

            foreach (var name in nameListInNamesapceName)
            {
                if (namespaceId == null)
                {
                    namespaceId = name;
                }
                else
                {
                    namespaceId += "." + name;
                }

                currentNamespaceNode = CreateNamespaceNode(namespaceId, name);

                if (parentNamespaceNode == null)
                {
                    parentNamespaceNode = currentNamespaceNode;
                    continue;
                }

                dgml.Add(new Link
                {
                    Source   = parentNamespaceNode,
                    Target   = currentNamespaceNode,
                    Category = "Contains"
                });

                parentNamespaceNode = currentNamespaceNode;
            }

            dgml.Add(new Link
            {
                Source   = currentNamespaceNode,
                Target   = currentClassNode,
                Category = "Contains"
            });
        }

        foreach (var propertyDefinition in currentTypeDefinition.Properties)
        {
            var node = CreatePropertyNode(propertyDefinition);

            dgml.Add(new Link { Source = currentClassNode, Target = node, Category = "Contains" });
        }

        foreach (var methodDefinition in currentTypeDefinition.Methods)
        {
            if (methodDefinition.IsGetter || methodDefinition.IsSetter)
            {
                continue;
            }

            var methodDefinitionNode = CreateMethodNode(methodDefinition);

            dgml.Add(new Link { Source = currentClassNode, Target = methodDefinitionNode, Category = "Contains" });
        }

        foreach (var fieldDefinition in currentTypeDefinition.Fields.Where(x => !x.IsBackingField()))
        {
            var fieldDefinitionNode = CreateFieldNode(fieldDefinition);

            dgml.Add(new Link { Source = currentClassNode, Target = fieldDefinitionNode, Category = "Contains" });
        }

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

                    if (mr.DeclaringType.Scope == currentTypeDefinition.Scope)
                    {
                        if (mr.DeclaringType.Resolve()?.IsNestedPrivate == true)
                        {
                            continue;
                        }

                        if (isInAnalyse(mr.DeclaringType) == false)
                        {
                            continue;
                        }

                        var currentMethodDefinitionNode = CreateMethodNode(currentMethodDefinition);
                        var targetMethodNode = CreateMethodNode(mr);

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

                else if (instruction.Operand is FieldDefinition fr)
                {
                    if (fr.IsBackingField())
                    {
                        continue;
                    }

                    if (fr.DeclaringType.Scope == currentTypeDefinition.Scope)
                    {
                        if (fr.DeclaringType.Resolve()?.IsNestedPrivate == true)
                        {
                            continue;
                        }

                        if (isInAnalyse(fr.DeclaringType) == false)
                        {
                            continue;
                        }

                        var currentMethodDefinitionNode = CreateMethodNode(currentMethodDefinition);
                        var targetFieldNode = CreateFieldNode(fr);

                        if (instruction.OpCode.Code == Code.Ldfld || instruction.OpCode.Code == Code.Ldsfld)
                        {
                            dgml.Add(new Link { Source = currentMethodDefinitionNode, Target = targetFieldNode, StrokeDashArray = "5,5", Description = "read" });
                            continue;
                        }

                        dgml.Add(new Link { Source = currentMethodDefinitionNode, Target = targetFieldNode });
                    }
                }

                else if (instruction.Operand is TypeDefinition td && td.FullName != currentTypeDefinition.FullName && CanExport(td))
                {
                    dgml.Add(new Link { Source = currentClassNode, Target = CreateTypeNode(td) });
                }

                else if (instruction.Operand is TypeReference tr && tr.Scope == currentTypeDefinition.Scope)
                {
                    var td2 = tr.Resolve();
                    if (td2 is not null && td2.FullName != currentTypeDefinition.FullName && CanExport(td2))
                    {
                        dgml.Add(new Link { Source = currentClassNode, Target = CreateTypeNode(td2) });
                    }
                }
            }
        }
    }

    static bool CanExport(TypeDefinition type)
    {
        if (type.Name == "<Module>" || type.Name == "<PrivateImplementationDetails>")
        {
            return false;
        }

        if (type.Name?.StartsWith("<>f__AnonymousType") is true)
        {
            return false;
        }

        if (type.Namespace == "Microsoft.CodeAnalysis" ||
            type.Namespace == "System.Runtime.CompilerServices" ||
            string.IsNullOrWhiteSpace(type.Namespace))
        {
            return false;
        }

        var hasExportOnlyNamespaceNameContainsRule = false;
        var hasMatchWithExportOnlyNamespaceContains = false;
        
        foreach (var name in Config?.ExportOnlyNamespaceNameContains ?? Enumerable.Empty<string>())
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                hasExportOnlyNamespaceNameContainsRule = true;
                if (type.Namespace?.Contains(name,StringComparison.OrdinalIgnoreCase) == true)
                {
                    hasMatchWithExportOnlyNamespaceContains = true;
                    break;
                }
            }
        }

        if (hasExportOnlyNamespaceNameContainsRule)
        {
            if (hasMatchWithExportOnlyNamespaceContains)
            {
                return true;
            }

            return false;
        }
        
        return true;
    }
    
    static readonly Config Config = ConfigReader.TryReadConfig();

    static Node CreateFieldNode(FieldReference fieldReference)
    {
        return new Node
        {
            Id              = fieldReference.FullName,
            Label           = fieldReference.Name,
            StrokeDashArray = "5,5",
            Background      = "#e5e9ee",
            Icon            = IconField,
            Description     = fieldReference.FullName
        };
    }

    static Node CreateMethodNode(MethodReference methodReference)
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

        if (methodDefinition != null && (methodDefinition.IsGetter || methodDefinition.IsSetter))
        {
            var propertyDefinition = methodDefinition.DeclaringType.Properties.FirstOrDefault(p => p.GetMethod == methodDefinition || p.SetMethod == methodDefinition);
            if (propertyDefinition != null)
            {
                return CreatePropertyNode(propertyDefinition);
            }
        }

        if (methodReference.DeclaringType.ContainsGenericParameter)
        {
            var elementType = methodReference.DeclaringType.GetElementType();

            id = id.Replace(methodReference.DeclaringType.FullName, elementType.FullName);
        }

        return new Node
        {
            Id          = id,
            Label       = label,
            Icon        = IconMethod,
            Description = methodReference.FullName
        };
    }

    static Node CreateNamespaceNode(string id, string label)
    {
        return new Node
        {
            Id    = id,
            Label = label,
            Icon  = IconNamespace,
            Group = "Collapsed"
        };
    }

    static Node CreatePropertyNode(PropertyReference propertyReference)
    {
        return new Node
        {
            Id          = propertyReference.FullName,
            Label       = propertyReference.Name,
            Background  = "#e5e9ee",
            Icon        = IconField,
            Description = propertyReference.FullName
        };
    }

    static Node CreateTypeNode(TypeDefinition typeDefinition)
    {
        return new Node
        {
            Id    = typeDefinition.FullName,
            Label = typeDefinition.Name,
            Icon  = typeDefinition.IsInterface ? IconInterface : IconClass,
            Group = "Collapsed"
        };
    }

    static string GetMethodNameWithParameterSignature(this MethodReference methodReference)
    {
        var builder = new StringBuilder();

        builder.Append(methodReference.Name);

        builder.Append("(");

        if (methodReference.HasParameters)
        {
            var parameters = methodReference.Parameters;
            for (var i = 0; i < parameters.Count; i++)
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

    static IEnumerable<TypeDefinition> GetTypesForAnalyze(this AssemblyDefinition assemblyDefinition)
    {
        foreach (var moduleDefinition in assemblyDefinition.Modules)
        {
            foreach (var type in moduleDefinition.Types)
            {
                if (!CanExport(type))
                {
                    continue;
                }

                yield return type;
            }
        }
    }

    static string Image(string fileName)
    {
        var workingDirectory = Directory.GetParent(typeof(Program).Assembly.Location)?.FullName;

        return Path.Combine(workingDirectory ?? string.Empty, "img", fileName);
    }

    static bool IsBackingField(this FieldReference fieldReference)
    {
        return fieldReference.Name.EndsWith(">k__BackingField");
    }

    static (BadImageFormatException exception, AssemblyDefinition assemblyDefinition) ReadAssemblyDefinition(string filePath)
    {
        return Try<BadImageFormatException, AssemblyDefinition>(() =>
        {
            var resolver = new DefaultAssemblyResolver();

            resolver.AddSearchDirectory(Path.GetDirectoryName(filePath));

            return AssemblyDefinition.ReadAssembly(filePath, new ReaderParameters { AssemblyResolver = resolver });
        });
    }

    static XElement ToDgml(this Link link)
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

        if (link.Description is not null)
        {
            element.Add(new XAttribute(nameof(link.Description), link.Description));
        }

        return element;
    }

    static XElement ToDgml(this Node node)
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

        if (node.Description is not null)
        {
            element.Add(new XAttribute(nameof(node.Description), node.Description));
        }

        if (node.FontSize > 0)
        {
            element.Add(new XAttribute(nameof(node.FontSize), node.FontSize.ToString("F")));
        }

        if (node.NodeRadius > 0)
        {
            element.Add(new XAttribute(nameof(node.NodeRadius), node.NodeRadius.ToString()));
        }

        return element;
    }

    static XElement ToDirectedGraphElement(this DirectedGraph directedGraph)
    {
        var links = directedGraph.Links;

        var nodes = ConnectedNodes(links).ToList();

        return createDirectedGraphElement(nodes.Select(arrangeNode).Select(ToDgml), links.Select(ToDgml));

        Node arrangeNode(Node node)
        {
            var outgoingLinkCount = links.Count(l => l.Source.Id == node.Id);

            var fontSize = calculateFontSize();

            return new Node
            {
                Background      = node.Background,
                Description     = node.Description,
                FontSize        = fontSize,
                Group           = node.Group,
                Icon            = node.Icon,
                Id              = node.Id,
                Label           = node.Label,
                NodeRadius      = node.NodeRadius,
                StrokeDashArray = node.StrokeDashArray
            };

            double calculateFontSize()
            {
                if (node.Group is null)
                {
                    if (outgoingLinkCount > 30)
                    {
                        return 25;
                    }

                    if (outgoingLinkCount > 3)
                    {
                        return 13 + outgoingLinkCount / 3.0;
                    }
                }

                return 0;
            }
        }

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
            }).Distinct();
        }
    }

    static (TException exception, T value) Try<TException, T>(Func<T> func) where TException : Exception
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
}