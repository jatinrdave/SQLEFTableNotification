using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;
using SqlDbEntityNotifier.CodeGen.Interfaces;
using SqlDbEntityNotifier.CodeGen.Models;
using System.Data;

namespace SqlDbEntityNotifier.CodeGen.SchemaReaders;

/// <summary>
/// Oracle schema reader implementation.
/// </summary>
public class OracleSchemaReader : ISchemaReader
{
    private readonly ILogger<OracleSchemaReader> _logger;
    private readonly string _connectionString;

    /// <summary>
    /// Initializes a new instance of the OracleSchemaReader class.
    /// </summary>
    public OracleSchemaReader(ILogger<OracleSchemaReader> logger, string connectionString)
    {
        _logger = logger;
        _connectionString = connectionString;
    }

    /// <inheritdoc />
    public async Task<DatabaseSchema> ReadSchemaAsync(string? schemaName = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Reading Oracle schema: {SchemaName}", schemaName ?? "current user");

            using var connection = new OracleConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var databaseSchema = new DatabaseSchema
            {
                DatabaseType = "Oracle",
                SchemaName = schemaName ?? "current_user",
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

            _logger.LogInformation("Successfully read Oracle schema with {TableCount} tables", databaseSchema.Tables.Count);
            return databaseSchema;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading Oracle schema: {SchemaName}", schemaName);
            throw;
        }
    }

    private async Task<IList<TableSchema>> GetTablesAsync(OracleConnection connection, string? schemaName, CancellationToken cancellationToken)
    {
        var tables = new List<TableSchema>();

        var query = @"
            SELECT 
                OWNER,
                TABLE_NAME,
                'TABLE' as TABLE_TYPE,
                COMMENTS
            FROM ALL_TABLES t
            LEFT JOIN ALL_TAB_COMMENTS tc ON t.OWNER = tc.OWNER AND t.TABLE_NAME = tc.TABLE_NAME
            WHERE t.OWNER = COALESCE(UPPER(:schemaName), USER)
            ORDER BY t.TABLE_NAME";

        using var command = new OracleCommand(query, connection);
        command.Parameters.Add(":schemaName", schemaName ?? (object)DBNull.Value);

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            tables.Add(new TableSchema
            {
                SchemaName = reader.GetString("OWNER"),
                TableName = reader.GetString("TABLE_NAME"),
                TableType = reader.GetString("TABLE_TYPE"),
                Comment = reader.IsDBNull("COMMENTS") ? null : reader.GetString("COMMENTS")
            });
        }

