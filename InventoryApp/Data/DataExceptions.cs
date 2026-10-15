namespace InventoryApp.Data;

/// <summary>Thrown when an outbound/adjustment movement would make stock negative.</summary>
public sealed class InsufficientStockException : Exception
{
    public int Available { get; }

    public InsufficientStockException(int available)
        : base($"Insufficient stock. Only {available} unit(s) available.")
    {
        Available = available;
    }
}

/// <summary>Thrown when a SKU already exists.</summary>
public sealed class DuplicateSkuException : Exception
{
    public DuplicateSkuException(string sku)
        : base($"A part with SKU '{sku}' already exists.")
    {
    }
}

/// <summary>Thrown when the referenced part no longer exists.</summary>
public sealed class PartNotFoundException : Exception
{
    public PartNotFoundException(int partId)
        : base($"Part #{partId} no longer exists. Please refresh the list.")
    {
    }
}
