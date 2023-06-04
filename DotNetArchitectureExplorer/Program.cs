
//var assemblyFilePath = @"C:\github\DotNetArchitectureExplorer\DotNetArchitectureExplorer\bin\Debug\net6.0\DotNetArchitectureExplorer.dll";
var assemblyFilePath = @"C:\github\ReactWithDotNet\ReactWithDotNet\bin\Debug\net6.0\ReactWithDotNet.dll";

var (exception, dgmlContent) = CreateMethodCallGraphOfAssembly(assemblyFilePath);
if (exception is null)
{
    File.WriteAllText($@"C:\github\DotNetArchitectureExplorer\DotNetArchitectureExplorer\{Path.GetFileNameWithoutExtension(assemblyFilePath)}.dgml", dgmlContent);
}

//var fullTypeName = "DotNetArchitectureExplorer.Extensions";

//var assemblyFilePath = @"C:\github\ReactWithDotNet\ReactWithDotNet\bin\Debug\net6.0\ReactWithDotNet.dll";

// var fullTypeName = "ReactWithDotNet.ElementSerializer";

//var fullTypeName = "ReactWithDotNet.ElementSerializer";

//var (exception, dgmlContent) = CreateMethodCallGraphOfType(assemblyFilePath, fullTypeName);
//if (exception is null)
//{
//    File.WriteAllText($@"C:\github\DotNetArchitectureExplorer\DotNetArchitectureExplorer\{fullTypeName}.dgml", dgmlContent);
//}