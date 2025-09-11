using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.Logging;
using SqlDbEntityNotifier.Core.Models;

namespace SqlDbEntityNotifier.Core.Filters;

/// <summary>
/// Engine for compiling and executing LINQ expressions on change events.
/// </summary>
public class ExpressionFilterEngine
{
    private readonly ILogger<ExpressionFilterEngine> _logger;
    private readonly Dictionary<string, Func<ChangeEvent, bool>> _compiledFilters;

    /// <summary>
    /// Initializes a new instance of the ExpressionFilterEngine class.
    /// </summary>
    public ExpressionFilterEngine(ILogger<ExpressionFilterEngine> logger)
    {
        _logger = logger;
        _compiledFilters = new Dictionary<string, Func<ChangeEvent, bool>>();
    }

    /// <summary>
    /// Compiles a LINQ expression into a filter function.
    /// </summary>
    /// <param name="expression">The LINQ expression to compile.</param>
    /// <param name="filterId">Optional filter identifier for caching.</param>
    /// <returns>A compiled filter function.</returns>
    public Func<ChangeEvent, bool> CompileFilter(Expression<Func<ChangeEvent, bool>> expression, string? filterId = null)
    {
        try
        {
            // Check cache first
            if (!string.IsNullOrEmpty(filterId) && _compiledFilters.TryGetValue(filterId, out var cachedFilter))
            {
                return cachedFilter;
            }

            // Validate and optimize the expression
            var optimizedExpression = OptimizeExpression(expression);
            
            // Compile the expression
            var compiledFilter = optimizedExpression.Compile();

            // Cache the compiled filter if an ID was provided
            if (!string.IsNullOrEmpty(filterId))
            {
                _compiledFilters[filterId] = compiledFilter;
            }

            _logger.LogDebug("Successfully compiled filter expression");
            return compiledFilter;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error compiling filter expression");
            throw;
        }
    }

    /// <summary>
    /// Applies a filter to a change event.
    /// </summary>
    /// <param name="changeEvent">The change event to filter.</param>
    /// <param name="filter">The filter function to apply.</param>
    /// <returns>True if the event passes the filter, false otherwise.</returns>
    public bool ApplyFilter(ChangeEvent changeEvent, Func<ChangeEvent, bool> filter)
    {
        try
        {
            return filter(changeEvent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying filter to change event");
            return false; // Fail safe - don't process events that cause filter errors
        }
    }

    /// <summary>
    /// Applies a filter to a collection of change events.
    /// </summary>
    /// <param name="changeEvents">The change events to filter.</param>
    /// <param name="filter">The filter function to apply.</param>
    /// <returns>Filtered change events.</returns>
    public IEnumerable<ChangeEvent> ApplyFilter(IEnumerable<ChangeEvent> changeEvents, Func<ChangeEvent, bool> filter)
    {
        return changeEvents.Where(ev => ApplyFilter(ev, filter));
    }

    /// <summary>
    /// Creates a filter for a specific table and operation.
    /// </summary>
    /// <param name="tableName">The table name to filter on.</param>
    /// <param name="operation">The operation to filter on (optional).</param>
    /// <returns>A compiled filter function.</returns>
    public Func<ChangeEvent, bool> CreateTableFilter(string tableName, string? operation = null)
    {
        if (string.IsNullOrEmpty(operation))
        {
            return CompileFilter(ev => ev.Table == tableName);
        }

        return CompileFilter(ev => ev.Table == tableName && ev.Operation == operation);
    }

    /// <summary>
    /// Creates a filter for a specific source and schema.
    /// </summary>
    /// <param name="source">The source to filter on.</param>
    /// <param name="schema">The schema to filter on (optional).</param>
    /// <returns>A compiled filter function.</returns>
    public Func<ChangeEvent, bool> CreateSourceFilter(string source, string? schema = null)
    {
        if (string.IsNullOrEmpty(schema))
        {
            return CompileFilter(ev => ev.Source == source);
        }

        return CompileFilter(ev => ev.Source == source && ev.Schema == schema);
    }

    /// <summary>
    /// Creates a filter based on change event data using JSON path expressions.
    /// </summary>
    /// <param name="jsonPath">The JSON path to evaluate.</param>
    /// <param name="expectedValue">The expected value.</param>
    /// <returns>A compiled filter function.</returns>
    public Func<ChangeEvent, bool> CreateDataFilter(string jsonPath, object expectedValue)
    {
        return CompileFilter(ev =>
        {
            try
            {
                var value = GetJsonValue(ev.After ?? ev.Before, jsonPath);
                return Equals(value, expectedValue);
            }
            catch
            {
                return false;
            }
        });
    }

    /// <summary>
    /// Combines multiple filters using AND logic.
    /// </summary>
    /// <param name="filters">The filters to combine.</param>
    /// <returns>A combined filter function.</returns>
    public Func<ChangeEvent, bool> CombineFilters(params Func<ChangeEvent, bool>[] filters)
    {
        return CompileFilter(ev => filters.All(filter => filter(ev)));
    }

    /// <summary>
    /// Combines multiple filters using OR logic.
    /// </summary>
    /// <param name="filters">The filters to combine.</param>
    /// <returns>A combined filter function.</returns>
    public Func<ChangeEvent, bool> CombineFiltersOr(params Func<ChangeEvent, bool>[] filters)
    {
        return CompileFilter(ev => filters.Any(filter => filter(ev)));
    }

    /// <summary>
    /// Clears the filter cache.
    /// </summary>
    public void ClearCache()
    {
        _compiledFilters.Clear();
        _logger.LogDebug("Filter cache cleared");
    }

    private static Expression<Func<ChangeEvent, bool>> OptimizeExpression(Expression<Func<ChangeEvent, bool>> expression)
    {
        // Basic expression optimization
        // In a real implementation, you might want to use a more sophisticated optimizer
        return expression;
    }

    private static object? GetJsonValue(JsonElement? jsonElement, string jsonPath)
    {
        if (!jsonElement.HasValue)
        {
            return null;
        }

        var element = jsonElement.Value;
        var pathParts = jsonPath.Split('.', StringSplitOptions.RemoveEmptyEntries);

        foreach (var part in pathParts)
        {
            if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(part, out var property))
            {
                element = property;
            }
            else
            {
                return null;
            }
        }

        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt32(out var intVal) ? intVal : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.GetRawText()
        };
    }
}