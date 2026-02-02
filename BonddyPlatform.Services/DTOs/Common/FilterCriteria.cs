namespace BonddyPlatform.Services.DTOs.Common;

/// <summary>
/// Represents a filter criteria for advanced filtering
/// </summary>
public class FilterCriteria
{
    /// <summary>
    /// Field name to filter on
    /// </summary>
    public string Field { get; set; } = string.Empty;

    /// <summary>
    /// Filter operator
    /// </summary>
    public FilterOperator Operator { get; set; } = FilterOperator.Equals;

    /// <summary>
    /// Filter value(s). For In/NotIn operators, use comma-separated values
    /// </summary>
    public string? Value { get; set; }
}
