namespace DotNetArchitectureExplorer;

static partial class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        if (args == null || args.Length == 0)
        {
            args = new[] { typeof(Program).Assembly.Location };
        }

        ExportMethodCallGraphOfAssembly(args[0]);
    }

    static void ExportMethodCallGraphOfAssembly(string assemblyFilePath)
    {
        var (exception, dgmlContent) = CreateMethodCallGraphOfAssembly(assemblyFilePath);
        if (exception is null)
        {
            var dgmlFilePath = Path.ChangeExtension(assemblyFilePath, "dgml");

            File.WriteAllText(dgmlFilePath, dgmlContent);
        }
    }
}