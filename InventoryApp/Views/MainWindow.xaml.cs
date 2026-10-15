using System.Windows;
using System.Windows.Input;
using InventoryApp.ViewModels;

namespace InventoryApp.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        Loaded += async (_, _) => await _viewModel.LoadCommand.ExecuteAsync(null);
    }

    private void OnRowDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (_viewModel.EditPartCommand.CanExecute(null))
            _viewModel.EditPartCommand.Execute(null);
    }
}
