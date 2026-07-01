using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FireTower.Shared.DTOs;
using FireTower.Shared.Enums;
using FireTower.Tray.Navigation;
using FireTower.Tray.Services;
using FireTower.Tray.Services.Ipc;

namespace FireTower.Tray.ViewModels;

/// <summary>
/// Coordinates the main window's navigation, connection-state indicator, service status
/// summary, and the global notification strip. Page ViewModels set ErrorMessage or
/// StatusMessage on themselves; this ViewModel forwards them to the main window so every
/// page gets feedback without duplicating notification code in each view.
/// </summary>
public sealed partial class MainWindowViewModel : ViewModelBase
{
    private readonly IFireTowerIpcClient _ipcClient;
    private readonly IUiDispatcher _dispatcher;

    [ObservableProperty]
    private string _selectedPage = "Dashboard";

    [ObservableProperty]
    private ServiceStatusDto? _serviceStatus;

    /// <summary>
    /// Latest error from the currently active page ViewModel.
    /// Shown in the main window's notification strip as long as the page has an error.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    [NotifyPropertyChangedFor(nameof(HasStatus))]
    [NotifyPropertyChangedFor(nameof(HasNotification))]
    [NotifyPropertyChangedFor(nameof(NotificationText))]
    private string? _pageError;

    /// <summary>
    /// Latest success/status message from the currently active page ViewModel.
    /// Auto-clears when the page clears it.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStatus))]
    [NotifyPropertyChangedFor(nameof(HasNotification))]
    [NotifyPropertyChangedFor(nameof(NotificationText))]
    private string? _pageStatus;

    [ObservableProperty]
    private bool _isPageBusy;

    // Computed notification properties used by MainWindow.xaml bindings.
    public bool HasError => PageError is not null;
    public bool HasStatus => PageError is null && PageStatus is not null;
    public bool HasNotification => HasError || HasStatus;
    public string? NotificationText => PageError ?? PageStatus;

    public INavigationService Navigation { get; }

    public ConnectionState ConnectionState => _ipcClient.State;

    public bool IsConnected => _ipcClient.State == ConnectionState.Connected;

    public bool IsDisconnected => !IsConnected;

    public event EventHandler? ShowWindowRequested;
    public event EventHandler? ExitRequested;

    public MainWindowViewModel(INavigationService navigation, IFireTowerIpcClient ipcClient, IUiDispatcher dispatcher)
    {
        Navigation = navigation;
        _ipcClient = ipcClient;
        _dispatcher = dispatcher;
        _ipcClient.ConnectionStateChanged += OnConnectionStateChanged;
        _ipcClient.ServiceStatusChanged += OnServiceStatusChanged;

        Navigation.CurrentViewModelChanged += OnCurrentViewModelChanged;
        Navigation.NavigateTo<DashboardViewModel>();
    }

    [RelayCommand]
    private void ShowWindow() => ShowWindowRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void Exit() => ExitRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private async Task PauseMonitoringAsync()
    {
        if (!IsConnected) return;
        await _ipcClient.Service.PauseMonitoringAsync(CancellationToken.None);
    }

    [RelayCommand]
    private async Task ResumeMonitoringAsync()
    {
        if (!IsConnected) return;
        await _ipcClient.Service.ResumeMonitoringAsync(CancellationToken.None);
    }

    [RelayCommand]
    private void NavigateTo(string pageKey)
    {
        SelectedPage = pageKey;
        switch (pageKey)
        {
            case "Dashboard": Navigation.NavigateTo<DashboardViewModel>(); break;
            case "VirtualMachines": Navigation.NavigateTo<VirtualMachinesViewModel>(); break;
            case "Discovery": Navigation.NavigateTo<DiscoveryViewModel>(); break;
            case "HealthProfiles": Navigation.NavigateTo<HealthProfilesViewModel>(); break;
            case "RecoveryProfiles": Navigation.NavigateTo<RecoveryProfilesViewModel>(); break;
            case "Providers": Navigation.NavigateTo<ProvidersViewModel>(); break;
            case "Logs": Navigation.NavigateTo<LogsViewModel>(); break;
            case "Events": Navigation.NavigateTo<EventsViewModel>(); break;
            case "Statistics": Navigation.NavigateTo<StatisticsViewModel>(); break;
            case "Settings": Navigation.NavigateTo<SettingsViewModel>(); break;
            case "About": Navigation.NavigateTo<AboutViewModel>(); break;
        }
    }

    private void OnConnectionStateChanged(object? sender, ConnectionState state)
    {
        _dispatcher.Invoke(() =>
        {
            OnPropertyChanged(nameof(ConnectionState));
            OnPropertyChanged(nameof(IsConnected));
            OnPropertyChanged(nameof(IsDisconnected));
        });
    }

    private void OnServiceStatusChanged(object? sender, ServiceStatusDto status)
    {
        _dispatcher.Invoke(() => ServiceStatus = status);
    }

    private void OnCurrentViewModelChanged(object? sender, EventArgs e)
    {
        // Clear leftover messages from the previous page when navigating.
        PageError = null;
        PageStatus = null;
        IsPageBusy = false;

        if (Navigation.CurrentViewModel is ViewModelBase pageVm)
        {
            pageVm.PropertyChanged += OnPageVmPropertyChanged;
            SyncFromPage(pageVm);
        }
    }

    private void OnPageVmPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (sender is not ViewModelBase pageVm) return;

        _dispatcher.Invoke(() =>
        {
            if (e.PropertyName is nameof(ErrorMessage) or nameof(StatusMessage) or nameof(IsBusy))
            {
                SyncFromPage(pageVm);
            }
        });
    }

    private void SyncFromPage(ViewModelBase pageVm)
    {
        PageError = pageVm.ErrorMessage;
        PageStatus = pageVm.StatusMessage;
        IsPageBusy = pageVm.IsBusy;
    }
}
