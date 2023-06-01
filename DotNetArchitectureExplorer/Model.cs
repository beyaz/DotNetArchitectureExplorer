using System.Xml.Linq;

namespace DotNetArchitectureExplorer;

static class Extensions2
{
   
}

public class BinaryDecisionTree
{
    
    

    internal BinaryDecisionTree()
    {
    
        Links = new List<Link>();
    }


  

    public List<Link> Links { get; }

    

    public void Add(Link vertex)
    {
        Links.Add(vertex);
    }

    
}

public static class DgmlHelper
{

    public static XElement ToDgml(this BinaryDecisionTree bdt)
    {
        IReadOnlyList<Link> links = bdt.Links;
        
        var nodeElements =
            from n in ConnectedNodes(links)
            select n.ToDgml();
        
        var linkElements =
            from v in links
            select v.ToDgml();
        
        return CreateGraph(nodeElements, linkElements);

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

   
}