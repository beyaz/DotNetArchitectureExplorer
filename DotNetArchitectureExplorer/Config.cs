using System.Text.Json;

namespace DotNetArchitectureExplorer;

sealed class Config
{
    public string[] ExportOnlyNamespaceNameContains { get; set; }
}

static class ConfigReader
{
    public static (bool success, Config config, Exception exception) TryReadConfig(string filePath)
    {
        if (File.Exists(filePath))
        {
            var config = JsonSerializer.Deserialize<Config>(File.ReadAllText(filePath), new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            return (true, config, default);
        }

        return (default, default, new FileNotFoundException(filePath));
    }
    
    public static Config TryReadConfig()
    {
        var directoryName = Path.GetDirectoryName(typeof(ConfigReader).Assembly.Location);
        if (directoryName is null)
        {
            return null;
        }
        
        return TryReadConfig(Path.Combine(directoryName, "Config.json")).config;
    }
}