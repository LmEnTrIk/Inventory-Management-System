using InventoryApp.Models;

namespace InventoryApp.Data;

public interface IPartRepository
{
    Task<IReadOnlyList<Part>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Part?> GetByIdAsync(int partId, CancellationToken cancellationToken = default);

    /// <summary>Inserts the part (plus an initial IN movement when quantity &gt; 0) and returns the new ID.</summary>
    Task<int> AddAsync(Part part, CancellationToken cancellationToken = default);

    Task UpdateAsync(Part part, CancellationToken cancellationToken = default);

    Task DeleteAsync(int partId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically updates stock and writes the audit record. Returns the new stock level.
    /// </summary>
    /// <exception cref="InsufficientStockException">Stock would drop below zero.</exception>
    Task<int> RegisterMovementAsync(int partId, MovementType type, int quantity, string? notes,
        CancellationToken cancellationToken = default);
}
