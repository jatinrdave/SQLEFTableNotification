using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SqlDbEntityNotifier.CodeGen.Models;
using SqlDbEntityNotifier.Core.Models;

namespace SqlDbEntityNotifier.CodeGen;

/// <summary>
/// Code generator for creating DTOs from database schemas.
/// </summary>
public class CodeGenerator
{
    private readonly ILogger<CodeGenerator> _logger;
    private readonly CodeGenOptions _options;
    private readonly ISchemaReader _schemaReader;

    /// <summary>
    /// Initializes a new instance of the CodeGenerator class.
    /// </summary>
    public CodeGenerator(
        ILogger<CodeGenerator> logger,
        IOptions<CodeGenOptions> options,
        ISchemaReader schemaReader)
    {
        _logger = logger;
        _options = options.Value;
        _schemaReader = schemaReader;
    }

    /// <summary>
    /// Generates DTOs for all tables in the database.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the code generation operation.</returns>
    public async Task<CodeGenResult> GenerateDtosAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting DTO generation for database: {DatabaseType}", _options.Database.Type);

        var result = new CodeGenResult
        {
            StartTime = DateTime.UtcNow
        };

        try
        {
            // Read database schema
            var tables = await _schemaReader.GetTablesAsync(cancellationToken);
            
            // Filter tables
            var filteredTables = FilterTables(tables);
            
            _logger.LogInformation("Found {TableCount} tables to generate DTOs for", filteredTables.Count);

            // Generate DTOs for each table
            foreach (var table in filteredTables)
            {
                try
                {
                    var dtoCode = await GenerateDtoForTableAsync(table, cancellationToken);
                    var fileName = $"{GetClassName(table.Name)}.cs";
                    var filePath = Path.Combine(_options.OutputDirectory, fileName);

                    // Ensure output directory exists
                    Directory.CreateDirectory(_options.OutputDirectory);

                    // Write file
                    await File.WriteAllTextAsync(filePath, dtoCode, cancellationToken);
                    
                    result.GeneratedFiles.Add(filePath);
                    result.GeneratedClasses.Add(GetClassName(table.Name));
                    
                    _logger.LogDebug("Generated DTO for table: {TableName} -> {ClassName}", table.Name, GetClassName(table.Name));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating DTO for table: {TableName}", table.Name);
                    result.Errors.Add($"Error generating DTO for table {table.Name}: {ex.Message}");
                }
            }

            // Generate project file if requested
            if (_options.GenerateProjectFile)
            {
                await GenerateProjectFileAsync(result, cancellationToken);
            }

            result.Success = true;
            result.EndTime = DateTime.UtcNow;
            result.Duration = result.EndTime - result.StartTime;

            _logger.LogInformation("DTO generation completed successfully. Generated {FileCount} files in {Duration}", 
                result.GeneratedFiles.Count, result.Duration);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = ex.Message;
            result.EndTime = DateTime.UtcNow;
            result.Duration = result.EndTime - result.StartTime;

            _logger.LogError(ex, "DTO generation failed");
        }

