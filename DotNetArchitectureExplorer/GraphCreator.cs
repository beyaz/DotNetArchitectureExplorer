using System;
using System.Collections.Generic;
using System.Linq;
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

        Func<MethodReference, Node> fromNodeCache = mr =>
        {
            if (nodeCache.ContainsKey(mr.FullName))
            {
                return nodeCache[mr.FullName];
            }

            nodeCache[mr.FullName] = new Node(mr, definition)
            {
                    
            };
            return nodeCache[mr.FullName];
        };


        Func<FieldReference, Node> fromNodeCacheField = fr =>
        {
            if (nodeCache.ContainsKey(fr.FullName))
            {
                return nodeCache[fr.FullName];
            }

            nodeCache[fr.FullName] = new Node(fr,definition);
            return nodeCache[fr.FullName];
        };

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
                        var source = fromNodeCache(method);
                        var target = fromNodeCache(mr);

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
                        var source = fromNodeCache(method);
                        var target = fromNodeCacheField(fr);

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