using Microsoft.Extensions.Logging;
using MySqlConnector;
using SqlDbEntityNotifier.CodeGen.Interfaces;
using SqlDbEntityNotifier.CodeGen.Models;
using System.Data;

namespace SqlDbEntityNotifier.CodeGen.SchemaReaders;

/// <summary>
/// MySQL schema reader implementation.
/// </summary>
public class MySQLSchemaReader : ISchemaReader
{
    private readonly ILogger<MySQLSchemaReader> _logger;
    private readonly string _connectionString;

    /// <summary>
    /// Initializes a new instance of the MySQLSchemaReader class.
    /// </summary>
    public MySQLSchemaReader(ILogger<MySQLSchemaReader> logger, string connectionString)
    {
        _logger = logger;
        _connectionString = connectionString;
    }

    /// <inheritdoc />
    public async Task<DatabaseSchema> ReadSchemaAsync(string? schemaName = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Reading MySQL schema: {SchemaName}", schemaName ?? "default");

            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var databaseSchema = new DatabaseSchema
            {
                DatabaseType = "MySQL",
                SchemaName = schemaName ?? "default",
                Tables = new List<TableSchema>()
            };

            // Get tables
            var tables = await GetTablesAsync(connection, schemaName, cancellationToken);
            databaseSchema.Tables.AddRange(tables);

            // Get columns for each table
            foreach (var table in databaseSchema.Tables)
            {
                table.Columns = await GetColumnsAsync(connection, table.SchemaName, table.TableName, cancellationToken);
                table.Indexes = await GetIndexesAsync(connection, table.SchemaName, table.TableName, cancellationToken);
                table.ForeignKeys = await GetForeignKeysAsync(connection, table.SchemaName, table.TableName, cancellationToken);
            }

            _logger.LogInformation("Successfully read MySQL schema with {TableCount} tables", databaseSchema.Tables.Count);
            return databaseSchema;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading MySQL schema: {SchemaName}", schemaName);
            throw;
        }
    }

    private async Task<IList<TableSchema>> GetTablesAsync(MySqlConnection connection, string? schemaName, CancellationToken cancellationToken)
    {
        var tables = new List<TableSchema>();

        var query = @"
            SELECT 
                TABLE_SCHEMA,
                TABLE_NAME,
                TABLE_TYPE,
                TABLE_COMMENT
            FROM information_schema.TABLES
            WHERE TABLE_SCHEMA = COALESCE(@schemaName, DATABASE())
            AND TABLE_TYPE = 'BASE TABLE'
            ORDER BY TABLE_NAME";

        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@schemaName", (object?)schemaName ?? DBNull.Value);

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            tables.Add(new TableSchema
            {
                SchemaName = reader.GetString("TABLE_SCHEMA"),
                TableName = reader.GetString("TABLE_NAME"),
                TableType = reader.GetString("TABLE_TYPE"),
                Comment = reader.IsDBNull("TABLE_COMMENT") ? null : reader.GetString("TABLE_COMMENT")
            });
        }

        return tables;
    }

    private async Task<IList<ColumnSchema>> GetColumnsAsync(MySqlConnection connection, string schemaName, string tableName, CancellationToken cancellationToken)
    {
        var columns = new List<ColumnSchema>();

        var query = @"
            SELECT 
                COLUMN_NAME,
                ORDINAL_POSITION,
                COLUMN_DEFAULT,
                IS_NULLABLE,
                DATA_TYPE,
                CHARACTER_MAXIMUM_LENGTH,
                NUMERIC_PRECISION,
                NUMERIC_SCALE,
                DATETIME_PRECISION,
                COLUMN_TYPE,
                COLUMN_COMMENT
            FROM information_schema.COLUMNS
            WHERE TABLE_SCHEMA = @schemaName
            AND TABLE_NAME = @tableName
            ORDER BY ORDINAL_POSITION";

        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@schemaName", schemaName);
        command.Parameters.AddWithValue("@tableName", tableName);

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            columns.Add(new ColumnSchema
            {
                ColumnName = reader.GetString("COLUMN_NAME"),
                OrdinalPosition = reader.GetInt32("ORDINAL_POSITION"),
                ColumnDefault = reader.IsDBNull("COLUMN_DEFAULT") ? null : reader.GetString("COLUMN_DEFAULT"),
                IsNullable = reader.GetString("IS_NULLABLE") == "YES",
                DataType = reader.GetString("DATA_TYPE"),
                CharacterMaximumLength = reader.IsDBNull("CHARACTER_MAXIMUM_LENGTH") ? null : reader.GetInt64("CHARACTER_MAXIMUM_LENGTH"),
                NumericPrecision = reader.IsDBNull("NUMERIC_PRECISION") ? null : reader.GetUInt32("NUMERIC_PRECISION"),
                NumericScale = reader.IsDBNull("NUMERIC_SCALE") ? null : reader.GetUInt32("NUMERIC_SCALE"),
                DateTimePrecision = reader.IsDBNull("DATETIME_PRECISION") ? null : reader.GetUInt32("DATETIME_PRECISION"),
                UdtName = reader.GetString("COLUMN_TYPE"),
                Comment = reader.IsDBNull("COLUMN_COMMENT") ? null : reader.GetString("COLUMN_COMMENT")
            });
        }

        return columns;
    }

    private async Task<IList<IndexSchema>> GetIndexesAsync(MySqlConnection connection, string schemaName, string tableName, CancellationToken cancellationToken)
    {
        var indexes = new List<IndexSchema>();

        var query = @"
            SELECT 
                INDEX_NAME,
                COLUMN_NAME,
                NON_UNIQUE,
                INDEX_TYPE,
                CARDINALITY
            FROM information_schema.STATISTICS
            WHERE TABLE_SCHEMA = @schemaName
            AND TABLE_NAME = @tableName
            ORDER BY INDEX_NAME, SEQ_IN_INDEX";

        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@schemaName", schemaName);
        command.Parameters.AddWithValue("@tableName", tableName);

        var indexGroups = new Dictionary<string, IndexSchema>();

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var indexName = reader.GetString("INDEX_NAME");
            var columnName = reader.GetString("COLUMN_NAME");
            var nonUnique = reader.GetInt32("NON_UNIQUE");
            var indexType = reader.GetString("INDEX_TYPE");

            if (!indexGroups.TryGetValue(indexName, out var index))
            {
                index = new IndexSchema
                {
                    IndexName = indexName,
                    IndexDefinition = $"{indexType} INDEX {indexName}",
                    IsUnique = nonUnique == 0,
                    IsPrimary = indexName == "PRIMARY",
                    ColumnNames = new List<string>()
                };
                indexGroups[indexName] = index;
            }

            index.ColumnNames.Add(columnName);
        }

        return indexGroups.Values.ToList();
    }

    private async Task<IList<ForeignKeySchema>> GetForeignKeysAsync(MySqlConnection connection, string schemaName, string tableName, CancellationToken cancellationToken)
    {
        var foreignKeys = new List<ForeignKeySchema>();

        var query = @"
            SELECT 
                CONSTRAINT_NAME,
                TABLE_NAME,
                COLUMN_NAME,
                REFERENCED_TABLE_NAME,
                REFERENCED_COLUMN_NAME,
                UPDATE_RULE,
                DELETE_RULE
            FROM information_schema.KEY_COLUMN_USAGE kcu
            JOIN information_schema.REFERENTIAL_CONSTRAINTS rc
                ON kcu.CONSTRAINT_NAME = rc.CONSTRAINT_NAME
                AND kcu.TABLE_SCHEMA = rc.CONSTRAINT_SCHEMA
            WHERE kcu.TABLE_SCHEMA = @schemaName
            AND kcu.TABLE_NAME = @tableName
            AND kcu.REFERENCED_TABLE_NAME IS NOT NULL
            ORDER BY kcu.CONSTRAINT_NAME, kcu.ORDINAL_POSITION";

        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@schemaName", schemaName);
        command.Parameters.AddWithValue("@tableName", tableName);

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            foreignKeys.Add(new ForeignKeySchema
            {
                ConstraintName = reader.GetString("CONSTRAINT_NAME"),
                TableName = reader.GetString("TABLE_NAME"),
                ColumnName = reader.GetString("COLUMN_NAME"),
                ForeignTableName = reader.GetString("REFERENCED_TABLE_NAME"),
                ForeignColumnName = reader.GetString("REFERENCED_COLUMN_NAME"),
                UpdateRule = reader.GetString("UPDATE_RULE"),
                DeleteRule = reader.GetString("DELETE_RULE")
            });
        }

        return foreignKeys;
    }
}