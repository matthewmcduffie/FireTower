using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace FireTower.Providers.VirtualBox.Platform;

/// <summary>
/// Runs a process in the active user's interactive session from a Windows Service
/// running in Session 0, and captures its stdout/stderr. Required for VBoxManage:
/// VBoxSVC (which manages runtime VM state) runs in the user's session, not Session 0.
/// COM cross-session calls are blocked, so VBoxManage must run as the user to reach it.
/// </summary>
[SupportedOSPlatform("windows")]
internal static class UserSessionRunner
{
    public static (int ExitCode, string Output, string Error) Run(
        string executablePath,
        IReadOnlyList<string> arguments,
        TimeSpan timeout)
    {
        var sessionId = WTSGetActiveConsoleSessionId();
        if (sessionId == 0xFFFFFFFF)
            return (-1, string.Empty, "No active user session found.");

        if (!WTSQueryUserToken(sessionId, out var userToken))
            return (-1, string.Empty, $"WTSQueryUserToken failed: {Marshal.GetLastWin32Error()}");

        try
        {
            CreateEnvironmentBlock(out var envBlock, userToken, false);

            var sa = new SECURITY_ATTRIBUTES
            {
                nLength = Marshal.SizeOf<SECURITY_ATTRIBUTES>(),
                bInheritHandle = true,
                lpSecurityDescriptor = IntPtr.Zero,
            };

            if (!CreatePipe(out var stdoutRead, out var stdoutWrite, ref sa, 0))
                return (-1, string.Empty, "CreatePipe (stdout) failed.");

            if (!CreatePipe(out var stderrRead, out var stderrWrite, ref sa, 0))
            {
                CloseHandle(stdoutRead); CloseHandle(stdoutWrite);
                return (-1, string.Empty, "CreatePipe (stderr) failed.");
            }

            // The parent's read ends must not be inherited by the child.
            SetHandleInformation(stdoutRead, HANDLE_FLAG_INHERIT, 0);
            SetHandleInformation(stderrRead, HANDLE_FLAG_INHERIT, 0);

            var si = new STARTUPINFO
            {
                cb         = Marshal.SizeOf<STARTUPINFO>(),
                dwFlags    = STARTF_USESTDHANDLES,
                hStdOutput = stdoutWrite,
                hStdError  = stderrWrite,
                hStdInput  = IntPtr.Zero,
            };

            // Build the command line the same way ProcessStartInfo does.
            var cmdLine = BuildCommandLine(executablePath, arguments);

            bool created = CreateProcessAsUser(
                userToken, null, cmdLine,
                IntPtr.Zero, IntPtr.Zero,
                true,
                CREATE_UNICODE_ENVIRONMENT | CREATE_NO_WINDOW,
                envBlock,
                null, ref si, out var pi);

            CloseHandle(stdoutWrite);
            CloseHandle(stderrWrite);
            if (envBlock != IntPtr.Zero) DestroyEnvironmentBlock(envBlock);

            if (!created)
            {
                CloseHandle(stdoutRead); CloseHandle(stderrRead);
                return (-1, string.Empty, $"CreateProcessAsUser failed: {Marshal.GetLastWin32Error()}");
            }

            var stdout = ReadPipe(stdoutRead);
            var stderr = ReadPipe(stderrRead);
            CloseHandle(stdoutRead);
            CloseHandle(stderrRead);

            var ms = (uint)Math.Min(timeout.TotalMilliseconds, uint.MaxValue);
            WaitForSingleObject(pi.hProcess, ms);
            GetExitCodeProcess(pi.hProcess, out var exitCode);

            CloseHandle(pi.hProcess);
            CloseHandle(pi.hThread);

            return ((int)exitCode, stdout, stderr);
        }
        finally
        {
            CloseHandle(userToken);
        }
    }

    private static string ReadPipe(IntPtr readHandle)
    {
        var sb = new StringBuilder();
        var buf = new byte[4096];
        while (ReadFile(readHandle, buf, (uint)buf.Length, out var bytesRead, IntPtr.Zero) && bytesRead > 0)
            sb.Append(Encoding.UTF8.GetString(buf, 0, (int)bytesRead));
        return sb.ToString();
    }

    private static string BuildCommandLine(string exe, IReadOnlyList<string> args)
    {
        var sb = new StringBuilder();
        sb.Append('"').Append(exe).Append('"');
        foreach (var arg in args)
        {
            sb.Append(' ');
            if (arg.Contains(' ') || arg.Contains('"'))
                sb.Append('"').Append(arg.Replace("\"", "\\\"")).Append('"');
            else
                sb.Append(arg);
        }
        return sb.ToString();
    }

    private const uint CREATE_UNICODE_ENVIRONMENT = 0x00000400;
    private const uint CREATE_NO_WINDOW           = 0x08000000;
    private const uint HANDLE_FLAG_INHERIT        = 0x00000001;
    private const uint STARTF_USESTDHANDLES       = 0x00000100;

    [DllImport("kernel32.dll")] static extern uint WTSGetActiveConsoleSessionId();
    [DllImport("Wtsapi32.dll", SetLastError = true)] static extern bool WTSQueryUserToken(uint sessionId, out IntPtr token);
    [DllImport("userenv.dll")]  static extern bool CreateEnvironmentBlock(out IntPtr env, IntPtr token, bool inherit);
    [DllImport("userenv.dll")]  static extern bool DestroyEnvironmentBlock(IntPtr env);
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool CreatePipe(out IntPtr read, out IntPtr write, ref SECURITY_ATTRIBUTES sa, uint size);
    [DllImport("kernel32.dll")] static extern bool SetHandleInformation(IntPtr obj, uint mask, uint flags);
    [DllImport("kernel32.dll")] static extern bool ReadFile(IntPtr file, byte[] buf, uint toRead, out uint read, IntPtr overlapped);
    [DllImport("kernel32.dll")] static extern uint WaitForSingleObject(IntPtr handle, uint ms);
    [DllImport("kernel32.dll")] static extern bool GetExitCodeProcess(IntPtr handle, out uint code);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr handle);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool CreateProcessAsUser(
        IntPtr token, string? app, string cmdLine,
        IntPtr procAttr, IntPtr threadAttr, bool inherit,
        uint flags, IntPtr env, string? dir,
        ref STARTUPINFO si, out PROCESS_INFORMATION pi);

    [StructLayout(LayoutKind.Sequential)] struct SECURITY_ATTRIBUTES
    {
        public int nLength; public IntPtr lpSecurityDescriptor; public bool bInheritHandle;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)] struct STARTUPINFO
    {
        public int cb;
        public string? lpReserved, lpDesktop, lpTitle;
        public uint dwX, dwY, dwXSize, dwYSize, dwXCountChars, dwYCountChars, dwFillAttribute, dwFlags;
        public short wShowWindow, cbReserved2;
        public IntPtr lpReserved2, hStdInput, hStdOutput, hStdError;
    }

    [StructLayout(LayoutKind.Sequential)] struct PROCESS_INFORMATION
    {
        public IntPtr hProcess, hThread;
        public uint dwProcessId, dwThreadId;
    }
}
