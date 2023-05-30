


using DotNetArchitectureExplorer;

var assemblyFilePath = @"C:\github\DotNetArchitectureExplorer\DotNetArchitectureExplorer\bin\Debug\net6.0\DotNetArchitectureExplorer.dll";

var fullTypeName = "DotNetArchitectureExplorer.Extensions";

Handler.CreateMethodCallGraph(assemblyFilePath, fullTypeName);