using System.ComponentModel;
using System.Windows;
using FireTower.Tray.ViewModels;

namespace FireTower.Tray;

/// <summary>
/// Closing the main window minimizes to the system tray rather than exiting, per tray.md.
/// Only the tray icon's Exit command (<see cref="MainWindowViewModel.ExitCommand"/>) ends
/// the process; the Windows Service is never affected either way.
/// </summary>
public partial class MainWindow : Window
{
    private bool _allowClose;

    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        viewModel.ShowWindowRequested += (_, _) =>
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        };

        viewModel.ExitRequested += (_, _) =>
        {
            _allowClose = true;
            System.Windows.Application.Current.Shutdown();
        };
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_allowClose)
        {
            e.Cancel = true;
            Hide();
            return;
        }

        base.OnClosing(e);
    }
}
