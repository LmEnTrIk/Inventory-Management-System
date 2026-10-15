namespace InventoryApp.Models;

public class Category
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public override string ToString() => Name;
}
