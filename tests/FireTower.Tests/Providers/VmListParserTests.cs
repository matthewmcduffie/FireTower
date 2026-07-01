using FireTower.Providers.VirtualBox.Parsing;

namespace FireTower.Tests.Providers;

/// <summary>
/// Unit tests for <see cref="VmListParser"/>, using captured VBoxManage list output.
/// </summary>
public sealed class VmListParserTests
{
    private const string SampleListOutput = """
        "Windows Dev Box" {3a4b5c6d-7e8f-9a0b-1c2d-3e4f5a6b7c8d}
        "Linux Server" {aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee}
        "Old VM (Archived)" {11111111-2222-3333-4444-555555555555}
        """;

    [Fact]
    public void Parse_ReturnsOneEntryPerLine()
    {
        var results = VmListParser.Parse(SampleListOutput);
        Assert.Equal(3, results.Count);
    }

    [Fact]
    public void Parse_ExtractsNameAndUuid()
    {
        var results = VmListParser.Parse(SampleListOutput);
        Assert.Equal("Windows Dev Box", results[0].Name);
        Assert.Equal("3a4b5c6d-7e8f-9a0b-1c2d-3e4f5a6b7c8d", results[0].Uuid);
    }

    [Fact]
    public void Parse_HandleVmNamesWithParentheses()
    {
        var results = VmListParser.Parse(SampleListOutput);
        Assert.Equal("Old VM (Archived)", results[2].Name);
    }

    [Fact]
    public void Parse_ReturnsEmpty_ForEmptyOutput()
    {
        var results = VmListParser.Parse(string.Empty);
        Assert.Empty(results);
    }
}
