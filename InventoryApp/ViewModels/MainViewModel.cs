using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using InventoryApp.Data;
using InventoryApp.Helpers;
using InventoryApp.Models;
using InventoryApp.Services;

namespace InventoryApp.ViewModels;

public partial class MainViewModel : ObservableObject, IRecipient<PartsChangedMessage>
{
    private readonly IPartRepository _parts;
    private readonly ICategoryRepository _categories;
    private readonly IDialogService _dialogs;

    public MainViewModel(IPartRepository parts, ICategoryRepository categories, IDialogService dialogs)
    {
        _parts = parts;
        _categories = categories;
        _dialogs = dialogs;

        Parts = [];
        PartsView = CollectionViewSource.GetDefaultView(Parts);
        PartsView.Filter = FilterPart;

        Categories = [AllCategories];
        _selectedCategory = AllCategories;

        WeakReferenceMessenger.Default.RegisterAll(this);
    }

    private static readonly Category AllCategories = new() { CategoryId = 0, Name = "All Categories" };

    public ObservableCollection<Part> Parts { get; }
    public ICollectionView PartsView { get; }
    public ObservableCollection<Category> Categories { get; }

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private Category? _selectedCategory;
    [ObservableProperty] private bool _lowStockOnly;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    [NotifyCanExecuteChangedFor(nameof(AddPartCommand))]
    [NotifyCanExecuteChangedFor(nameof(EditPartCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeletePartCommand))]
    [NotifyCanExecuteChangedFor(nameof(RegisterMovementCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExportCsvCommand))]
    private bool _isLoading;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditPartCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeletePartCommand))]
    [NotifyCanExecuteChangedFor(nameof(RegisterMovementCommand))]
    private Part? _selectedPart;

    [ObservableProperty] private int _totalItems;
    [ObservableProperty] private decimal _totalInventoryValue;
    [ObservableProperty] private int _lowStockAlerts;

    public bool IsIdle => !IsLoading;

    partial void OnSearchTextChanged(string value) => RefreshView();
    partial void OnSelectedCategoryChanged(Category? value) => RefreshView();
    partial void OnLowStockOnlyChanged(bool value) => RefreshView();

    public void Receive(PartsChangedMessage message) => _ = ReloadAsync();

    [RelayCommand]
    private Task LoadAsync() => ReloadAsync();

    [RelayCommand(CanExecute = nameof(IsIdle))]
    private Task RefreshAsync() => ReloadAsync();

    [RelayCommand(CanExecute = nameof(IsIdle))]
    private void AddPart() => _dialogs.ShowPartEditor(null);

    [RelayCommand(CanExecute = nameof(CanOperateOnSelection))]
    private void EditPart()
    {
        if (SelectedPart is { } part)
            _dialogs.ShowPartEditor(part);
    }

    [RelayCommand(CanExecute = nameof(CanOperateOnSelection))]
    private void RegisterMovement()
    {
        if (SelectedPart is { } part)
            _dialogs.ShowStockMovement(part);
    }

    [RelayCommand(CanExecute = nameof(CanOperateOnSelection))]
    private async Task DeletePartAsync()
    {
        if (SelectedPart is not { } part)
            return;

        if (!_dialogs.Confirm(
                $"Delete part '{part.Name}' (SKU {part.Sku})?\n\nIts stock movement history will be deleted too. This cannot be undone.",
                "Delete part"))
            return;

        try
        {
            IsLoading = true;
            await _parts.DeleteAsync(part.PartId);
        }
        catch (Exception ex)
        {
            _dialogs.ShowError($"Could not delete the part.\n\n{ex.Message}");
            IsLoading = false;
            return;
        }

        IsLoading = false;
        await ReloadAsync();
    }

    [RelayCommand(CanExecute = nameof(IsIdle))]
    private async Task ExportCsvAsync()
    {
        var rows = PartsView.Cast<Part>().ToList();
        if (rows.Count == 0)
        {
            _dialogs.ShowInfo("There are no rows to export with the current filters.", "Export");
            return;
        }

        var path = _dialogs.PickCsvSavePath($"inventory_{DateTime.Now:yyyyMMdd_HHmm}.csv");
        if (path is null)
            return;

        try
        {
            IsLoading = true;
            await CsvExporter.ExportAsync(rows, path);
            _dialogs.ShowInfo($"Exported {rows.Count} row(s) to:\n{path}", "Export complete");
        }
        catch (Exception ex)
        {
            _dialogs.ShowError($"Export failed.\n\n{ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanOperateOnSelection() => IsIdle && SelectedPart is not null;

    private async Task ReloadAsync()
    {
        if (IsLoading)
            return;

        var selectedId = SelectedPart?.PartId;
        var categoryId = SelectedCategory?.CategoryId ?? 0;

        try
        {
            IsLoading = true;

            var partsTask = _parts.GetAllAsync();
            var categoriesTask = _categories.GetAllAsync();
            await Task.WhenAll(partsTask, categoriesTask);

            Categories.Clear();
            Categories.Add(AllCategories);
            foreach (var category in categoriesTask.Result)
                Categories.Add(category);
            SelectedCategory = Categories.FirstOrDefault(c => c.CategoryId == categoryId) ?? AllCategories;

            using (PartsView.DeferRefresh())
            {
                Parts.Clear();
                foreach (var part in partsTask.Result)
                    Parts.Add(part);
            }

            SelectedPart = selectedId is { } id ? Parts.FirstOrDefault(p => p.PartId == id) : null;
            UpdateStatistics();
        }
        catch (Exception ex)
        {
            _dialogs.ShowError($"Could not load inventory data.\n\n{ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool FilterPart(object item)
    {
        if (item is not Part part)
            return false;

        if (SelectedCategory is { CategoryId: > 0 } category && part.CategoryId != category.CategoryId)
            return false;

        if (LowStockOnly && !part.IsLowStock)
            return false;

        var term = SearchText?.Trim();
        return string.IsNullOrEmpty(term)
               || part.Name.Contains(term, StringComparison.CurrentCultureIgnoreCase)
               || part.Sku.Contains(term, StringComparison.CurrentCultureIgnoreCase);
    }

    private void RefreshView()
    {
        PartsView.Refresh();
        UpdateStatistics();
    }

    private void UpdateStatistics()
    {
        var visible = PartsView.Cast<Part>().ToList();
        TotalItems = visible.Count;
        TotalInventoryValue = visible.Sum(p => p.UnitPrice * p.QuantityInStock);
        LowStockAlerts = visible.Count(p => p.IsLowStock);
    }
}
