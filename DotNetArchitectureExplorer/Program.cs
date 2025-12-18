namespace DotNetArchitectureExplorer;

static partial class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        DbDiagramExporter.BuildDatabaseDgml();
        
        if (args == null || args.Length == 0)
        {
            args = new[] { typeof(Program).Assembly.Location };
        }

        foreach (var assemblyFilePath in args)
        {
            ExportMethodCallGraphOfAssembly(assemblyFilePath);
        }
    }

    static void ExportMethodCallGraphOfAssembly(string assemblyFilePath)
    {
        var (exception, dgmlContent) = CreateMethodCallGraphOfAssembly(assemblyFilePath);
        if (exception is not null)
        {
            Console.WriteLine(exception);

            Console.Read();

            return;
        }

        var dgmlFilePath = Path.ChangeExtension(assemblyFilePath, "dgml");

        File.WriteAllText(dgmlFilePath, dgmlContent);
    }
}