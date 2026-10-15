using System.Windows;
using InventoryApp.Data;
using InventoryApp.Models;
using InventoryApp.ViewModels;
using InventoryApp.Views;
using Microsoft.Win32;

namespace InventoryApp.Services;

public sealed class DialogService : IDialogService
{
    private readonly IPartRepository _parts;
    private readonly ICategoryRepository _categories;

    public DialogService(IPartRepository parts, ICategoryRepository categories)
    {
        _parts = parts;
        _categories = categories;
    }

    private static Window? Owner => Application.Current?.MainWindow;

    public void ShowError(string message, string title = "Error") =>
        Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);

    public void ShowInfo(string message, string title = "Information") =>
        Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);

    public bool Confirm(string message, string title = "Confirm") =>
        Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;

    public bool ShowPartEditor(Part? part)
    {
        var window = new PartEditorWindow(new PartEditorViewModel(_parts, _categories, part)) { Owner = Owner };
        return window.ShowDialog() == true;
    }

    public bool ShowStockMovement(Part part)
    {
        var window = new StockMovementWindow(new StockMovementViewModel(_parts, part)) { Owner = Owner };
        return window.ShowDialog() == true;
    }

    public string? PickCsvSavePath(string suggestedFileName)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Export inventory to CSV",
            Filter = "CSV files (*.csv)|*.csv",
            DefaultExt = ".csv",
            FileName = suggestedFileName,
            AddExtension = true,
            OverwritePrompt = true
        };
        return dialog.ShowDialog(Owner) == true ? dialog.FileName : null;
    }

    private static MessageBoxResult Show(string message, string title, MessageBoxButton buttons, MessageBoxImage image) =>
        Owner is { } owner
            ? MessageBox.Show(owner, message, title, buttons, image)
            : MessageBox.Show(message, title, buttons, image);
}
