using Dapper;
using InventoryApp.Models;

namespace InventoryApp.Data;

public sealed class CategoryRepository : ICategoryRepository
{
    private readonly DbConnectionFactory _factory;

    public CategoryRepository(DbConnectionFactory factory) => _factory = factory;

    public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _factory.CreateOpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<Category>(new CommandDefinition(
            """
            SELECT CategoryID AS CategoryId, Name, Description
            FROM Categories
            ORDER BY Name
            """,
            cancellationToken: cancellationToken));
        return rows.AsList();
    }
}
