using System.Diagnostics;
using System.Xml.Linq;
using Mono.Cecil;

namespace DotNetArchitectureExplorer;

[DebuggerDisplay("{" + nameof(Value) + "}")]
public sealed class Node
{
    static readonly string ns = "http://schemas.microsoft.com/vs/2009/dgml";

    readonly TypeDefinition _typeDefinition;

    

    public Node(FieldReference fieldReference, TypeDefinition typeDefinition)
    {
        _typeDefinition = typeDefinition;
        FieldReference  = fieldReference;
        Id              = fieldReference.FullName;
        Value           = fieldReference.Name;
    }

    public Node(MethodReference methodDefinition, TypeDefinition typeDefinition)
    {
        _typeDefinition = typeDefinition;
        MethodReference = methodDefinition;

        Id = methodDefinition.FullName;
        if (IsProperty)
        {
            Value = methodDefinition.Name.RemoveFromStart("set_").RemoveFromStart("get_");
        }
        else
        {
            Value = methodDefinition.Name;
        }

        if (IsBaseMethod)
        {
            Value = "base." + Value;
        }
    }

    bool IsBaseMethod
    {
        get
        {
            if (MethodDefinition != null)
            {
                return _typeDefinition != MethodDefinition.DeclaringType;
            }

            return _typeDefinition == FieldReference?.DeclaringType;
        }
    }

    public string Id { get; }

    public bool IsProperty
    {
        get
        {
            if (MethodDefinition == null)
            {
                return false;
            }

            return MethodDefinition.IsSetter || MethodDefinition.IsGetter;
        }
    }

    public bool IsField => FieldReference != null;

    public MethodDefinition MethodDefinition => MethodReference?.Resolve();

    public MethodReference MethodReference { get; }
    public FieldReference FieldReference { get; }
    public string Value { get; }

    public override bool Equals(object obj)
    {
        var node = obj as Node;
        return node != null && node.GetHashCode() == GetHashCode();
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public XElement ToDgml()
    {
        var element = new XElement(XName.Get("Node", ns), new XAttribute("Label", Value), new XAttribute("Id", Id));

        if (IsProperty)
        {
            element.Add(new XAttribute("StrokeDashArray", "5,5"));
            element.Add(new XAttribute("Background", "#f2f4f7"));
        }

        if (IsField)
        {
            element.Add(new XAttribute("StrokeDashArray", "5,5"));
            element.Add(new XAttribute("Background", "#90B9F5"));
        }

        return element;
    }
}

[DebuggerDisplay("{Source.Value} -{VertexType}-> {Target.Value}")]
public class Vertex
{
    static readonly string ns = "http://schemas.microsoft.com/vs/2009/dgml";

    public Vertex(Node source, Node target, VertexType vertexType = VertexType.None)
    {
        Source     = source;
        Target     = target;
        VertexType = vertexType;
    }

    public Node Source { get; set; }

    public Node Target { get; set; }

    public VertexType VertexType { get; set; }

    public XElement ToDgml()
    {
        var element = new XElement(XName.Get("Link", ns), new XAttribute("Source", Source.Id), new XAttribute("Target", Target.Id));

        if (VertexType == VertexType.ReadProperty)
        {
            element.Add(new XAttribute("StrokeDashArray", "5,5"));
        }

        return element;
    }
}

public enum VertexType
{
    None,
    ReadProperty,
    True,
    False
}

static class Extensions2
{
    public static IEnumerable<Node> ConnectedNodes(this BinaryDecisionTree bdt)
    {
        return bdt.Vertices.SelectMany(v => new[]
            {
                v.Source,
                v.Target
            })
            .Distinct()
            .ToList();
    }
}

public class BinaryDecisionTree
{
    
    

    internal BinaryDecisionTree()
    {
        Nodes    = new List<Node>();
        Vertices = new List<Vertex>();
    }


    public List<Node> Nodes { get; }

    public List<Vertex> Vertices { get; }

    public void Add(params Node[] node)
    {
        Nodes.AddRange(node);
    }

    public void Add(params Vertex[] vertex)
    {
        Vertices.AddRange(vertex);
    }

    public void Remove(Node node)
    {
        Nodes.Remove(node);
    }

    public void Remove(Vertex vertex)
    {
        Vertices.Remove(vertex);
    }
}

public static class DgmlHelper
{
    static readonly string ns = "http://schemas.microsoft.com/vs/2009/dgml";

    public static XElement ToDgml(this BinaryDecisionTree bdt)
    {
        var nodes =
            from n in bdt.ConnectedNodes()
            select n.ToDgml();
        var links =
            from v in bdt.Vertices
            select v.ToDgml();
        return CreateGraph(nodes, links);
    }

    static XElement CreateGraph(IEnumerable<XElement> nodes, IEnumerable<XElement> links)
    {
        var xElement = new XElement(XName.Get("DirectedGraph", ns));
        var xElement2 = new XElement(XName.Get("Nodes", ns));
        var xElement3 = new XElement(XName.Get("Links", ns));
        xElement2.Add(nodes.Cast<object>().ToArray());
        xElement3.Add(links.Cast<object>().ToArray());
        xElement.Add(xElement2);
        xElement.Add(xElement3);
        return xElement;
    }
}