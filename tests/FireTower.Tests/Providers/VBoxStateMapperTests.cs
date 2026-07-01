using FireTower.Providers.VirtualBox.Parsing;
using FireTower.Shared.Enums;

namespace FireTower.Tests.Providers;

/// <summary>
/// Unit tests for <see cref="VBoxStateMapper"/>, verifying VirtualBox native state
/// string -> FireTower <see cref="VmPowerState"/> translation, per virtualbox.md.
/// Does not require VirtualBox to be installed.
/// </summary>
public sealed class VBoxStateMapperTests
{
    [Theory]
    [InlineData("running", VmPowerState.Running)]
    [InlineData("poweroff", VmPowerState.Stopped)]
    [InlineData("paused", VmPowerState.Paused)]
    [InlineData("saved", VmPowerState.Saved)]
    [InlineData("starting", VmPowerState.Starting)]
    [InlineData("stopping", VmPowerState.Stopping)]
    [InlineData("restoring", VmPowerState.Restoring)]
    [InlineData("aborted", VmPowerState.Aborted)]
    [InlineData("gurumeditation", VmPowerState.Aborted)]
    [InlineData("inaccessible", VmPowerState.Inaccessible)]
    [InlineData("somethingcompletely_unknown", VmPowerState.Unknown)]
    public void Map_TranslatesNativeStateToFireTowerState(string vboxState, VmPowerState expected)
    {
        Assert.Equal(expected, VBoxStateMapper.Map(vboxState));
    }

    [Fact]
    public void Map_IsCaseInsensitive()
    {
        Assert.Equal(VmPowerState.Running, VBoxStateMapper.Map("RUNNING"));
        Assert.Equal(VmPowerState.Running, VBoxStateMapper.Map("Running"));
    }
}
