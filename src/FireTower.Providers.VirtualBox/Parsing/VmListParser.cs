using System.Text.RegularExpressions;

namespace FireTower.Providers.VirtualBox.Parsing;

/// <summary>
/// Parses <c>VBoxManage list vms</c> output, where each line has the form
/// <c>"VM Name" {uuid}</c>.
/// </summary>
public static partial class VmListParser
{
    public static IReadOnlyList<(string Name, string Uuid)> Parse(string output)
    {
        var results = new List<(string Name, string Uuid)>();

        foreach (Match match in LinePattern().Matches(output))
        {
            results.Add((match.Groups["name"].Value, match.Groups["uuid"].Value));
        }

        return results;
    }

    [GeneratedRegex("""^"(?<name>.*)"\s+\{(?<uuid>[0-9a-fA-F-]+)\}\s*$""", RegexOptions.Multiline)]
    private static partial Regex LinePattern();
}
