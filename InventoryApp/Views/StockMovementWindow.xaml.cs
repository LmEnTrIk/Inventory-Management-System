using System.Windows;
using InventoryApp.ViewModels;

namespace InventoryApp.Views;

public partial class StockMovementWindow : Window
{
    public StockMovementWindow(StockMovementViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.CloseRequested += registered => DialogResult = registered;
    }
}
