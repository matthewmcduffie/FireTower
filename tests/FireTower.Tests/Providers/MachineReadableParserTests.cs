using FireTower.Providers.VirtualBox.Parsing;

namespace FireTower.Tests.Providers;

/// <summary>
/// Unit tests for <see cref="MachineReadableParser"/>, using captured VBoxManage output
/// rather than a live VirtualBox installation, per virtualbox.md.
/// </summary>
public sealed class MachineReadableParserTests
{
    private const string SampleShowVmInfo = """
        name="My Test VM"
        groups="/"
        ostype="Ubuntu_64"
        UUID="{550e8400-e29b-41d4-a716-446655440000}"
        CfgFile="C:\VMs\test.vbox"
        VMState="running"
        VMStateChangeTime="2024-01-15T08:30:00.000000000"
        SnapshotCount=2
        """;

    [Fact]
    public void ParseKeyValuePairs_ExtractsAllFields()
    {
        var fields = MachineReadableParser.ParseKeyValuePairs(SampleShowVmInfo);

        Assert.True(fields.ContainsKey("name"));
        Assert.True(fields.ContainsKey("VMState"));
        Assert.True(fields.ContainsKey("SnapshotCount"));
    }

    [Fact]
    public void ParseKeyValuePairs_StripsQuotesFromStringValues()
    {
        var fields = MachineReadableParser.ParseKeyValuePairs(SampleShowVmInfo);
        Assert.Equal("My Test VM", fields["name"]);
    }

    [Fact]
    public void ParseKeyValuePairs_HandlesUnquotedNumericValues()
    {
        var fields = MachineReadableParser.ParseKeyValuePairs(SampleShowVmInfo);
        Assert.Equal("2", fields["SnapshotCount"]);
    }

    [Fact]
    public void ToMachineInfo_MapsFieldsCorrectly()
    {
        var fields = MachineReadableParser.ParseKeyValuePairs(SampleShowVmInfo);
        var info = MachineReadableParser.ToMachineInfo(fields);

        Assert.Equal("My Test VM", info.Name);
        Assert.Equal("running", info.VmState);
        Assert.Equal("Ubuntu_64", info.OsType);
        Assert.Equal(2, info.SnapshotCount);
    }

    [Fact]
    public void ToMachineInfo_ReturnUnknownState_WhenVMStateAbsent()
    {
        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var info = MachineReadableParser.ToMachineInfo(fields);
        Assert.Equal("unknown", info.VmState);
    }

    [Fact]
    public void ParseKeyValuePairs_IgnoresBlankLines()
    {
        var output = "\n\nname=\"VM\"\n\n\nVMState=\"poweroff\"\n";
        var fields = MachineReadableParser.ParseKeyValuePairs(output);
        Assert.Equal(2, fields.Count);
    }
}
