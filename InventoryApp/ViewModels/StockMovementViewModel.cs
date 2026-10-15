using System.ComponentModel.DataAnnotations;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using InventoryApp.Data;
using InventoryApp.Models;
using InventoryApp.Services;

namespace InventoryApp.ViewModels;

public partial class StockMovementViewModel : ObservableValidator
{
    private readonly IPartRepository _parts;

    public StockMovementViewModel(IPartRepository parts, Part part)
    {
        _parts = parts;
        Part = part;
        _currentStock = part.QuantityInStock;
    }

    /// <summary>Raised with true when a movement was registered, false when cancelled.</summary>
    public event Action<bool>? CloseRequested;

    public Part Part { get; }

    public string PartTitle => $"{Part.Name}  ({Part.Sku})";

    public IReadOnlyList<MovementType> MovementTypes { get; } = Enum.GetValues<MovementType>();

    public string QuantityHint => MovementType switch
    {
        MovementType.In => "Units received (whole number greater than 0).",
        MovementType.Out => "Units issued (whole number greater than 0, cannot exceed current stock).",
        _ => "Signed change, e.g. 5 to add or -3 to remove (result cannot be below 0).",
    };

    [ObservableProperty] private int _currentStock;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(QuantityHint))]
    [NotifyPropertyChangedFor(nameof(ResultingStockText))]
    private MovementType _movementType = MovementType.In;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [NotifyPropertyChangedFor(nameof(ResultingStockText))]
    [CustomValidation(typeof(StockMovementViewModel), nameof(ValidateQuantity))]
    private string _quantityText = string.Empty;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [StringLength(255, ErrorMessage = "Notes must be at most 255 characters.")]
    private string? _notes;

    [ObservableProperty] private string? _serverError;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _isBusy;

    public string ResultingStockText =>
        TryComputeDelta(out var delta) ? $"Stock after movement: {CurrentStock + delta}" : "Stock after movement: -";

    partial void OnMovementTypeChanged(MovementType value)
    {
        if (!string.IsNullOrEmpty(QuantityText))
            ValidateProperty(QuantityText, nameof(QuantityText));
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        ServerError = null;
        ValidateAllProperties();
        if (HasErrors || !TryParseQuantity(out var quantity))
            return;

        try
        {
            IsBusy = true;
            var newStock = await _parts.RegisterMovementAsync(Part.PartId, MovementType, quantity, Notes);
            CurrentStock = newStock;
            WeakReferenceMessenger.Default.Send(new PartsChangedMessage());
            CloseRequested?.Invoke(true);
        }
        catch (InsufficientStockException ex)
        {
            CurrentStock = ex.Available;
            ServerError = ex.Message;
            ValidateProperty(QuantityText, nameof(QuantityText));
        }
        catch (Exception ex)
        {
            ServerError = $"Could not register the movement: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanSave() => !IsBusy;

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(false);

    private bool TryParseQuantity(out int quantity) =>
        int.TryParse(QuantityText?.Trim(), NumberStyles.Integer, CultureInfo.CurrentCulture, out quantity);

    private bool TryComputeDelta(out int delta)
    {
        delta = 0;
        if (!TryParseQuantity(out var q))
            return false;
        delta = MovementType == MovementType.Out ? -q : q;
        return true;
    }

    public static ValidationResult? ValidateQuantity(string? value, ValidationContext context)
    {
        var vm = (StockMovementViewModel)context.ObjectInstance;

        if (!int.TryParse(value?.Trim(), NumberStyles.Integer, CultureInfo.CurrentCulture, out var quantity))
            return new ValidationResult("Quantity must be a whole number.");

        switch (vm.MovementType)
        {
            case MovementType.In:
                if (quantity <= 0)
                    return new ValidationResult("Inbound quantity must be greater than 0.");
                break;
            case MovementType.Out:
                if (quantity <= 0)
                    return new ValidationResult("Outbound quantity must be greater than 0.");
                if (quantity > vm.CurrentStock)
                    return new ValidationResult($"Not enough stock. Only {vm.CurrentStock} unit(s) available.");
                break;
            default:
                if (quantity == 0)
                    return new ValidationResult("Adjustment cannot be 0.");
                if (vm.CurrentStock + quantity < 0)
                    return new ValidationResult($"Adjustment would drop stock below 0 (current: {vm.CurrentStock}).");
                break;
        }

        return ValidationResult.Success;
    }
}
