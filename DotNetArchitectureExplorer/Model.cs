﻿using System.Xml.Linq;

namespace DotNetArchitectureExplorer;

static class Extensions2
{
    public static IEnumerable<Node> ConnectedNodes(this BinaryDecisionTree bdt)
    {
        return bdt.Links.SelectMany(v => new[]
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
    
        Links = new List<Link>();
    }


  

    public List<Link> Links { get; }

    

    public void Add(params Link[] vertex)
    {
        Links.AddRange(vertex);
    }

    
}

public static class DgmlHelper
{

    public static XElement ToDgml(this BinaryDecisionTree bdt)
    {
        var nodes =
            from n in bdt.ConnectedNodes()
            select n.ToDgml();
        var links =
            from v in bdt.Links
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