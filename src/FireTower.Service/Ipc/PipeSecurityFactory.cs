using System.IO.Pipes;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;

namespace FireTower.Service.Ipc;

/// <summary>
/// Builds the access control list applied to FireTower's Named Pipe. The tray application
/// typically runs as a different Windows identity than the service (an interactive user
/// versus LocalSystem or a service account), so access is granted to any locally
/// authenticated user rather than only the service's own identity. Named Pipes are
/// inherently local-machine-only, satisfying ipc.md's "only local processes may connect";
/// this ACL adds defense in depth on shared machines.
/// </summary>
[SupportedOSPlatform("windows")]
public static class PipeSecurityFactory
{
    public static PipeSecurity Create()
    {
        var security = new PipeSecurity();

        security.AddAccessRule(new PipeAccessRule(
            new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null),
            PipeAccessRights.FullControl,
            AccessControlType.Allow));

        security.AddAccessRule(new PipeAccessRule(
            new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null),
            PipeAccessRights.ReadWrite,
            AccessControlType.Allow));

        return security;
    }
}
