using System.ComponentModel;
using System.Windows;
using ShippingGuard.Core.Configuration;
using ShippingGuard.Tray.ViewModels;
using ShippingGuard.Tray.Views;

namespace ShippingGuard.Tray;

public partial class MainWindow : Window
{
    private bool _allowClose;
    private readonly AgentSettings _settings;

    public MainWindow(MainViewModel vm, AgentSettings settings)
    {
        _settings = settings;
        DataContext = vm;

        vm.ShowWindowRequested += (_, _) => { Show(); WindowState = WindowState.Normal; Activate(); };
        vm.ExitRequested       += (_, _) => { _allowClose = true; Application.Current.Shutdown(); };

        InitializeComponent();
    }

    private void AddApp_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new AddAppDialog { Owner = this };
        if (dlg.ShowDialog() != true || dlg.ViewModel.Result is null) return;
        ProfileLoader.Save(_settings.ProfilesDirectory, dlg.ViewModel.Result);
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_allowClose) { e.Cancel = true; Hide(); return; }
        base.OnClosing(e);
    }
}
