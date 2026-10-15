namespace InventoryApp.Models;

public class Part
{
    public int PartId { get; set; }
    public int? CategoryId { get; set; }

    /// <summary>Joined from Categories; display only.</summary>
    public string? CategoryName { get; set; }

    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int QuantityInStock { get; set; }
    public int MinThreshold { get; set; } = 5;

    public bool IsLowStock => QuantityInStock <= MinThreshold;

    public decimal TotalValue => UnitPrice * QuantityInStock;

    public string Status => QuantityInStock <= 0 ? "Out of Stock" : IsLowStock ? "Low Stock" : "OK";
}
