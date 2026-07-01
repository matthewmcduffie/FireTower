namespace FireTower.Tray.Navigation;

/// <summary>
/// Switches the main window's content between page ViewModels. Views never navigate
/// directly; they raise commands that ViewModels forward here, per tray.md's requirement
/// that the UI contain presentation only.
/// </summary>
public interface INavigationService
{
    object? CurrentViewModel { get; }

    event EventHandler? CurrentViewModelChanged;

    void NavigateTo<TViewModel>() where TViewModel : class;
}
