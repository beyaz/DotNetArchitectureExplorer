using Mono.Cecil;

namespace DotNetArchitectureExplorer;

static class Extensions
{
    public static (BadImageFormatException exception, AssemblyDefinition assemblyDefinition) ReadAssemblyDefinition(string filePath)
    {
        return Try<BadImageFormatException, AssemblyDefinition>(() =>
        {
            var resolver = new DefaultAssemblyResolver();

            resolver.AddSearchDirectory(Path.GetDirectoryName(filePath));

            return AssemblyDefinition.ReadAssembly(filePath, new ReaderParameters { AssemblyResolver = resolver });
        });
    }

    public static (Exception exception, T value) Try<T>(Func<T> func)
    {
        try
        {
            return (default, func());
        }
        catch (Exception exception)
        {
            return (exception, default);
        }
    }

    public static (TException exception, T value) Try<TException, T>(Func<T> func) where TException : Exception
    {
        try
        {
            return (default, func());
        }
        catch (TException exception)
        {
            return (exception, default);
        }
    }

    /// <summary>
    ///     Removes value from start of str
    /// </summary>
    public static string RemoveFromStart(this string data, string value)
    {
        return RemoveFromStart(data, value, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Removes value from start of str
    /// </summary>
    public static string RemoveFromStart(this string data, string value, StringComparison comparison)
    {
        if (data == null)
        {
            return null;
        }

        if (data.StartsWith(value, comparison))
        {
            return data.Substring(value.Length, data.Length - value.Length);
        }

        return data;
    }

    public static void ForEachType(this AssemblyDefinition assemblyDefinition, Action<TypeDefinition> action)
    {
        foreach (var moduleDefinition in assemblyDefinition.Modules)
        {
            foreach (var type in moduleDefinition.Types)
            {
                action(type);
            }
        }
    }

    public static TypeDefinition FindType(this AssemblyDefinition assemblyDefinition, Func<TypeDefinition, bool> action)
    {
        foreach (var moduleDefinition in assemblyDefinition.Modules)
        {
            foreach (var type in moduleDefinition.Types)
            {
                if (action(type))
                {
                    return type;
                }
            }
        }

        return null;
    }
}