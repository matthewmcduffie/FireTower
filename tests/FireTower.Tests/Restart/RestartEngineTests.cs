using FireTower.Core.Interfaces;
using FireTower.Core.Models;
using FireTower.Core.Restart;
using FireTower.Shared.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace FireTower.Tests.Restart;

/// <summary>
/// Unit tests for <see cref="RestartEngine"/>'s recovery decision logic, per
/// restart-engine.md. Uses NSubstitute mocks so no VirtualBox installation is needed.
/// </summary>
public sealed class RestartEngineTests
{
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly IRestartHistoryRepository _history = Substitute.For<IRestartHistoryRepository>();
    private readonly IProviderManager _providerManager = Substitute.For<IProviderManager>();

    private RestartEngine CreateEngine()
    {
        return new RestartEngine(
            _history,
            _providerManager,
            new RestartLockRegistry(),
            _clock,
            NullLogger<RestartEngine>.Instance);
    }

    private static VirtualMachine VmWithCriticalHealth(DateTimeOffset? lastRestart = null) => new()
    {
        Id = Guid.NewGuid(),
        ExternalId = "vm-001",
        Name = "TestVM",
        ProviderId = "virtualbox",
        HealthProfileId = "hp1",
        RecoveryProfileId = "rp1",
        Health = HealthState.Critical,
        LastRestart = lastRestart,
    };

    private static RecoveryProfile DefaultProfile() => new()
    {
        Id = "rp1",
        Name = "Default",
        PreferGracefulRestart = true,
        CooldownSeconds = 600,
        MaxRestartAttempts = 3,
        RetryWindowSeconds = 3600,
        EscalationSequence = new[] { RecoveryAction.Restart, RecoveryAction.ForceRestart, RecoveryAction.Notify },
    };

    private static HealthEvaluation CriticalEvaluation(Guid vmId) => new()
    {
        VirtualMachineId = vmId,
        PreviousState = HealthState.Healthy,
        NewState = HealthState.Critical,
        CheckResults = Array.Empty<HealthCheckResult>(),
        Timestamp = DateTimeOffset.UtcNow,
    };

    [Fact]
    public async Task EvaluateAsync_ReturnsDoNothing_WhenVmIsHealthy()
    {
        _clock.UtcNow.Returns(DateTimeOffset.UtcNow);
        var engine = CreateEngine();
        var vm = VmWithCriticalHealth();
        vm.Health = HealthState.Healthy;
        var evaluation = new HealthEvaluation
        {
            VirtualMachineId = vm.Id,
            PreviousState = HealthState.Healthy,
            NewState = HealthState.Healthy,
            CheckResults = Array.Empty<HealthCheckResult>(),
            Timestamp = DateTimeOffset.UtcNow,
        };

        var decision = await engine.EvaluateAsync(vm, DefaultProfile(), evaluation, CancellationToken.None);

        Assert.Equal(RecoveryAction.DoNothing, decision.Action);
    }

    [Fact]
    public async Task EvaluateAsync_ReturnsSuppressed_WhenWithinCooldownPeriod()
    {
        var now = DateTimeOffset.UtcNow;
        _clock.UtcNow.Returns(now);
        _history.CountWithinWindowAsync(Arg.Any<Guid>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
               .Returns(Task.FromResult(0));

        var vm = VmWithCriticalHealth(lastRestart: now - TimeSpan.FromSeconds(60));
        var engine = CreateEngine();
        var decision = await engine.EvaluateAsync(vm, DefaultProfile(), CriticalEvaluation(vm.Id), CancellationToken.None);

        Assert.True(decision.Suppressed);
        Assert.Equal(RecoveryAction.DoNothing, decision.Action);
    }

    [Fact]
    public async Task EvaluateAsync_SelectsRestartAction_OnFirstAttempt()
    {
        var now = DateTimeOffset.UtcNow;
        _clock.UtcNow.Returns(now);
        _history.CountWithinWindowAsync(Arg.Any<Guid>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
               .Returns(Task.FromResult(0));

        var vm = VmWithCriticalHealth();
        var engine = CreateEngine();
        var decision = await engine.EvaluateAsync(vm, DefaultProfile(), CriticalEvaluation(vm.Id), CancellationToken.None);

        Assert.Equal(RecoveryAction.Restart, decision.Action);
        Assert.False(decision.Suppressed);
    }

    [Fact]
    public async Task EvaluateAsync_EscalatesToForceRestart_OnSecondAttempt()
    {
        var now = DateTimeOffset.UtcNow;
        _clock.UtcNow.Returns(now);
        _history.CountWithinWindowAsync(Arg.Any<Guid>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
               .Returns(Task.FromResult(1));

        var vm = VmWithCriticalHealth();
        var engine = CreateEngine();
        var decision = await engine.EvaluateAsync(vm, DefaultProfile(), CriticalEvaluation(vm.Id), CancellationToken.None);

        Assert.Equal(RecoveryAction.ForceRestart, decision.Action);
    }

    [Fact]
    public async Task EvaluateAsync_ReturnsNotify_WhenRetryLimitExceeded()
    {
        var now = DateTimeOffset.UtcNow;
        _clock.UtcNow.Returns(now);
        _history.CountWithinWindowAsync(Arg.Any<Guid>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
               .Returns(Task.FromResult(3));

        var vm = VmWithCriticalHealth();
        var engine = CreateEngine();
        var decision = await engine.EvaluateAsync(vm, DefaultProfile(), CriticalEvaluation(vm.Id), CancellationToken.None);

        Assert.Equal(RecoveryAction.Notify, decision.Action);
    }

    [Fact]
    public async Task EvaluateAsync_SuppressesRecovery_DuringMaintenanceWindow()
    {
        var now = new DateTimeOffset(2024, 1, 15, 2, 0, 0, TimeSpan.Zero);
        _clock.UtcNow.Returns(now);
        _history.CountWithinWindowAsync(Arg.Any<Guid>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
               .Returns(Task.FromResult(0));

        var profile = new RecoveryProfile
        {
            Id = "rp1",
            Name = "Maintenance Profile",
            CooldownSeconds = 600,
            MaxRestartAttempts = 3,
            RetryWindowSeconds = 3600,
            EscalationSequence = new[] { RecoveryAction.Restart },
            MaintenanceWindowDay = now.DayOfWeek,
            MaintenanceWindowStart = new TimeOnly(1, 0),
            MaintenanceWindowEnd = new TimeOnly(3, 0),
        };

        var vm = VmWithCriticalHealth();
        var engine = CreateEngine();
        var decision = await engine.EvaluateAsync(vm, profile, CriticalEvaluation(vm.Id), CancellationToken.None);

        Assert.True(decision.Suppressed);
    }
}
