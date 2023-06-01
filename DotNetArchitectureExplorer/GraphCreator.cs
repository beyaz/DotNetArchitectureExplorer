using Mono.Cecil;
using Mono.Cecil.Cil;

namespace DotNetArchitectureExplorer;

class GraphCreator
{
    public string CreateGraph(TypeDefinition definition)
    {
        var nodeCache = new Dictionary<string, Node>();

        var dgml = new DirectedGraph();

        foreach (var method in definition.Methods)
        {
            nodeCache[method.FullName] = CreateMethodNode(method, definition);
        }

        Node FromNodeCache(MethodReference mr)
        {
            if (nodeCache.TryGetValue(mr.FullName, out var cache))
            {
                return cache;
            }

            nodeCache[mr.FullName] = CreateMethodNode(mr, definition);
            
            return nodeCache[mr.FullName];
        }

        Node FromNodeCacheField(FieldReference fr)
        {
            if (nodeCache.TryGetValue(fr.FullName, out var field))
            {
                return field;
            }

            nodeCache[fr.FullName] = CreateFieldNode(fr, definition);
            return nodeCache[fr.FullName];
        }

        foreach (var method in definition.Methods.Where(m => m.HasBody))
        {
            foreach (var instruction in method.Body.Instructions)
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
                    
                    if (mr.DeclaringType == definition || IsInheritedFrom(definition, mr.DeclaringType))
                    {
                        var source = FromNodeCache(method);
                        var target = FromNodeCache(mr);

                        if (md is { IsGetter: true })
                        {
                            dgml.Add(new Link { Source = source, Target = target, LinkType = LinkType.ReadProperty });
                            continue;
                        }

                        dgml.Add(new Link { Source = source, Target = target, LinkType = LinkType.None });
                    }
                }

                if (instruction.Operand is FieldDefinition fr)
                {
                    if (fr.Name?.EndsWith(">k__BackingField") == true)
                    {
                        continue;
                    }

                    if (fr.DeclaringType == definition || IsInheritedFrom(definition, fr.DeclaringType))
                    {
                        var source = FromNodeCache(method);
                        var target = FromNodeCacheField(fr);

                        if (instruction.OpCode.Code == Code.Ldfld)
                        {
                            dgml.Add(new Link { Source = source, Target = target, LinkType = LinkType.ReadProperty });
                            continue;
                        }

                        dgml.Add(new Link { Source = source, Target = target, LinkType = LinkType.None });
                    }
                }
            }
        }

        return dgml.ToDirectedGraphElement().ToString();
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
}