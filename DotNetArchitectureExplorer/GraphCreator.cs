using Mono.Cecil;
using Mono.Cecil.Cil;

namespace DotNetArchitectureExplorer;

class GraphCreator
{
    #region Public Methods
    public string CreateGraph(TypeDefinition definition)
    {
        var nodeCache = new Dictionary<string, Node>();

        var dgml = new BinaryDecisionTree();

        foreach (var method in definition.Methods)
        {
            nodeCache[method.FullName] = new Node(method,definition);
        }

        Node FromNodeCache(MethodReference mr)
        {
            if (nodeCache.TryGetValue(mr.FullName, out var cache))
            {
                return cache;
            }

            nodeCache[mr.FullName] = new Node(mr, definition);
            return nodeCache[mr.FullName];
        }

        Node FromNodeCacheField(FieldReference fr)
        {
            if (nodeCache.TryGetValue(fr.FullName, out var field))
            {
                return field;
            }

            nodeCache[fr.FullName] = new Node(fr, definition);
            return nodeCache[fr.FullName];
        }

        foreach (var method in definition.Methods.Where(m => m.HasBody))
        {
            foreach (var instruction in method.Body.Instructions)
            {
                var mr = instruction.Operand as MethodReference;
                if (mr != null)
                {
                    var md = instruction.Operand as MethodDefinition;
                    if (mr.IsGenericInstance)
                    {
                        mr = ((GenericInstanceMethod) mr).ElementMethod;
                    }

                    if (mr.DeclaringType == definition || IsInheritedFrom(definition, mr.DeclaringType))
                    {
                        var source = FromNodeCache(method);
                        var target = FromNodeCache(mr);

                        if (md != null && md.IsGetter)
                        {
                            dgml.Add(new Vertex(source, target, VertexType.ReadProperty));
                            continue;
                        }

                        dgml.Add(new Vertex(source, target));
                    }
                }

                var fr = instruction.Operand as FieldDefinition;
                if (fr != null)
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
                            dgml.Add(new Vertex(source, target, VertexType.ReadProperty));
                            continue;
                        }

                        dgml.Add(new Vertex(source, target));
                    }
                }


            }
        }

        return dgml.ToDgml().ToString();
    }
    #endregion

    #region Methods
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
    #endregion
}