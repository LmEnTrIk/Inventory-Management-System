using System.Windows;
using InventoryApp.ViewModels;

namespace InventoryApp.Views;

public partial class PartEditorWindow : Window
{
    public PartEditorWindow(PartEditorViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.CloseRequested += saved => DialogResult = saved;
        Loaded += async (_, _) => await viewModel.InitializeAsync();
    }
}
