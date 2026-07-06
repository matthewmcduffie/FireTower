using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;
using ShippingGuard.Core.Configuration;
using ShippingGuard.Tray.Ipc;
using ShippingGuard.Tray.ViewModels;
using ShippingGuard.UiAutomation;
using Microsoft.Extensions.Logging.Abstractions;

namespace ShippingGuard.Tray;

public partial class App : Application
{
    private TaskbarIcon? _trayIcon;
    private MainViewModel? _vm;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            var settings = new AgentSettings();
            settings.EnsureDirectoriesExist();

            var ipc = new AgentIpcClient(settings.PipeName);
            var dialogWatcher = new DialogWatcher(NullLogger<DialogWatcher>.Instance);
            _vm = new MainViewModel(ipc, settings, dialogWatcher);

            _trayIcon = (TaskbarIcon)FindResource("TrayIcon");
            _trayIcon.Icon = System.Drawing.SystemIcons.Application;
            _trayIcon.DataContext = _vm;
            _trayIcon.TrayMouseDoubleClick += (_, _) => _vm.ShowWindowCommand.Execute(null);

            var window = new MainWindow(_vm, settings);
            window.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"ShippingGuard failed to start: {ex.Message}", "ShippingGuard",
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _vm?.Dispose();
        _trayIcon?.Dispose();
        base.OnExit(e);
    }
}
