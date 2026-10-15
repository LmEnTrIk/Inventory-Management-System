namespace InventoryApp.Models;

public enum MovementType
{
    In,
    Out,
    Adjustment
}

public static class MovementTypeExtensions
{
    /// <summary>Value stored in the MySQL ENUM column.</summary>
    public static string ToDbValue(this MovementType type) => type switch
    {
        MovementType.In => "IN",
        MovementType.Out => "OUT",
        MovementType.Adjustment => "ADJUSTMENT",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };

    public static string ToDisplayName(this MovementType type) => type switch
    {
        MovementType.In => "Inbound / Receipt",
        MovementType.Out => "Outbound / Disbursement",
        MovementType.Adjustment => "Adjustment (+/-)",
        _ => type.ToString()
    };
}

public class StockMovement
{
    public int MovementId { get; set; }
    public int PartId { get; set; }
    public string MovementType { get; set; } = string.Empty;

    /// <summary>Positive for IN/OUT; signed delta for ADJUSTMENT.</summary>
    public int Quantity { get; set; }

    public DateTime MovementDate { get; set; }
    public string? Notes { get; set; }
}
