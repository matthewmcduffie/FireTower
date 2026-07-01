using System.Windows;
using FireTower.Core.Configuration.Paths;
using FireTower.Tray.Navigation;
using FireTower.Tray.Services;
using FireTower.Tray.Services.Ipc;
using FireTower.Tray.ViewModels;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FireTower.Tray;

/// <summary>
/// Composition root for the tray application: builds the dependency injection container,
/// starts the IPC connection, and owns the tray icon's lifetime independently of the main
/// window, per tray.md.
/// </summary>
public partial class App : System.Windows.Application
{
    private IHost? _host;
    private TaskbarIcon? _trayIcon;

    // WPF's Application.OnStartup is declared void by the base class, so it cannot be
    // changed to return Task; the try/catch below is what actually guards against the
    // unhandled-exception risk VSTHRD100 warns about.
#pragma warning disable VSTHRD100
    protected override async void OnStartup(StartupEventArgs e)
#pragma warning restore VSTHRD100
    {
        base.OnStartup(e);

        try
        {
            _host = Host.CreateApplicationBuilder().Build();
            var services = BuildServiceProvider();

            var preferencesService = services.GetRequiredService<ITrayPreferencesService>();
            await preferencesService.LoadAsync();

            var ipcClient = services.GetRequiredService<IFireTowerIpcClient>();
            ipcClient.Start();

            _trayIcon = (TaskbarIcon)FindResource("TrayIcon");

            // Use the Windows "shield" system icon as a placeholder until a custom icon
            // is designed. Setting Icon directly on the TaskbarIcon prevents the blank
            // white square that appears when no icon is configured.
            _trayIcon.Icon = System.Drawing.SystemIcons.Shield;

            var mainWindowViewModel = services.GetRequiredService<MainWindowViewModel>();
            _trayIcon.DataContext = mainWindowViewModel;
            _trayIcon.TrayMouseDoubleClick += (_, _) => mainWindowViewModel.ShowWindowCommand.Execute(null);

            var mainWindow = services.GetRequiredService<MainWindow>();
            if (!preferencesService.Current.MinimizeToTrayOnStartup)
            {
                mainWindow.Show();
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"FireTower failed to start: {ex.Message}", "FireTower", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    private ServiceProvider BuildServiceProvider()
    {
        var collection = new ServiceCollection();

        collection.AddLogging();
        collection.AddSingleton<IFireTowerPaths, FireTowerPaths>();
        collection.AddSingleton<ITrayPreferencesService, TrayPreferencesService>();
        collection.AddSingleton<IUiDispatcher, WpfDispatcher>();
        collection.AddSingleton<IFireTowerIpcClient, FireTowerIpcClient>();
        collection.AddSingleton<INavigationService, NavigationService>();

        collection.AddTransient<DashboardViewModel>();
        collection.AddTransient<VirtualMachinesViewModel>();
        collection.AddTransient<DiscoveryViewModel>();
        collection.AddTransient<HealthProfilesViewModel>();
        collection.AddTransient<RecoveryProfilesViewModel>();
        collection.AddTransient<ProvidersViewModel>();
        collection.AddTransient<LogsViewModel>();
        collection.AddTransient<EventsViewModel>();
        collection.AddTransient<StatisticsViewModel>();
        collection.AddTransient<SettingsViewModel>();
        collection.AddTransient<AboutViewModel>();

        collection.AddSingleton<MainWindowViewModel>();
        collection.AddSingleton<MainWindow>();

        var provider = collection.BuildServiceProvider();
        _serviceProvider = provider;
        return provider;
    }

    private ServiceProvider? _serviceProvider;

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        if (_serviceProvider?.GetService<IFireTowerIpcClient>() is FireTowerIpcClient client)
        {
            client.Dispose();
        }

        _serviceProvider?.Dispose();
        _host?.Dispose();
        base.OnExit(e);
    }
}
