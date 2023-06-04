using Mono.Cecil;

namespace DotNetArchitectureExplorer;

static class CecilExtensions
{
    public static string GetPropertyFullNameOfGetterMethod(MethodReference mr)
    {
        if (mr.DeclaringType == null)
        {
            return GetPropertyNameOfGetterMethod(mr);
        }

        return $"{mr.ReturnType.FullName} ::{GetPropertyNameOfGetterMethod(mr)}";
    }

    public static string GetPropertyNameOfGetterMethod(MethodReference mr)
    {
        return mr.Name.RemoveFromStart("get_");
    }
}