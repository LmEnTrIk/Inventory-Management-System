using System.ComponentModel.DataAnnotations;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using InventoryApp.Data;
using InventoryApp.Models;
using InventoryApp.Services;

namespace InventoryApp.ViewModels;

public partial class PartEditorViewModel : ObservableValidator
{
    private readonly IPartRepository _parts;
    private readonly ICategoryRepository _categories;
    private readonly Part? _original;

    public PartEditorViewModel(IPartRepository parts, ICategoryRepository categories, Part? part)
    {
        _parts = parts;
        _categories = categories;
        _original = part;

        if (part is not null)
        {
            _sku = part.Sku;
            _name = part.Name;
            _unitPriceText = part.UnitPrice.ToString("0.00", CultureInfo.CurrentCulture);
            _quantityText = part.QuantityInStock.ToString(CultureInfo.CurrentCulture);
            _minThresholdText = part.MinThreshold.ToString(CultureInfo.CurrentCulture);
        }
    }

    /// <summary>Raised with true when the part was saved, false when cancelled.</summary>
    public event Action<bool>? CloseRequested;

    public bool IsEdit => _original is not null;
    public string Title => IsEdit ? "Edit Part" : "New Part";

    /// <summary>On existing parts stock is changed via stock movements so every change is audited.</summary>
    public bool IsQuantityEditable => !IsEdit;

    public List<Category> Categories { get; } = [];

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "SKU is required.")]
    [StringLength(50, ErrorMessage = "SKU must be at most 50 characters.")]
    private string _sku = string.Empty;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Name is required.")]
    [StringLength(150, ErrorMessage = "Name must be at most 150 characters.")]
    private string _name = string.Empty;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Please select a category.")]
    private Category? _selectedCategory;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [CustomValidation(typeof(PartEditorViewModel), nameof(ValidateUnitPrice))]
    private string _unitPriceText = "0.00";

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [CustomValidation(typeof(PartEditorViewModel), nameof(ValidateQuantity))]
    private string _quantityText = "0";

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [CustomValidation(typeof(PartEditorViewModel), nameof(ValidateMinThreshold))]
    private string _minThresholdText = "5";

    [ObservableProperty] private string? _serverError;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _isBusy;

    public async Task InitializeAsync()
    {
        try
        {
            IsBusy = true;
            var categories = await _categories.GetAllAsync();
            Categories.Clear();
            Categories.AddRange(categories);
            OnPropertyChanged(nameof(Categories));

            if (_original?.CategoryId is { } id)
                SelectedCategory = Categories.FirstOrDefault(c => c.CategoryId == id);
        }
        catch (Exception ex)
        {
            ServerError = $"Could not load categories: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        ServerError = null;
        ValidateAllProperties();
        if (HasErrors)
            return;

        var part = new Part
        {
            PartId = _original?.PartId ?? 0,
            CategoryId = SelectedCategory!.CategoryId,
            Sku = Sku.Trim(),
            Name = Name.Trim(),
            UnitPrice = ParseDecimal(UnitPriceText),
            QuantityInStock = _original?.QuantityInStock ?? ParseInt(QuantityText),
            MinThreshold = ParseInt(MinThresholdText)
        };

        try
        {
            IsBusy = true;
            if (IsEdit)
                await _parts.UpdateAsync(part);
            else
                await _parts.AddAsync(part);

            WeakReferenceMessenger.Default.Send(new PartsChangedMessage());
            CloseRequested?.Invoke(true);
        }
        catch (DuplicateSkuException ex)
        {
            ServerError = ex.Message;
        }
        catch (Exception ex)
        {
            ServerError = $"Could not save the part: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanSave() => !IsBusy;

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(false);

    #region Validation

    private static readonly NumberStyles IntStyle = NumberStyles.Integer;
    private static readonly NumberStyles DecimalStyle = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;

    private static decimal ParseDecimal(string text) =>
        decimal.Parse(text.Trim(), DecimalStyle, CultureInfo.CurrentCulture);

    private static int ParseInt(string text) =>
        int.Parse(text.Trim(), IntStyle, CultureInfo.CurrentCulture);

    public static ValidationResult? ValidateUnitPrice(string? value, ValidationContext context)
    {
        if (!decimal.TryParse(value?.Trim(), DecimalStyle, CultureInfo.CurrentCulture, out var price))
            return new ValidationResult("Enter a valid price (e.g. 12.50).");
        if (price < 0m)
            return new ValidationResult("Price must be 0.00 or greater.");
        if (price > 99_999_999.99m)
            return new ValidationResult("Price is too large.");
        if (decimal.Round(price, 2) != price)
            return new ValidationResult("Price can have at most 2 decimal places.");
        return ValidationResult.Success;
    }

    public static ValidationResult? ValidateQuantity(string? value, ValidationContext context) =>
        ValidateNonNegativeInt(value, "Quantity");

    public static ValidationResult? ValidateMinThreshold(string? value, ValidationContext context) =>
        ValidateNonNegativeInt(value, "Min threshold");

    private static ValidationResult? ValidateNonNegativeInt(string? value, string field)
    {
        if (!int.TryParse(value?.Trim(), IntStyle, CultureInfo.CurrentCulture, out var number))
            return new ValidationResult($"{field} must be a whole number.");
        if (number < 0)
            return new ValidationResult($"{field} cannot be negative.");
        return ValidationResult.Success;
    }

    #endregion
}
