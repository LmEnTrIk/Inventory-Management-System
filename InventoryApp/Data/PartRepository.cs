using Dapper;
using InventoryApp.Models;
using MySqlConnector;

namespace InventoryApp.Data;

public sealed class PartRepository : IPartRepository
{
    private const string SelectPartSql = """
        SELECT p.PartID AS PartId, p.CategoryID AS CategoryId, c.Name AS CategoryName,
               p.SKU AS Sku, p.Name AS Name, p.UnitPrice AS UnitPrice,
               p.QuantityInStock AS QuantityInStock, p.MinThreshold AS MinThreshold
        FROM Parts p
        LEFT JOIN Categories c ON c.CategoryID = p.CategoryID
        """;

    private readonly DbConnectionFactory _factory;

    public PartRepository(DbConnectionFactory factory) => _factory = factory;

    public async Task<IReadOnlyList<Part>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _factory.CreateOpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<Part>(new CommandDefinition(
            SelectPartSql + " ORDER BY p.Name", cancellationToken: cancellationToken));
        return rows.AsList();
    }

    public async Task<Part?> GetByIdAsync(int partId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _factory.CreateOpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Part>(new CommandDefinition(
            SelectPartSql + " WHERE p.PartID = @partId", new { partId }, cancellationToken: cancellationToken));
    }

    public async Task<int> AddAsync(Part part, CancellationToken cancellationToken = default)
    {
        await using var connection = await _factory.CreateOpenConnectionAsync(cancellationToken);
        await using var tx = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            var id = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                """
                INSERT INTO Parts (CategoryID, SKU, Name, UnitPrice, QuantityInStock, MinThreshold)
                VALUES (@CategoryId, @Sku, @Name, @UnitPrice, @QuantityInStock, @MinThreshold);
                SELECT LAST_INSERT_ID();
                """,
                part, tx, cancellationToken: cancellationToken));

            if (part.QuantityInStock > 0)
            {
                await connection.ExecuteAsync(new CommandDefinition(
                    """
                    INSERT INTO Stock_Movements (PartID, MovementType, Quantity, Notes)
                    VALUES (@id, 'IN', @qty, 'Initial stock')
                    """,
                    new { id, qty = part.QuantityInStock }, tx, cancellationToken: cancellationToken));
            }

            await tx.CommitAsync(cancellationToken);
            return id;
        }
        catch (MySqlException ex) when (ex.ErrorCode == MySqlErrorCode.DuplicateKeyEntry)
        {
            await tx.RollbackAsync(CancellationToken.None);
            throw new DuplicateSkuException(part.Sku);
        }
        catch
        {
            await tx.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task UpdateAsync(Part part, CancellationToken cancellationToken = default)
    {
        await using var connection = await _factory.CreateOpenConnectionAsync(cancellationToken);
        try
        {
            // Quantity is intentionally not updated here: stock changes go through
            // RegisterMovementAsync so every change is audited.
            var affected = await connection.ExecuteAsync(new CommandDefinition(
                """
                UPDATE Parts
                SET CategoryID = @CategoryId, SKU = @Sku, Name = @Name,
                    UnitPrice = @UnitPrice, MinThreshold = @MinThreshold
                WHERE PartID = @PartId
                """,
                part, cancellationToken: cancellationToken));

            if (affected == 0 && !await ExistsAsync(connection, part.PartId, cancellationToken))
                throw new PartNotFoundException(part.PartId);
        }
        catch (MySqlException ex) when (ex.ErrorCode == MySqlErrorCode.DuplicateKeyEntry)
        {
            throw new DuplicateSkuException(part.Sku);
        }
    }

    public async Task DeleteAsync(int partId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _factory.CreateOpenConnectionAsync(cancellationToken);
        var affected = await connection.ExecuteAsync(new CommandDefinition(
            "DELETE FROM Parts WHERE PartID = @partId", new { partId }, cancellationToken: cancellationToken));
        if (affected == 0)
            throw new PartNotFoundException(partId);
    }

    public async Task<int> RegisterMovementAsync(int partId, MovementType type, int quantity, string? notes,
        CancellationToken cancellationToken = default)
    {
        if (quantity == 0 || (type != MovementType.Adjustment && quantity < 0))
            throw new ArgumentOutOfRangeException(nameof(quantity), "Invalid movement quantity.");

        await using var connection = await _factory.CreateOpenConnectionAsync(cancellationToken);
        await using var tx = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            // Lock the row so concurrent movements cannot oversell stock.
            var current = await connection.QuerySingleOrDefaultAsync<int?>(new CommandDefinition(
                "SELECT QuantityInStock FROM Parts WHERE PartID = @partId FOR UPDATE",
                new { partId }, tx, cancellationToken: cancellationToken));

            if (current is null)
                throw new PartNotFoundException(partId);

            var delta = type == MovementType.Out ? -quantity : quantity;
            var updated = current.Value + delta;
            if (updated < 0)
                throw new InsufficientStockException(current.Value);

            await connection.ExecuteAsync(new CommandDefinition(
                "UPDATE Parts SET QuantityInStock = @updated WHERE PartID = @partId",
                new { updated, partId }, tx, cancellationToken: cancellationToken));

            await connection.ExecuteAsync(new CommandDefinition(
                """
                INSERT INTO Stock_Movements (PartID, MovementType, Quantity, Notes)
                VALUES (@partId, @movementType, @quantity, @notes)
                """,
                new
                {
                    partId,
                    movementType = type.ToDbValue(),
                    quantity,
                    notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()
                }, tx, cancellationToken: cancellationToken));

            await tx.CommitAsync(cancellationToken);
            return updated;
        }
        catch
        {
            await tx.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private static async Task<bool> ExistsAsync(MySqlConnection connection, int partId, CancellationToken ct) =>
        await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(1) FROM Parts WHERE PartID = @partId", new { partId }, cancellationToken: ct)) > 0;
}
