
var assemblyFilePath = @"C:\github\DotNetArchitectureExplorer\DotNetArchitectureExplorer\bin\Debug\net6.0\DotNetArchitectureExplorer.dll";

var fullTypeName = "DotNetArchitectureExplorer.Extensions";

var (exception, dgmlContent) = CreateMethodCallGraph(assemblyFilePath, fullTypeName);
if (exception is null)
{
    File.WriteAllText(@"C:\github\DotNetArchitectureExplorer\DotNetArchitectureExplorer\Sample.dgml", dgmlContent);
}