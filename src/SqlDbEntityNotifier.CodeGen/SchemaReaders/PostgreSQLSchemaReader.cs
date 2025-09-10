using Microsoft.Extensions.Logging;
using Npgsql;
using SqlDbEntityNotifier.CodeGen.Interfaces;
using SqlDbEntityNotifier.CodeGen.Models;
using System.Data;

namespace SqlDbEntityNotifier.CodeGen.SchemaReaders;

/// <summary>
/// PostgreSQL schema reader implementation.
/// </summary>
public class PostgreSQLSchemaReader : ISchemaReader
{
    private readonly ILogger<PostgreSQLSchemaReader> _logger;
    private readonly string _connectionString;

    /// <summary>
    /// Initializes a new instance of the PostgreSQLSchemaReader class.
    /// </summary>
    public PostgreSQLSchemaReader(ILogger<PostgreSQLSchemaReader> logger, string connectionString)
    {
        _logger = logger;
        _connectionString = connectionString;
    }

    /// <inheritdoc />
    public async Task<DatabaseSchema> ReadSchemaAsync(string? schemaName = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Reading PostgreSQL schema: {SchemaName}", schemaName ?? "public");

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var databaseSchema = new DatabaseSchema
            {
                DatabaseType = "PostgreSQL",
                SchemaName = schemaName ?? "public",
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

            _logger.LogInformation("Successfully read PostgreSQL schema with {TableCount} tables", databaseSchema.Tables.Count);
            return databaseSchema;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading PostgreSQL schema: {SchemaName}", schemaName);
            throw;
        }
    }

    private async Task<IList<TableSchema>> GetTablesAsync(NpgsqlConnection connection, string? schemaName, CancellationToken cancellationToken)
    {
        var tables = new List<TableSchema>();

        var query = @"
            SELECT 
                t.table_schema,
                t.table_name,
                t.table_type,
                obj_description(c.oid) as table_comment
            FROM information_schema.tables t
            LEFT JOIN pg_class c ON c.relname = t.table_name
            LEFT JOIN pg_namespace n ON n.oid = c.relnamespace AND n.nspname = t.table_schema
            WHERE t.table_schema = COALESCE(@schemaName, 'public')
            AND t.table_type = 'BASE TABLE'
            ORDER BY t.table_name";

        using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("@schemaName", (object?)schemaName ?? DBNull.Value);

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            tables.Add(new TableSchema
            {
                SchemaName = reader.GetString("table_schema"),
                TableName = reader.GetString("table_name"),
                TableType = reader.GetString("table_type"),
                Comment = reader.IsDBNull("table_comment") ? null : reader.GetString("table_comment")
            });
        }

