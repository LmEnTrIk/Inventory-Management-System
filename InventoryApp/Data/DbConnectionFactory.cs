using System.IO;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using MySqlConnector;

namespace InventoryApp.Data;

/// <summary>
/// Creates MySQL connections from <c>appsettings.json</c> and bootstraps the schema.
/// </summary>
public sealed class DbConnectionFactory
{
    private readonly string _connectionString;

    public DbConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("InventoryDb")
            ?? throw new InvalidOperationException(
                "Connection string 'InventoryDb' is missing from appsettings.json.");
    }

    /// <summary>Creates and opens a new connection. The caller owns and must dispose it.</summary>
    public async Task<MySqlConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new MySqlConnection(_connectionString);
        try
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>Creates the database (if needed) and applies the embedded schema script.</summary>
    public async Task EnsureDatabaseAsync(CancellationToken cancellationToken = default)
    {
        var builder = new MySqlConnectionStringBuilder(_connectionString);
        var database = builder.Database;
        if (string.IsNullOrWhiteSpace(database))
            throw new InvalidOperationException("The connection string must specify a Database.");

        builder.Database = string.Empty;
        await using (var server = new MySqlConnection(builder.ConnectionString))
        {
            await server.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var create = server.CreateCommand();
            create.CommandText =
                $"CREATE DATABASE IF NOT EXISTS `{database.Replace("`", "``")}` " +
                "CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;";
            await create.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await using var connection = await CreateOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var script = connection.CreateCommand();
        script.CommandText = ReadSchemaScript(); // MySqlConnector supports multiple statements per command.
        await script.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static string ReadSchemaScript()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("schema.sql")
            ?? throw new InvalidOperationException("Embedded resource 'schema.sql' was not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