        return tables;
    }

    private async Task<IList<ColumnSchema>> GetColumnsAsync(OracleConnection connection, string schemaName, string tableName, CancellationToken cancellationToken)
    {
        var columns = new List<ColumnSchema>();

        var query = @"
            SELECT 
                COLUMN_NAME,
                COLUMN_ID as ORDINAL_POSITION,
                DATA_DEFAULT as COLUMN_DEFAULT,
                NULLABLE,
                DATA_TYPE,
                DATA_LENGTH as CHARACTER_MAXIMUM_LENGTH,
                DATA_PRECISION as NUMERIC_PRECISION,
                DATA_SCALE as NUMERIC_SCALE,
                DATA_TYPE as UDT_NAME,
                COMMENTS as COLUMN_COMMENT
            FROM ALL_TAB_COLUMNS c
            LEFT JOIN ALL_COL_COMMENTS cc ON c.OWNER = cc.OWNER AND c.TABLE_NAME = cc.TABLE_NAME AND c.COLUMN_NAME = cc.COLUMN_NAME
            WHERE c.OWNER = :schemaName
            AND c.TABLE_NAME = :tableName
            ORDER BY c.COLUMN_ID";

        using var command = new OracleCommand(query, connection);
        command.Parameters.Add(":schemaName", schemaName);
        command.Parameters.Add(":tableName", tableName);

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            columns.Add(new ColumnSchema
            {
                ColumnName = reader.GetString("COLUMN_NAME"),
                OrdinalPosition = reader.GetInt32("ORDINAL_POSITION"),
                ColumnDefault = reader.IsDBNull("COLUMN_DEFAULT") ? null : reader.GetString("COLUMN_DEFAULT"),
                IsNullable = reader.GetString("NULLABLE") == "Y",
                DataType = reader.GetString("DATA_TYPE"),
                CharacterMaximumLength = reader.IsDBNull("CHARACTER_MAXIMUM_LENGTH") ? null : reader.GetInt32("CHARACTER_MAXIMUM_LENGTH"),
                NumericPrecision = reader.IsDBNull("NUMERIC_PRECISION") ? null : reader.GetByte("NUMERIC_PRECISION"),
                NumericScale = reader.IsDBNull("NUMERIC_SCALE") ? null : reader.GetInt32("NUMERIC_SCALE"),
                DateTimePrecision = null, // Oracle doesn't have separate datetime precision
                UdtName = reader.GetString("UDT_NAME"),
                Comment = reader.IsDBNull("COLUMN_COMMENT") ? null : reader.GetString("COLUMN_COMMENT")
            });
        }

        return columns;
    }

    private async Task<IList<IndexSchema>> GetIndexesAsync(OracleConnection connection, string schemaName, string tableName, CancellationToken cancellationToken)
    {
        var indexes = new List<IndexSchema>();

        var query = @"
            SELECT 
                i.INDEX_NAME,
                i.INDEX_TYPE,
                i.UNIQUENESS,
                i.TABLE_NAME,
                LISTAGG(ic.COLUMN_NAME, ',') WITHIN GROUP (ORDER BY ic.COLUMN_POSITION) as COLUMN_NAMES
            FROM ALL_INDEXES i
            JOIN ALL_IND_COLUMNS ic ON i.OWNER = ic.INDEX_OWNER AND i.INDEX_NAME = ic.INDEX_NAME
            WHERE i.OWNER = :schemaName
            AND i.TABLE_NAME = :tableName
            AND i.INDEX_TYPE != 'LOB'
            GROUP BY i.INDEX_NAME, i.INDEX_TYPE, i.UNIQUENESS, i.TABLE_NAME
            ORDER BY i.INDEX_NAME";

        using var command = new OracleCommand(query, connection);
        command.Parameters.Add(":schemaName", schemaName);
        command.Parameters.Add(":tableName", tableName);

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var columnNames = reader.GetString("COLUMN_NAMES").Split(',').ToList();

            indexes.Add(new IndexSchema
            {
                IndexName = reader.GetString("INDEX_NAME"),
                IndexDefinition = $"{reader.GetString("INDEX_TYPE")} INDEX {reader.GetString("INDEX_NAME")}",
                IsUnique = reader.GetString("UNIQUENESS") == "UNIQUE",
                IsPrimary = reader.GetString("INDEX_NAME").StartsWith("SYS_") && reader.GetString("UNIQUENESS") == "UNIQUE",
                ColumnNames = columnNames
            });
        }

        return indexes;
    }

    private async Task<IList<ForeignKeySchema>> GetForeignKeysAsync(OracleConnection connection, string schemaName, string tableName, CancellationToken cancellationToken)
    {
        var foreignKeys = new List<ForeignKeySchema>();

        var query = @"
            SELECT 
                c.CONSTRAINT_NAME,
                c.TABLE_NAME,
                cc.COLUMN_NAME,
                r.TABLE_NAME as REFERENCED_TABLE_NAME,
                rc.COLUMN_NAME as REFERENCED_COLUMN_NAME,
                c.DELETE_RULE,
                c.UPDATE_RULE
            FROM ALL_CONSTRAINTS c
            JOIN ALL_CONS_COLUMNS cc ON c.OWNER = cc.OWNER AND c.CONSTRAINT_NAME = cc.CONSTRAINT_NAME
            JOIN ALL_CONSTRAINTS r ON c.R_OWNER = r.OWNER AND c.R_CONSTRAINT_NAME = r.CONSTRAINT_NAME
            JOIN ALL_CONS_COLUMNS rc ON r.OWNER = rc.OWNER AND r.CONSTRAINT_NAME = rc.CONSTRAINT_NAME
            WHERE c.OWNER = :schemaName
            AND c.TABLE_NAME = :tableName
            AND c.CONSTRAINT_TYPE = 'R'
            ORDER BY c.CONSTRAINT_NAME, cc.POSITION";

        using var command = new OracleCommand(query, connection);
        command.Parameters.Add(":schemaName", schemaName);
        command.Parameters.Add(":tableName", tableName);

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