        return result;
    }

    /// <summary>
    /// Generates a DTO for a specific table.
    /// </summary>
    /// <param name="tableName">The table name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the code generation operation.</returns>
    public async Task<string> GenerateDtoForTableAsync(string tableName, CancellationToken cancellationToken = default)
    {
        var table = await _schemaReader.GetTableAsync(tableName, cancellationToken);
        return await GenerateDtoForTableAsync(table, cancellationToken);
    }

    private async Task<string> GenerateDtoForTableAsync(TableSchema table, CancellationToken cancellationToken)
    {
        var className = GetClassName(table.Name);
        var properties = new StringBuilder();

        foreach (var column in table.Columns)
        {
            var propertyCode = GenerateProperty(column);
            properties.AppendLine(propertyCode);
        }

        var dtoCode = _options.Templates.DtoTemplate
            .Replace("{FileHeader}", _options.FileHeader)
            .Replace("{Namespace}", _options.Namespace)
            .Replace("{TableName}", table.Name)
            .Replace("{ClassName}", className)
            .Replace("{Properties}", properties.ToString().TrimEnd());

        return dtoCode;
    }

    private string GenerateProperty(ColumnSchema column)
    {
        var propertyName = GetPropertyName(column.Name);
        var propertyType = GetPropertyType(column);
        var attributes = GenerateAttributes(column);
        var nullableSuffix = column.IsNullable && _options.GenerateNullableReferenceTypes ? "?" : "";

        var propertyCode = _options.Templates.PropertyTemplate
            .Replace("{ColumnName}", column.Name)
            .Replace("{PropertyName}", propertyName)
            .Replace("{PropertyType}", propertyType)
            .Replace("{Attributes}", attributes)
            .Replace("{NullableSuffix}", nullableSuffix);

        return propertyCode;
    }

    private string GenerateAttributes(ColumnSchema column)
    {
        var attributes = new List<string>();

        if (_options.GenerateJsonAttributes)
        {
            var jsonAttribute = _options.Templates.JsonPropertyTemplate
                .Replace("{ColumnName}", column.Name);
            attributes.Add(jsonAttribute);
        }

        if (_options.GenerateDataAnnotations)
        {
            if (column.IsPrimaryKey)
            {
                attributes.Add(_options.Templates.KeyAttributeTemplate);
            }

            if (!column.IsNullable && !column.IsPrimaryKey)
            {
                attributes.Add(_options.Templates.RequiredAttributeTemplate);
            }
        }

        return string.Join("\n    ", attributes);
    }

    private string GetClassName(string tableName)
    {
        return _options.NamingConvention switch
        {
            NamingConvention.PascalCase => ToPascalCase(tableName),
            NamingConvention.CamelCase => ToCamelCase(tableName),
            NamingConvention.SnakeCase => tableName,
            _ => ToPascalCase(tableName)
        };
    }

    private string GetPropertyName(string columnName)
    {
        return _options.NamingConvention switch
        {
            NamingConvention.PascalCase => ToPascalCase(columnName),
            NamingConvention.CamelCase => ToCamelCase(columnName),
            NamingConvention.SnakeCase => columnName,
            _ => ToPascalCase(columnName)
        };
    }

    private string GetPropertyType(ColumnSchema column)
    {
        return column.DataType.ToLowerInvariant() switch
        {
            "integer" or "int" or "int4" or "serial" => "int",
            "bigint" or "int8" or "bigserial" => "long",
            "smallint" or "int2" or "smallserial" => "short",
            "decimal" or "numeric" => "decimal",
            "real" or "float4" => "float",
            "double precision" or "float8" => "double",
            "boolean" or "bool" => "bool",
            "character varying" or "varchar" or "text" or "string" => "string",
            "character" or "char" => "string",
            "date" => "DateTime",
            "timestamp" or "timestamp without time zone" => "DateTime",
            "timestamptz" or "timestamp with time zone" => "DateTimeOffset",
            "time" or "time without time zone" => "TimeSpan",
            "timetz" or "time with time zone" => "TimeSpan",
            "uuid" => "Guid",
            "bytea" or "blob" => "byte[]",
            "json" or "jsonb" => "string",
            _ => "object"
        };
    }

    private IList<TableSchema> FilterTables(IList<TableSchema> tables)
    {
        var filtered = tables.AsEnumerable();

        // Exclude system tables if not requested
        if (!_options.Filter.IncludeSystemTables)
        {
            filtered = filtered.Where(t => !IsSystemTable(t.Name));
        }

        // Apply include filter
        if (_options.Filter.IncludeTables.Any())
        {
            filtered = filtered.Where(t => _options.Filter.IncludeTables.Contains(t.Name, StringComparer.OrdinalIgnoreCase));
        }

        // Apply exclude filter
        if (_options.Filter.ExcludeTables.Any())
        {
            filtered = filtered.Where(t => !_options.Filter.ExcludeTables.Contains(t.Name, StringComparer.OrdinalIgnoreCase));
        }

        return filtered.ToList();
    }

    private static bool IsSystemTable(string tableName)
    {
        var systemTables = new[]
        {
            "sqlite_master", "sqlite_sequence", "sqlite_stat1", "sqlite_stat4",
            "information_schema", "pg_catalog", "pg_toast", "sys"
        };

        return systemTables.Any(st => tableName.StartsWith(st, StringComparison.OrdinalIgnoreCase));
    }

    private async Task GenerateProjectFileAsync(CodeGenResult result, CancellationToken cancellationToken)
    {
        var projectContent = $@"<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""System.Text.Json"" Version=""8.0.0"" />
    <PackageReference Include=""System.ComponentModel.Annotations"" Version=""5.0.0"" />
  </ItemGroup>

</Project>";

        var projectPath = Path.Combine(_options.OutputDirectory, "Generated.csproj");
        await File.WriteAllTextAsync(projectPath, projectContent, cancellationToken);
        result.GeneratedFiles.Add(projectPath);
    }

    private static string ToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var words = input.Split(new[] { '_', '-', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        var result = new StringBuilder();

        foreach (var word in words)
        {
            if (word.Length > 0)
            {
                result.Append(char.ToUpperInvariant(word[0]));
                if (word.Length > 1)
                {
                    result.Append(word.Substring(1).ToLowerInvariant());
                }
            }
        }

        return result.ToString();
    }

    private static string ToCamelCase(string input)
    {
        var pascalCase = ToPascalCase(input);
        if (string.IsNullOrEmpty(pascalCase))
            return pascalCase;

        return char.ToLowerInvariant(pascalCase[0]) + pascalCase.Substring(1);
    }
}

/// <summary>
/// Result of code generation operation.
/// </summary>
public sealed class CodeGenResult
{
    /// <summary>
    /// Gets or sets whether the generation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the list of generated files.
    /// </summary>
    public IList<string> GeneratedFiles { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets the list of generated class names.
    /// </summary>
    public IList<string> GeneratedClasses { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets the list of errors encountered.
    /// </summary>
    public IList<string> Errors { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets the start time of the generation.
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Gets or sets the end time of the generation.
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// Gets or sets the duration of the generation.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Gets or sets any error message.
    /// </summary>
    public string? Error { get; set; }
}