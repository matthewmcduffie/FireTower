using System.Windows;
using ShippingGuard.Tray.ViewModels;

namespace ShippingGuard.Tray.Views;

public partial class AddAppDialog : Window
{
    private readonly AddAppViewModel _vm;

    public AddAppDialog()
    {
        _vm = new AddAppViewModel();
        DataContext = _vm;
        InitializeComponent();
    }

    public AddAppViewModel ViewModel => _vm;

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        if (_vm.TryBuild()) DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