        return tables;
    }

    private async Task<IList<ColumnSchema>> GetColumnsAsync(NpgsqlConnection connection, string schemaName, string tableName, CancellationToken cancellationToken)
    {
        var columns = new List<ColumnSchema>();

        var query = @"
            SELECT 
                c.column_name,
                c.ordinal_position,
                c.column_default,
                c.is_nullable,
                c.data_type,
                c.character_maximum_length,
                c.numeric_precision,
                c.numeric_scale,
                c.datetime_precision,
                c.udt_name,
                col_description(pgc.oid, c.ordinal_position) as column_comment
            FROM information_schema.columns c
            LEFT JOIN pg_class pgc ON pgc.relname = c.table_name
            LEFT JOIN pg_namespace pgn ON pgn.oid = pgc.relnamespace AND pgn.nspname = c.table_schema
            WHERE c.table_schema = @schemaName
            AND c.table_name = @tableName
            ORDER BY c.ordinal_position";

        using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("@schemaName", schemaName);
        command.Parameters.AddWithValue("@tableName", tableName);

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            columns.Add(new ColumnSchema
            {
                ColumnName = reader.GetString("column_name"),
                OrdinalPosition = reader.GetInt32("ordinal_position"),
                ColumnDefault = reader.IsDBNull("column_default") ? null : reader.GetString("column_default"),
                IsNullable = reader.GetString("is_nullable") == "YES",
                DataType = reader.GetString("data_type"),
                CharacterMaximumLength = reader.IsDBNull("character_maximum_length") ? null : reader.GetInt32("character_maximum_length"),
                NumericPrecision = reader.IsDBNull("numeric_precision") ? null : reader.GetByte("numeric_precision"),
                NumericScale = reader.IsDBNull("numeric_scale") ? null : reader.GetInt32("numeric_scale"),
                DateTimePrecision = reader.IsDBNull("datetime_precision") ? null : reader.GetInt16("datetime_precision"),
                UdtName = reader.GetString("udt_name"),
                Comment = reader.IsDBNull("column_comment") ? null : reader.GetString("column_comment")
            });
        }

        return columns;
    }

    private async Task<IList<IndexSchema>> GetIndexesAsync(NpgsqlConnection connection, string schemaName, string tableName, CancellationToken cancellationToken)
    {
        var indexes = new List<IndexSchema>();

        var query = @"
            SELECT 
                i.indexname,
                i.indexdef,
                i.indisunique,
                i.indisprimary,
                array_agg(a.attname ORDER BY a.attnum) as column_names
            FROM pg_indexes i
            LEFT JOIN pg_class c ON c.relname = i.tablename
            LEFT JOIN pg_namespace n ON n.oid = c.relnamespace AND n.nspname = i.schemaname
            LEFT JOIN pg_index ix ON ix.indexrelid = (i.schemaname||'.'||i.indexname)::regclass
            LEFT JOIN pg_attribute a ON a.attrelid = c.oid AND a.attnum = ANY(ix.indkey)
            WHERE i.schemaname = @schemaName
            AND i.tablename = @tableName
            GROUP BY i.indexname, i.indexdef, i.indisunique, i.indisprimary
            ORDER BY i.indexname";

        using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("@schemaName", schemaName);
        command.Parameters.AddWithValue("@tableName", tableName);

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var columnNames = reader.IsDBNull("column_names") ? new string[0] : (string[])reader.GetValue("column_names");

            indexes.Add(new IndexSchema
            {
                IndexName = reader.GetString("indexname"),
                IndexDefinition = reader.GetString("indexdef"),
                IsUnique = reader.GetBoolean("indisunique"),
                IsPrimary = reader.GetBoolean("indisprimary"),
                ColumnNames = columnNames.ToList()
            });
        }

        return indexes;
    }

    private async Task<IList<ForeignKeySchema>> GetForeignKeysAsync(NpgsqlConnection connection, string schemaName, string tableName, CancellationToken cancellationToken)
    {
        var foreignKeys = new List<ForeignKeySchema>();

        var query = @"
            SELECT 
                tc.constraint_name,
                tc.table_name,
                kcu.column_name,
                ccu.table_name AS foreign_table_name,
                ccu.column_name AS foreign_column_name,
                rc.update_rule,
                rc.delete_rule
            FROM information_schema.table_constraints AS tc
            JOIN information_schema.key_column_usage AS kcu
                ON tc.constraint_name = kcu.constraint_name
                AND tc.table_schema = kcu.table_schema
            JOIN information_schema.constraint_column_usage AS ccu
                ON ccu.constraint_name = tc.constraint_name
                AND ccu.table_schema = tc.table_schema
            JOIN information_schema.referential_constraints AS rc
                ON tc.constraint_name = rc.constraint_name
                AND tc.table_schema = rc.constraint_schema
            WHERE tc.constraint_type = 'FOREIGN KEY'
            AND tc.table_schema = @schemaName
            AND tc.table_name = @tableName
            ORDER BY tc.constraint_name, kcu.ordinal_position";

        using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("@schemaName", schemaName);
        command.Parameters.AddWithValue("@tableName", tableName);

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            foreignKeys.Add(new ForeignKeySchema
            {
                ConstraintName = reader.GetString("constraint_name"),
                TableName = reader.GetString("table_name"),
                ColumnName = reader.GetString("column_name"),
                ForeignTableName = reader.GetString("foreign_table_name"),
                ForeignColumnName = reader.GetString("foreign_column_name"),
                UpdateRule = reader.GetString("update_rule"),
                DeleteRule = reader.GetString("delete_rule")
            });
        }

        return foreignKeys;
    }
}