global using static DotNetArchitectureExplorer.Icon;

namespace DotNetArchitectureExplorer;

class Icon
{
    public static string IconClass => Image("class.png");
    public static string IconField => Image("field.png");
    public static string IconInterface => Image("interface.png");
    public static string IconMethod => Image("method.png");
    public static string IconNamespace => Image("namespace.png");
    
    static string Image(string fileName)
    {
        var workingDirectory = Directory.GetParent(typeof(Program).Assembly.Location)?.FullName;

        return Path.Combine(workingDirectory ?? string.Empty, "img", fileName);
    }
}