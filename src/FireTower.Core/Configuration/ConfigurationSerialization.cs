using System.Text.Json;
using System.Text.Json.Serialization;

namespace FireTower.Core.Configuration;

/// <summary>
/// The single set of JSON options used for every configuration file, so formatting and
/// enum representation stay consistent across firetower.json, providers.json, and the rest.
/// </summary>
public static class ConfigurationSerialization
{
    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };
}
