using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SqlDbEntityNotifier.Core.Security;

/// <summary>
/// Service for masking personally identifiable information (PII) in change events.
/// </summary>
public class PiiMaskingService
{
    private readonly ILogger<PiiMaskingService> _logger;
    private readonly PiiMaskingOptions _options;
    private readonly Dictionary<string, Regex> _compiledPatterns;

    /// <summary>
    /// Initializes a new instance of the PiiMaskingService class.
    /// </summary>
    public PiiMaskingService(
        ILogger<PiiMaskingService> logger,
        IOptions<PiiMaskingOptions> options)
    {
        _logger = logger;
        _options = options.Value;
        _compiledPatterns = new Dictionary<string, Regex>();

        // Compile regex patterns for performance
        CompilePatterns();
    }

    /// <summary>
    /// Masks PII data in a JSON element.
    /// </summary>
    /// <param name="jsonElement">The JSON element to mask.</param>
    /// <param name="tableName">The table name for context-specific masking.</param>
    /// <returns>The masked JSON element.</returns>
    public JsonElement MaskPiiData(JsonElement jsonElement, string tableName)
    {
        if (!_options.Enabled)
        {
            return jsonElement;
        }

        try
        {
            return MaskJsonElement(jsonElement, tableName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error masking PII data for table {TableName}", tableName);
            return jsonElement; // Return original data if masking fails
        }
    }

    /// <summary>
    /// Masks PII data in a JSON string.
    /// </summary>
    /// <param name="jsonString">The JSON string to mask.</param>
    /// <param name="tableName">The table name for context-specific masking.</param>
    /// <returns>The masked JSON string.</returns>
    public string MaskPiiData(string jsonString, string tableName)
    {
        if (!_options.Enabled || string.IsNullOrEmpty(jsonString))
        {
            return jsonString;
        }

        try
        {
            var jsonElement = JsonDocument.Parse(jsonString).RootElement;
            var maskedElement = MaskPiiData(jsonElement, tableName);
            return maskedElement.GetRawText();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error masking PII data in JSON string for table {TableName}", tableName);
            return jsonString; // Return original data if masking fails
        }
    }

    /// <summary>
    /// Checks if a column should be masked based on the configuration.
    /// </summary>
    /// <param name="columnName">The column name to check.</param>
    /// <param name="tableName">The table name for context.</param>
    /// <returns>True if the column should be masked, false otherwise.</returns>
    public bool ShouldMaskColumn(string columnName, string tableName)
    {
        if (!_options.Enabled)
        {
            return false;
        }

        // Check table-specific column rules
        if (_options.TableColumnRules.TryGetValue(tableName, out var tableRules))
        {
            if (tableRules.Contains(columnName, StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        // Check global column rules
        if (_options.GlobalColumnRules.Contains(columnName, StringComparer.OrdinalIgnoreCase))
        {
            return true;
        }

        // Check regex patterns
        foreach (var pattern in _compiledPatterns.Values)
        {
            if (pattern.IsMatch(columnName))
            {
                return true;
            }
        }

        return false;
    }

    private JsonElement MaskJsonElement(JsonElement jsonElement, string tableName)
    {
        return jsonElement.ValueKind switch
        {
            JsonValueKind.Object => MaskJsonObject(jsonElement, tableName),
            JsonValueKind.Array => MaskJsonArray(jsonElement, tableName),
            _ => jsonElement
        };
    }

    private JsonElement MaskJsonObject(JsonElement jsonObject, string tableName)
    {
        var maskedProperties = new Dictionary<string, object>();

        foreach (var property in jsonObject.EnumerateObject())
        {
            var propertyName = property.Name;
            var propertyValue = property.Value;

            if (ShouldMaskColumn(propertyName, tableName))
            {
                maskedProperties[propertyName] = MaskValue(propertyValue, propertyName);
            }
            else if (propertyValue.ValueKind == JsonValueKind.Object || propertyValue.ValueKind == JsonValueKind.Array)
            {
                // Recursively mask nested objects and arrays
                var maskedValue = MaskJsonElement(propertyValue, tableName);
                maskedProperties[propertyName] = JsonSerializer.Deserialize<object>(maskedValue.GetRawText());
            }
            else
            {
                maskedProperties[propertyName] = JsonSerializer.Deserialize<object>(propertyValue.GetRawText());
            }
        }

        return JsonSerializer.SerializeToElement(maskedProperties);
    }

    private JsonElement MaskJsonArray(JsonElement jsonArray, string tableName)
    {
        var maskedItems = new List<object>();

        foreach (var item in jsonArray.EnumerateArray())
        {
            var maskedItem = MaskJsonElement(item, tableName);
            maskedItems.Add(JsonSerializer.Deserialize<object>(maskedItem.GetRawText()));
        }

        return JsonSerializer.SerializeToElement(maskedItems);
    }

    private object MaskValue(JsonElement value, string columnName)
    {
        var valueString = value.GetRawText().Trim('"');

        // Apply specific masking strategies based on column name patterns
        if (IsEmailColumn(columnName))
        {
            return MaskEmail(valueString);
        }
        else if (IsPhoneColumn(columnName))
        {
            return MaskPhone(valueString);
        }
        else if (IsSsnColumn(columnName))
        {
            return MaskSsn(valueString);
        }
        else if (IsCreditCardColumn(columnName))
        {
            return MaskCreditCard(valueString);
        }
        else
        {
            // Default masking strategy
            return ApplyDefaultMasking(valueString);
        }
    }

    private string MaskEmail(string email)
    {
        if (string.IsNullOrEmpty(email) || !email.Contains('@'))
        {
            return _options.DefaultMaskValue;
        }

        var parts = email.Split('@');
        if (parts.Length != 2)
        {
            return _options.DefaultMaskValue;
        }

        var localPart = parts[0];
        var domain = parts[1];

        if (localPart.Length <= 2)
        {
            return $"{localPart[0]}***@{domain}";
        }

        return $"{localPart[0]}***{localPart[^1]}@{domain}";
    }

    private string MaskPhone(string phone)
    {
        if (string.IsNullOrEmpty(phone))
        {
            return _options.DefaultMaskValue;
        }

        // Remove all non-digit characters
        var digits = Regex.Replace(phone, @"\D", "");
        
        if (digits.Length < 4)
        {
            return _options.DefaultMaskValue;
        }

        // Show last 4 digits
        return $"***-***-{digits[^4..]}";
    }

    private string MaskSsn(string ssn)
    {
        if (string.IsNullOrEmpty(ssn))
        {
            return _options.DefaultMaskValue;
        }

        // Remove all non-digit characters
        var digits = Regex.Replace(ssn, @"\D", "");
        
        if (digits.Length != 9)
        {
            return _options.DefaultMaskValue;
        }

        // Show last 4 digits
        return $"***-**-{digits[^4..]}";
    }

    private string MaskCreditCard(string cardNumber)
    {
        if (string.IsNullOrEmpty(cardNumber))
        {
            return _options.DefaultMaskValue;
        }

        // Remove all non-digit characters
        var digits = Regex.Replace(cardNumber, @"\D", "");
        
        if (digits.Length < 4)
        {
            return _options.DefaultMaskValue;
        }

        // Show last 4 digits
        return $"****-****-****-{digits[^4..]}";
    }

    private string ApplyDefaultMasking(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return _options.DefaultMaskValue;
        }

        return _options.DefaultMaskValue;
    }

    private static bool IsEmailColumn(string columnName)
    {
        return columnName.Contains("email", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPhoneColumn(string columnName)
    {
        return columnName.Contains("phone", StringComparison.OrdinalIgnoreCase) ||
               columnName.Contains("mobile", StringComparison.OrdinalIgnoreCase) ||
               columnName.Contains("telephone", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSsnColumn(string columnName)
    {
        return columnName.Contains("ssn", StringComparison.OrdinalIgnoreCase) ||
               columnName.Contains("social", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCreditCardColumn(string columnName)
    {
        return columnName.Contains("credit", StringComparison.OrdinalIgnoreCase) ||
               columnName.Contains("card", StringComparison.OrdinalIgnoreCase) ||
               columnName.Contains("payment", StringComparison.OrdinalIgnoreCase);
    }

    private void CompilePatterns()
    {
        foreach (var pattern in _options.ColumnNamePatterns)
        {
            try
            {
                _compiledPatterns[pattern] = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Invalid regex pattern for PII masking: {Pattern}", pattern);
            }
        }
    }
}

/// <summary>
/// Configuration options for PII masking.
/// </summary>
public sealed class PiiMaskingOptions
{
    /// <summary>
    /// Gets or sets whether PII masking is enabled.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Gets or sets the default mask value to use.
    /// </summary>
    public string DefaultMaskValue { get; set; } = "***MASKED***";

    /// <summary>
    /// Gets or sets global column names that should always be masked.
    /// </summary>
    public IList<string> GlobalColumnRules { get; set; } = new List<string>
    {
        "password", "passwd", "pwd",
        "ssn", "social_security_number",
        "credit_card", "card_number",
        "email", "email_address",
        "phone", "phone_number", "mobile"
    };

    /// <summary>
    /// Gets or sets table-specific column masking rules.
    /// </summary>
    public IDictionary<string, IList<string>> TableColumnRules { get; set; } = new Dictionary<string, IList<string>>();

    /// <summary>
    /// Gets or sets regex patterns for column names that should be masked.
    /// </summary>
    public IList<string> ColumnNamePatterns { get; set; } = new List<string>
    {
        @".*password.*",
        @".*secret.*",
        @".*token.*",
        @".*key.*",
        @".*ssn.*",
        @".*social.*"
    };
}