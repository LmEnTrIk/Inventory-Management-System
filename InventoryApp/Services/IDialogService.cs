using InventoryApp.Models;

namespace InventoryApp.Services;

/// <summary>Abstracts windows/dialogs so ViewModels stay UI-framework agnostic and testable.</summary>
public interface IDialogService
{
    void ShowError(string message, string title = "Error");
    void ShowInfo(string message, string title = "Information");
    bool Confirm(string message, string title = "Confirm");

    /// <summary>Opens the part editor (null = new part). Returns true if saved.</summary>
    bool ShowPartEditor(Part? part);

    /// <summary>Opens the stock movement dialog. Returns true if a movement was registered.</summary>
    bool ShowStockMovement(Part part);

    /// <summary>Shows a save dialog. Returns null when cancelled.</summary>
    string? PickCsvSavePath(string suggestedFileName);
}
