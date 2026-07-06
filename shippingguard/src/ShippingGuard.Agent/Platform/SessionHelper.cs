using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace ShippingGuard.Agent.Platform;

/// <summary>
/// Launches processes in the active user session from a Windows Service
/// running in Session 0. Without this, processes spawned by a service would
/// be invisible to the logged-in user.
/// </summary>
[SupportedOSPlatform("windows")]
public static class SessionHelper
{
    public static bool TryLaunchInUserSession(string exe, string args, out int processId)
    {
        processId = 0;
        var sessionId = NativeMethods.WTSGetActiveConsoleSessionId();
        if (sessionId == 0xFFFFFFFF) return false;

        if (!NativeMethods.WTSQueryUserToken(sessionId, out var userToken))
            return false;

        try
        {
            if (!NativeMethods.CreateEnvironmentBlock(out var envBlock, userToken, false))
                envBlock = IntPtr.Zero;

            var si = new NativeMethods.STARTUPINFO { cb = Marshal.SizeOf<NativeMethods.STARTUPINFO>() };
            var flags = NativeMethods.CREATE_UNICODE_ENVIRONMENT | NativeMethods.CREATE_NEW_CONSOLE;
            var commandLine = string.IsNullOrEmpty(args) ? $"\"{exe}\"" : $"\"{exe}\" {args}";

            if (!NativeMethods.CreateProcessAsUser(
                    userToken, null, commandLine, IntPtr.Zero, IntPtr.Zero,
                    false, flags, envBlock, null, ref si,
                    out var pi))
            {
                if (envBlock != IntPtr.Zero) NativeMethods.DestroyEnvironmentBlock(envBlock);
                return false;
            }

            processId = (int)pi.dwProcessId;
            NativeMethods.CloseHandle(pi.hProcess);
            NativeMethods.CloseHandle(pi.hThread);
            if (envBlock != IntPtr.Zero) NativeMethods.DestroyEnvironmentBlock(envBlock);
            return true;
        }
        finally
        {
            NativeMethods.CloseHandle(userToken);
        }
    }

    private static class NativeMethods
    {
        public const uint CREATE_UNICODE_ENVIRONMENT = 0x00000400;
        public const uint CREATE_NEW_CONSOLE = 0x00000010;

        [DllImport("kernel32.dll")] public static extern uint WTSGetActiveConsoleSessionId();
        [DllImport("Wtsapi32.dll")] public static extern bool WTSQueryUserToken(uint sessionId, out IntPtr token);
        [DllImport("userenv.dll")]  public static extern bool CreateEnvironmentBlock(out IntPtr environment, IntPtr token, bool inherit);
        [DllImport("userenv.dll")]  public static extern bool DestroyEnvironmentBlock(IntPtr environment);
        [DllImport("kernel32.dll")] public static extern bool CloseHandle(IntPtr handle);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
        public static extern bool CreateProcessAsUser(
            IntPtr token, string? applicationName, string commandLine,
            IntPtr processAttributes, IntPtr threadAttributes,
            bool inheritHandles, uint creationFlags, IntPtr environment,
            string? currentDirectory, ref STARTUPINFO startupInfo,
            out PROCESS_INFORMATION processInfo);

        [StructLayout(LayoutKind.Sequential)]
        public struct STARTUPINFO
        {
            public int cb;
            public string? lpReserved; public string? lpDesktop; public string? lpTitle;
            public uint dwX, dwY, dwXSize, dwYSize, dwXCountChars, dwYCountChars, dwFillAttribute, dwFlags;
            public short wShowWindow, cbReserved2;
            public IntPtr lpReserved2, hStdInput, hStdOutput, hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PROCESS_INFORMATION
        {
            public IntPtr hProcess, hThread;
            public uint dwProcessId, dwThreadId;
        }
    }
}
