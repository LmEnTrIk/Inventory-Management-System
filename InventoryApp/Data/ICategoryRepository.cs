using InventoryApp.Models;

namespace InventoryApp.Data;

public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken = default);
}
