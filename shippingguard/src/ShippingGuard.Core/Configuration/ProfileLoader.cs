using System.Text.Json;
using System.Text.Json.Serialization;
using ShippingGuard.Core.Models;

namespace ShippingGuard.Core.Configuration;

public static class ProfileLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static IReadOnlyList<AppProfile> LoadAll(string profilesDirectory)
    {
        if (!Directory.Exists(profilesDirectory))
            return Array.Empty<AppProfile>();

        var profiles = new List<AppProfile>();
        foreach (var file in Directory.GetFiles(profilesDirectory, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(file);
                var profile = JsonSerializer.Deserialize<AppProfile>(json, Options);
                if (profile is not null)
                    profiles.Add(profile);
            }
            catch { /* skip malformed profiles */ }
        }
        return profiles;
    }

    public static void Save(string profilesDirectory, AppProfile profile)
    {
        Directory.CreateDirectory(profilesDirectory);
        var path = Path.Combine(profilesDirectory, $"{profile.Id}.json");
        File.WriteAllText(path, JsonSerializer.Serialize(profile, Options));
    }

    public static void Delete(string profilesDirectory, string profileId)
    {
        var path = Path.Combine(profilesDirectory, $"{profileId}.json");
        if (File.Exists(path)) File.Delete(path);
    }
}
