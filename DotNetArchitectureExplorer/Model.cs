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

