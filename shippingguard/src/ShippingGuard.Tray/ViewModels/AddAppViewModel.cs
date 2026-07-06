using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using ShippingGuard.Core.Models;

namespace ShippingGuard.Tray.ViewModels;

public sealed partial class AddAppViewModel : ObservableObject
{
    [ObservableProperty] private string _displayName = string.Empty;
    [ObservableProperty] private string _processName = string.Empty;
    [ObservableProperty] private string _launchCommand = string.Empty;
    [ObservableProperty] private int _checkIntervalSeconds = 10;
    [ObservableProperty] private int _maxRestartAttempts = 5;
    [ObservableProperty] private bool _killIfHung = true;
    [ObservableProperty] private string? _validationError;

    public AppProfile? Result { get; private set; }

    [RelayCommand]
    private void BrowseForExe()
    {
        var dlg = new OpenFileDialog
        {
            Title = "Select Application Executable",
            Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*",
        };
        if (dlg.ShowDialog() == true)
        {
            LaunchCommand = $"\"{dlg.FileName}\"";
            if (string.IsNullOrWhiteSpace(DisplayName))
                DisplayName = Path.GetFileNameWithoutExtension(dlg.FileName);
            if (string.IsNullOrWhiteSpace(ProcessName))
                ProcessName = Path.GetFileNameWithoutExtension(dlg.FileName);
        }
    }

    public bool TryBuild()
    {
        ValidationError = null;
        if (string.IsNullOrWhiteSpace(DisplayName)) { ValidationError = "Display name is required."; return false; }
        if (string.IsNullOrWhiteSpace(ProcessName)) { ValidationError = "Process name is required."; return false; }
        if (string.IsNullOrWhiteSpace(LaunchCommand)) { ValidationError = "Launch command is required. Use Browse or type the full path."; return false; }

        Result = new AppProfile
        {
            Id = Guid.NewGuid().ToString("N")[..8],
            DisplayName = DisplayName.Trim(),
            ProcessName = ProcessName.Trim(),
            LaunchCommand = LaunchCommand.Trim(),
            CheckIntervalSeconds = Math.Max(5, CheckIntervalSeconds),
            MaxRestartAttempts = Math.Max(1, MaxRestartAttempts),
            KillIfHung = KillIfHung,
            Enabled = true,
        };
        return true;
    }
}
