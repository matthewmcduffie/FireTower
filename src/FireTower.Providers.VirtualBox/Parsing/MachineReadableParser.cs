using FireTower.Providers.VirtualBox.Models;

namespace FireTower.Providers.VirtualBox.Parsing;

/// <summary>
/// Parses <c>VBoxManage showvminfo --machinereadable</c> output (key="value" lines) into
/// structured fields, per the parsing requirements in virtualbox.md. Captured sample output
/// is used in provider tests rather than a live VirtualBox installation.
/// </summary>
public static class MachineReadableParser
{
    public static IReadOnlyDictionary<string, string> ParseKeyValuePairs(string output)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var rawLine in output.Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.Length == 0)
            {
                continue;
            }

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = line[..separatorIndex].Trim();
            var value = line[(separatorIndex + 1)..].Trim().Trim('"');
            result[key] = value;
        }

        return result;
    }

    public static VBoxMachineInfo ToMachineInfo(IReadOnlyDictionary<string, string> fields)
    {
        var snapshotCount = 0;
        if (fields.TryGetValue("SnapshotCount", out var snapshotCountText))
        {
            int.TryParse(snapshotCountText, out snapshotCount);
        }

        return new VBoxMachineInfo
        {
            Uuid = fields.GetValueOrDefault("UUID", string.Empty),
            Name = fields.GetValueOrDefault("name", string.Empty),
            VmState = fields.GetValueOrDefault("VMState", "unknown"),
            OsType = fields.GetValueOrDefault("ostype"),
            ConfigFile = fields.GetValueOrDefault("CfgFile"),
            SnapshotCount = snapshotCount,
        };
    }
}
