namespace DotNetArchitectureExplorer;

static class Handler
{
    public static (string exception, string dgmlContent) CreateMethodCallGraph(string assemblyFilePath, string fullTypeName)
    {
        var (exception, assemblyDefinition) = ReadAssemblyDefinition(assemblyFilePath);
        if (exception is not null)
        {
            return (exception.ToString(), default);
        }

        foreach (var moduleDefinition in assemblyDefinition.Modules)
        {
            foreach (var typeDefinition in moduleDefinition.Types)
            {
                var dgml = new GraphCreator().CreateGraph(typeDefinition);
            }
        }

        return (default, "f");
    }
}