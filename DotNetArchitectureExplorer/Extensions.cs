using Mono.Cecil;

namespace DotNetArchitectureExplorer;

static class Extensions
{
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